using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricAppTest.Regression;

/// <summary>Fixes from the review before the 8.0 release: threads, sources, short links and bulk list changes.</summary>
public class ReleaseReviewRegressionTests
{
    private sealed class HeadlessMainViewModel(TestWorkspace ws, SearchService search)
        : MainWindowViewModel(ws.Window.Object, ws.Storage(), search)
    {
        public override bool IsPlaybackSupported => false;
    }

    [AvaloniaFact]
    public async Task TheFetchedSongLinkIsSetOnTheUiThread()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var song = TestWorkspace.Song();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["123"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("123", "123", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        var fetchedOffUiThread = false;
        api.Setup(x => x.GetSongLink("123")).Returns(() =>
        {
            fetchedOffUiThread = !Dispatcher.UIThread.CheckAccess();
            return new ResultVo<string>("https://test.invalid/song.mp3");
        });
        var vm = new HeadlessMainViewModel(ws, ws.Search(api));
        try
        {
            bool? setOnUiThread = null;
            vm.SearchResultViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SearchResultViewModel.SongLink) && vm.SearchResultViewModel.SongLink.Length > 0)
                    setOnUiThread = Dispatcher.UIThread.CheckAccess();
            };

            vm.SearchParamViewModel.SearchText = "123";
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);

            // The request leaves the UI thread; the link it brings back is bound and played there.
            Assert.True(fetchedOffUiThread);
            Assert.True(setOnUiThread);
            Assert.True(vm.HasSongLink);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public async Task ALateSongLinkIsKeptWhenOnlyAnOptionChangedMeanwhile()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var song = TestWorkspace.Song();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["123"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("123", "123", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        using var asked = new ManualResetEventSlim();
        using var answer = new ManualResetEventSlim();
        api.Setup(x => x.GetSongLink("123")).Returns(() =>
        {
            asked.Set();
            answer.Wait(TimeSpan.FromSeconds(10));
            return new ResultVo<string>("https://test.invalid/song.mp3");
        });
        var vm = new HeadlessMainViewModel(ws, ws.Search(api));
        try
        {
            vm.SearchParamViewModel.SearchText = "123";
            var search = vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.True(asked.Wait(TimeSpan.FromSeconds(10)));

            // While the link is on its way, the output format changes: the preview is redone, the song is the same.
            vm.SearchParamViewModel.OutputFormat.Value = OutputFormatEnum.SRT;
            Dispatcher.UIThread.RunJobs();
            answer.Set();
            await search;

            Assert.Equal("https://test.invalid/song.mp3", vm.SearchResultViewModel.SongLink);
            Assert.True(vm.HasSongLink);
        }
        finally
        {
            answer.Set();
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Fact]
    public void ExportingSongLinksKeepsGoingPastASongWithoutOne()
    {
        using var ws = new TestWorkspace();
        var net = new Mock<IMusicApi>(MockBehavior.Strict);
        net.Setup(x => x.GetSongLink("1")).Returns(ResultVo<string>.Failure("VIP only"));
        net.Setup(x => x.GetSongLink("2")).Returns(new ResultVo<string>("https://test.invalid/2.mp3"));
        var storage = ws.Storage();
        storage.SetSearchService(ws.Search(net));
        var several = TestWorkspace.Result(TestWorkspace.Song("1"), TestWorkspace.Song("2"));

        storage.SaveSongLink(several, ws.Settings, ws.Window.Object);

        Assert.Contains("https://test.invalid/2.mp3", several.ConsoleOutput);
    }

    [Fact]
    public void TheMemoryCacheTakesWritesFromManyThreadsAtOnce()
    {
        var prefix = "concurrency-" + Guid.NewGuid().ToString("N");
        Parallel.For(0, 20_000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            GlobalCache.DoCache(prefix, CacheType.SONG_LINK, i.ToString(), "link" + i);
            GlobalCache.Query<string>(prefix, CacheType.SONG_LINK, (i / 2).ToString());
        });

        for (var i = 0; i < 20_000; i++)
        {
            Assert.Equal("link" + i, GlobalCache.Query<string>(prefix, CacheType.SONG_LINK, i.ToString()));
        }
    }

    [Fact]
    public void CopyingSongLinksAsksEachSongsOwnSource()
    {
        using var ws = new TestWorkspace();
        // NetEase is picked on the page, but the songs came from QQ Music; the strict NetEase mock fails if asked.
        ws.Settings.Param.SearchSource = SearchSourceEnum.NET_EASE_MUSIC;
        var qq = new Mock<IMusicApi>(MockBehavior.Strict);
        qq.Setup(x => x.GetSongLink("1")).Returns(new ResultVo<string>("https://test.invalid/1.mp3"));
        qq.Setup(x => x.GetSongLink("2")).Returns(new ResultVo<string>("https://test.invalid/2.mp3"));
        var storage = ws.Storage();
        storage.SetSearchService(ws.Search(new(MockBehavior.Strict), qq));

        var single = TestWorkspace.Result(TestWorkspace.Song("1", source: SearchSourceEnum.QQ_MUSIC));
        Assert.Equal(ErrorMsgConst.SONG_URL_GET_SUCCESS, storage.SaveSongLink(single, ws.Settings, ws.Window.Object));
        Assert.Equal("https://test.invalid/1.mp3", ws.Clipboard);

        var several = TestWorkspace.Result(TestWorkspace.Song("1", source: SearchSourceEnum.QQ_MUSIC),
            TestWorkspace.Song("2", source: SearchSourceEnum.QQ_MUSIC));
        storage.SaveSongLink(several, ws.Settings, ws.Window.Object);
        Assert.Contains("https://test.invalid/2.mp3", several.ConsoleOutput);
    }

    private static Func<HttpClient> Client(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        () => new HttpClient(new ResponseHandler(respond));

    [Fact]
    public void ShortLinksFollowOnlySuccessfulRedirectsAndReportNetworkFailuresPlainly()
    {
        const string shortLink = "https://qishui.douyin.com/s/abc/";
        const string target = "https://music.douyin.com/qishui/share/track?track_id=7400000000000000000";

        Assert.Equal(target, GlobalUtils.ResolveShortLink(shortLink, Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, target)
        })));

        // An error page is not followed.
        Assert.Null(GlobalUtils.ResolveShortLink(shortLink, Client(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://music.douyin.com/404")
        })));

        // Share text that is not a link is an invalid input, not a raw .NET message.
        var notALink = Assert.Throws<MusicLyricException>(() =>
            GlobalUtils.ResolveShortLink("分享歌曲 https://c6.y.qq.com/base/fcgi-bin/u?__=x", Client(_ => new HttpResponseMessage())));
        Assert.Equal(ErrorMsgConst.INPUT_ID_ILLEGAL, notALink.Message);

        // A timeout or a network error says what happened, not "One or more errors occurred".
        foreach (var failure in new Exception[] { new TaskCanceledException("timeout"), new HttpRequestException("dns") })
        {
            var ex = Assert.Throws<MusicLyricException>(() => GlobalUtils.ResolveShortLink(shortLink, Client(_ => throw failure)));
            Assert.Equal(ErrorMsgConst.SHORT_LINK_UNREACHABLE, ex.Message);
        }
    }

    [Fact]
    public void BulkChangesToTheDownloadListRebuildTheShownRowsOnce()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        var ids = Enumerable.Range(1, 200).Select(i => i.ToString()).ToList();
        var results = ids.ToDictionary(id => id, id => new ResultVo<SaveVo>(TestWorkspace.Song(id)));
        var resets = 0;
        vm.VisibleItems.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
        };

        vm.AddSearchResults(results, ids.Select(id => TestWorkspace.Input(id)).ToList());
        Assert.Equal(200, vm.VisibleItems.Count);
        Assert.Equal(1, resets);

        resets = 0;
        foreach (var item in vm.Items.Take(150)) item.IsSelected = true;
        vm.ExecuteDeleteSelectedCommand.Execute(null);
        Assert.Equal(50, vm.VisibleItems.Count);
        Assert.Equal(1, resets);
    }

    [AvaloniaFact]
    public void TheSignInStateNamesTheSourceAsTheSourcePickerDoes() => TestWorkspace.WithLanguage("en-US", () =>
    {
        var account = new AccountStatusViewModel(SearchSourceEnum.QQ_MUSIC);
        Assert.StartsWith(MusicSources.Get(SearchSourceEnum.QQ_MUSIC).Name, account.SourceLabel);
        Assert.StartsWith(LocalizationService.Get("Enum.SearchSourceEnum.QQ_MUSIC"), account.SourceLabel);
    });
}
