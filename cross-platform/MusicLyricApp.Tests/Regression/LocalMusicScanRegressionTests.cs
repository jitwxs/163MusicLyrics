using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.LocalMusic;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class LocalMusicScanRegressionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class InlineProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
    private sealed class Reader(Func<string, LocalAudioFile> read) : IAudioMetadataReader { public LocalAudioFile Read(string path) => read(path); }
    private static LocalAudioFile Audio(string path = "song.flac", string title = "园游会", string artist = "周杰伦", long duration = 255000) => new(path, title, artist, "七里香", duration);
    private static SearchResultVo.SongSearchResultVo Candidate(string id = "186008", string title = "园游会", string artist = "周杰伦", long duration = 255000) =>
        new() { DisplayId = id, Title = title, AuthorName = [artist], AlbumName = "七里香", Duration = duration };
    private static ResultVo<SearchResultVo> Results(params SearchResultVo.SongSearchResultVo[] songs)
    {
        var result = new SearchResultVo();
        result.SongVos.AddRange(songs);
        return new(result);
    }

    [Theory]
    [InlineData("01 - 周杰伦 - 园游会.flac", "园游会", "周杰伦")]
    [InlineData("01 - 园游会.mp3", "园游会", "")]
    [InlineData("1984.mp3", "1984", "")]
    [InlineData("周杰伦 - 晴天 (Live).ogg", "晴天 (Live)", "周杰伦")]
    public void FilenameFallbackKeepsVersionAndNumericTitle(string name, string title, string artist)
    {
        Assert.Equal((title, artist), AudioMetadataReader.ParseFileName(name));
    }

    [Fact]
    public void MetadataReaderReadsRealWaveTagsWithoutChangingAudio()
    {
        using var ws = new TestWorkspace();
        var path = Path.Combine(ws.Root, "filename is not title.wav");
        WriteWave(path);
        using (var tagged = TagLib.File.Create(path))
        {
            tagged.Tag.Title = "园游会";
            tagged.Tag.Performers = ["周杰伦"];
            tagged.Tag.Album = "七里香";
            tagged.Save();
        }
        var bytes = File.ReadAllBytes(path);
        var audio = new AudioMetadataReader().Read(path);
        Assert.Equal("园游会", audio.Title);
        Assert.Equal("周杰伦", audio.Artist);
        Assert.Equal("七里香", audio.Album);
        Assert.InRange(audio.Duration, 990, 1010);
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }

    [Fact]
    public void ScannerFiltersFormatsRecursesAndKeepsFailuresPerFile()
    {
        using var ws = new TestWorkspace();
        var nested = Directory.CreateDirectory(Path.Combine(ws.Root, "nested")).FullName;
        foreach (var name in new[] { "a.MP3", "b.flac", "c.m4a", "d.ogg", "e.wav", "ignore.txt", "ignore.lrc" })
            File.WriteAllText(Path.Combine(ws.Root, name), "fixture");
        File.WriteAllText(Path.Combine(nested, "broken.mp3"), "broken");
        var scanner = new LocalAudioScanner(new Reader(p => Path.GetFileName(p) == "broken.mp3" ? throw new IOException("broken tags") : Audio(p)));
        var files = new List<LocalAudioFile>();
        var issues = new List<string>();
        scanner.Scan(ws.Root, false, new InlineProgress<LocalAudioFile>(files.Add), new InlineProgress<string>(issues.Add), Token);
        Assert.Equal(5, files.Count);
        files.Clear();
        scanner.Scan(ws.Root, true, new InlineProgress<LocalAudioFile>(files.Add), new InlineProgress<string>(issues.Add), Token);
        Assert.Equal(6, files.Count);
        Assert.Equal("broken tags", Assert.Single(files, x => x.Error != null).Error);
        Assert.Empty(issues);
        scanner.Scan(Path.Combine(ws.Root, "missing"), true, new InlineProgress<LocalAudioFile>(files.Add), new InlineProgress<string>(issues.Add), Token);
        Assert.Single(issues);
    }

    [Fact]
    public void ScannerCancellationStopsBeforeNextFile()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "a.mp3"), "");
        File.WriteAllText(Path.Combine(ws.Root, "b.mp3"), "");
        using var cancel = new CancellationTokenSource();
        var files = new List<LocalAudioFile>();
        Assert.Throws<OperationCanceledException>(() => new LocalAudioScanner(new Reader(p => Audio(p))).Scan(ws.Root, true,
            new InlineProgress<LocalAudioFile>(f => { files.Add(f); cancel.Cancel(); }), new InlineProgress<string>(_ => { }), cancel.Token));
        Assert.Single(files);
    }

    [Theory]
    [InlineData("园游会", "周杰伦", 255000, true)]
    [InlineData("园游会 (Live)", "周杰伦", 255000, false)]
    [InlineData("园游会", "其他歌手", 255000, false)]
    [InlineData("园游会", "周杰伦", 280000, false)]
    public void MatchingRequiresArtistAndCompatibleVersionAndDuration(string title, string artist, long duration, bool reliable)
    {
        Assert.Equal(reliable, LocalSongMatcher.Score(Audio(), SearchSourceEnum.NET_EASE_MUSIC,
            Candidate(title: title, artist: artist, duration: duration)).Reliable);
    }

    [Fact]
    public void MissingArtistAndTiedCandidatesRequireConfirmation()
    {
        Assert.False(LocalSongMatcher.Score(Audio(artist: ""), SearchSourceEnum.NET_EASE_MUSIC, Candidate()).Reliable);
        var first = LocalSongMatcher.Score(Audio(), SearchSourceEnum.NET_EASE_MUSIC, Candidate());
        Assert.Null(new LocalMatchResult([first, first with { Id = "another-version" }]).Automatic);
    }

    [Fact]
    public async Task MatchingUsesSongSearchAndFallbackWithSourceQualifiedIds()
    {
        var net = new Mock<IMusicApi>(MockBehavior.Strict);
        var qq = new Mock<IMusicApi>(MockBehavior.Strict);
        net.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Returns(Results(Candidate(artist: "其他歌手")));
        qq.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Returns(Results(Candidate()));
        var matcher = new LocalSongMatcher(s => s == SearchSourceEnum.NET_EASE_MUSIC ? net.Object : qq.Object, TimeSpan.Zero);
        var result = await matcher.MatchAsync(Audio(), "园游会 周杰伦", SearchSourceEnum.NET_EASE_MUSIC, true, false, Token);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, result.Automatic!.Source);
        net.VerifyAll(); qq.VerifyAll();
    }

    [Fact]
    public async Task FallbackTriesEverySourceAfterThePreferredOne()
    {
        // Preferred QQ Music finds nothing reliable, NetEase neither; only Soda Music has the song.
        var asked = new List<SearchSourceEnum>();
        Mock<IMusicApi> Api(SearchSourceEnum source, SearchResultVo result)
        {
            var api = new Mock<IMusicApi>(MockBehavior.Strict);
            api.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Returns(() =>
            {
                asked.Add(source);
                return new ResultVo<SearchResultVo>(result);
            });
            return api;
        }

        var apis = new Dictionary<SearchSourceEnum, Mock<IMusicApi>>
        {
            [SearchSourceEnum.QQ_MUSIC] = Api(SearchSourceEnum.QQ_MUSIC, Results(Candidate(artist: "其他歌手")).Data),
            [SearchSourceEnum.NET_EASE_MUSIC] = Api(SearchSourceEnum.NET_EASE_MUSIC, Results(Candidate(artist: "其他歌手")).Data),
            [SearchSourceEnum.SODA_MUSIC] = Api(SearchSourceEnum.SODA_MUSIC, Results(Candidate()).Data),
        };
        var matcher = new LocalSongMatcher(s => apis[s].Object, TimeSpan.Zero);
        var result = await matcher.MatchAsync(Audio(), "园游会 周杰伦", SearchSourceEnum.QQ_MUSIC, true, false, Token);

        Assert.Equal(new[] { SearchSourceEnum.QQ_MUSIC, SearchSourceEnum.NET_EASE_MUSIC, SearchSourceEnum.SODA_MUSIC }, asked);
        Assert.Equal(SearchSourceEnum.SODA_MUSIC, result.Automatic!.Source);
    }

    [Fact]
    public async Task ASourceThatTimesOutIsAWarningAndTheNextSourceIsTried()
    {
        // HttpClient reports a timeout as TaskCanceledException; after the retries it must not end the match.
        var net = new Mock<IMusicApi>(MockBehavior.Strict);
        net.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Throws(new TaskCanceledException("timeout"));
        var qq = new Mock<IMusicApi>(MockBehavior.Strict);
        qq.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Returns(Results(Candidate()));
        var matcher = new LocalSongMatcher(s => s == SearchSourceEnum.NET_EASE_MUSIC ? net.Object : qq.Object, TimeSpan.Zero);

        var result = await matcher.MatchAsync(Audio(), "园游会 周杰伦", SearchSourceEnum.NET_EASE_MUSIC, true, false, Token);

        Assert.Equal(SearchSourceEnum.QQ_MUSIC, result.Automatic!.Source);
        Assert.Contains(MusicLyricApp.Core.MusicSources.Get(SearchSourceEnum.NET_EASE_MUSIC).Name, result.Warning);
    }

    [Fact]
    public async Task ReliablePreferredMatchAvoidsUnnecessaryRequests()
    {
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search("q", SearchTypeEnum.SONG_ID)).Returns(Results(Candidate()));
        var matcher = new LocalSongMatcher(source => source == SearchSourceEnum.NET_EASE_MUSIC ? api.Object : throw new InvalidOperationException(), TimeSpan.Zero);
        Assert.NotNull((await matcher.MatchAsync(Audio(), "q", SearchSourceEnum.NET_EASE_MUSIC, true, false, Token)).Automatic);
    }

    [Fact]
    public async Task ExplicitFilenameIdModeNeverUsesPlaylistOrFuzzySearch()
    {
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongVo(It.Is<string[]>(ids => ids.SequenceEqual(new[] { "186008" })))).Returns(
            new Dictionary<string, ResultVo<SongVo>> { ["186008"] = new(TestWorkspace.Song("186008").SongVo) });
        var result = await new LocalSongMatcher(_ => api.Object, TimeSpan.Zero).MatchAsync(Audio("186008.mp3"), "ignored",
            SearchSourceEnum.NET_EASE_MUSIC, true, true, Token);
        Assert.Equal("186008", result.Automatic!.Id);
        api.VerifyAll();
        api.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LyricsUseInternalIdAndCacheByPlatform()
    {
        var calls = 0;
        IMusicApi Api(SearchSourceEnum source)
        {
            var api = new Mock<IMusicApi>(MockBehavior.Strict);
            api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>>
                { ["same"] = new(new SongVo { Id = "internal", DisplayId = "same", Duration = 1000 }) });
            api.Setup(x => x.GetLyricVo("internal", "same", false)).Returns(() =>
            {
                calls++;
                return new ResultVo<LyricVo>(new() { Lyric = $"[00:00.000]{source}" });
            });
            return api.Object;
        }
        var matcher = new LocalSongMatcher(Api, TimeSpan.Zero);
        var candidate = new LocalSongCandidate(SearchSourceEnum.NET_EASE_MUSIC, "same", "", "", "", 1000, 100, true);
        var net = await matcher.GetLyricsAsync(candidate, false, Token);
        Assert.Same(net, await matcher.GetLyricsAsync(candidate, false, Token));
        var qq = await matcher.GetLyricsAsync(candidate with { Source = SearchSourceEnum.QQ_MUSIC }, false, Token);
        Assert.NotEqual(net.Lyric, qq.Lyric);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task WriterPreservesLocalNamesHierarchyEncodingAndExistingLyrics()
    {
        using var ws = new TestWorkspace();
        var audio = Path.Combine(ws.Root, "album", "01 - local.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(audio)!);
        WriteWave(audio);
        var output = Path.Combine(ws.Root, "output");
        var options = new LocalSaveOptions(ws.Root, output, false, false);
        ws.Settings.Param.Encoding = OutputEncodingEnum.UTF_8_BOM;
        var writer = new LocalLyricWriter();
        var result = await writer.WriteAsync(audio, TestWorkspace.Song().LyricVo, ws.Settings, options, Token);
        var target = Assert.Single(result.Paths);
        Assert.Equal(Path.Combine(output, "album", "01 - local.lrc"), target);
        Assert.True(File.ReadAllBytes(target).AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        File.WriteAllText(target, "keep existing");
        Assert.True((await writer.WriteAsync(audio, TestWorkspace.Song().LyricVo, ws.Settings, options, Token)).Skipped);
        Assert.Equal("keep existing", File.ReadAllText(target));
        await writer.WriteAsync(audio, TestWorkspace.Song().LyricVo, ws.Settings, options with { Overwrite = true }, Token);
        Assert.Contains("测试歌词", File.ReadAllText(target));
        Assert.Empty(Directory.GetFiles(output, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task WriterSupportsSrtAndCancellationWithoutTouchingDestination()
    {
        using var ws = new TestWorkspace();
        var audio = Path.Combine(ws.Root, "song.mp3");
        var options = new LocalSaveOptions(ws.Root, "", true, true);
        ws.Settings.Param.OutputFileFormat = OutputFormatEnum.SRT;
        var writer = new LocalLyricWriter();
        var result = await writer.WriteAsync(audio, TestWorkspace.Song().LyricVo, ws.Settings, options, Token);
        var target = Assert.Single(result.Paths);
        Assert.EndsWith("song.srt", target);
        Assert.Contains("-->", File.ReadAllText(target));
        var bytes = File.ReadAllBytes(target);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(audio, TestWorkspace.Song().LyricVo, ws.Settings, options, cancelled.Token));
        Assert.Equal(bytes, File.ReadAllBytes(target));
        Assert.Throws<IOException>(() => LocalLyricWriter.OutputStem(Path.Combine(ws.Root, "..", "outside.mp3"), options));
    }

    [AvaloniaFact]
    public async Task ScanMatchPreviewSaveKeepsTwoLocalCopiesAndMainSearchSettings()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Param.SearchType = SearchTypeEnum.PLAYLIST_ID;
        Directory.CreateDirectory(Path.Combine(ws.Root, "a"));
        Directory.CreateDirectory(Path.Combine(ws.Root, "b"));
        foreach (var dir in new[] { "a", "b" }) File.WriteAllText(Path.Combine(ws.Root, dir, "local.flac"), "fake audio");
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search("园游会 周杰伦", SearchTypeEnum.SONG_ID)).Returns(Results(Candidate()));
        var song = TestWorkspace.Song("186008");
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["186008"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("186008", "186008", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, vm.Items.Count);
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        Assert.All(vm.Items, x => Assert.Equal(LocalScanState.Ready, x.State));
        vm.SelectedItem = vm.Items[0];
        await vm.PreviewCommand.ExecuteAsync(null);
        Assert.Contains("测试歌词", vm.Preview);
        await vm.SaveSelectedCommand.ExecuteAsync(null);
        Assert.All(vm.Items, x => Assert.Equal(LocalScanState.Saved, x.State));
        Assert.Equal(2, Directory.GetFiles(ws.Root, "*.lrc", SearchOption.AllDirectories).Length);
        Assert.Equal(SearchTypeEnum.PLAYLIST_ID, ws.Settings.Param.SearchType);
        api.Verify(x => x.GetLyricVo("186008", "186008", false), Times.Once);
    }

    [AvaloniaFact]
    public async Task AmbiguousMatchesCannotBeSavedUntilConfirmed()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "local.flac"), "fake");
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search(It.IsAny<string>(), SearchTypeEnum.SONG_ID)).Returns(Results(Candidate(), Candidate("another")));
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root, UseFallback = false };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        var row = Assert.Single(vm.Items);
        Assert.Equal(LocalScanState.Review, row.State);
        vm.SelectedItem = row;
        row.Candidate = row.Candidates[0];
        await vm.SaveSelectedCommand.ExecuteAsync(null);
        Assert.Empty(Directory.GetFiles(ws.Root, "*.lrc"));
        vm.ConfirmCandidateCommand.Execute(null);
        Assert.Equal(LocalScanState.Ready, row.State);
        row.Candidate = row.Candidates[1];
        Assert.Equal(LocalScanState.Review, row.State);
        row.Keyword = "corrected title";
        Assert.Null(row.Candidate);
        Assert.Empty(row.Candidates);
    }

    [AvaloniaFact]
    public void LocalPageHasBoundControlsAndLocalizedTitle()
    {
        using var ws = new TestWorkspace();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => new Mock<IMusicApi>().Object);
        var view = new LocalMusicScanView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Same(vm.ScanCommand, view.FindControl<TKCode.Arc.Button>("StartScanButton")!.Command);
            Assert.Same(vm.SaveSelectedCommand, view.FindControl<TKCode.Arc.Button>("SaveLocalLyricsButton")!.Command);
            Assert.NotNull(view.FindControl<TKCode.Arc.Select>("CandidatePicker"));
            Assert.Contains(view.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == LocalizationService.Get("Nav.Local") && t.IsEffectivelyVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task CancelDuringRequestKeepsRowsAndDoesNotStartNextRequest()
    {
        using var ws = new TestWorkspace();
        foreach (var name in new[] { "a.flac", "b.flac" }) File.WriteAllText(Path.Combine(ws.Root, name), "fake");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search(It.IsAny<string>(), SearchTypeEnum.SONG_ID)).Returns(() =>
        {
            started.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException();
            return Results(Candidate());
        });
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        var matching = vm.MatchSelectedCommand.ExecuteAsync(null);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.True(vm.IsBusy);
            vm.CancelCommand.Execute(null);
        }
        finally { release.Set(); }
        await matching;
        Assert.False(vm.IsBusy);
        Assert.Equal(2, vm.Items.Count);
        Assert.All(vm.Items, row => Assert.Equal(LocalScanState.Pending, row.State));
        Assert.Equal(LocalizationService.Get("LocalScan.Cancelled"), vm.Message);
        api.Verify(x => x.Search(It.IsAny<string>(), SearchTypeEnum.SONG_ID), Times.Once);
    }

    [AvaloniaFact]
    public async Task ExistingLyricsSkipNetworkAndSameStemCannotOverwriteAnotherAudioTask()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "same.flac"), "fake");
        File.WriteAllText(Path.Combine(ws.Root, "same.mp3"), "fake");
        var lyricsPath = Path.Combine(ws.Root, "same.lrc");
        File.WriteAllText(lyricsPath, "existing");
        var api = SongApi();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        Assert.All(vm.Items, row => Assert.Equal(LocalScanState.Skipped, row.State));
        api.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<SearchTypeEnum>()), Times.Never);
        Assert.Equal("existing", File.ReadAllText(lyricsPath));
        vm.Overwrite = true;
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        await vm.SaveSelectedCommand.ExecuteAsync(null);
        Assert.Single(vm.Items, row => row.State == LocalScanState.Saved);
        Assert.Equal(LocalizationService.Get("LocalScan.OutputCollision"), Assert.Single(vm.Items, row => row.State == LocalScanState.Failed).Detail);
    }

    [AvaloniaFact]
    public async Task SaveFailureCanRetryWithoutLosingConfirmedCandidate()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "song.flac"), "fake");
        var blockedOutput = Path.Combine(ws.Root, "not-a-directory");
        File.WriteAllText(blockedOutput, "blocks directory creation");
        var api = SongApi();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero)
        { FolderPath = ws.Root, BesideAudio = false, OutputFolder = blockedOutput };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        await vm.SaveSelectedCommand.ExecuteAsync(null);
        var row = Assert.Single(vm.Items);
        Assert.Equal(LocalScanState.Failed, row.State);
        Assert.True(row.RetrySave);
        var candidate = row.Candidate;
        vm.OutputFolder = Path.Combine(ws.Root, "recovered");
        await vm.RetryCommand.ExecuteAsync(null);
        Assert.Equal(LocalScanState.Saved, row.State);
        Assert.Same(candidate, row.Candidate);
        Assert.True(File.Exists(Path.Combine(vm.OutputFolder, "song.lrc")));
        api.Verify(x => x.Search(It.IsAny<string>(), SearchTypeEnum.SONG_ID), Times.Once);
    }

    [Fact]
    public async Task SeparatedLyricsProduceCompleteFilesWithLocalStem()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.OutputLyricTypes = "0,1";
        ws.Settings.Config.SeparateFileForIsolated = true;
        ws.Settings.Param.ShowLrcType = ShowLrcTypeEnum.ISOLATED;
        var lyrics = TestWorkspace.Song().LyricVo;
        lyrics.TranslateLyric = "[00:01.000]translated";
        var options = new LocalSaveOptions(ws.Root, "", true, false);
        var result = await new LocalLyricWriter().WriteAsync(Path.Combine(ws.Root, "local.flac"), lyrics, ws.Settings, options, Token);
        Assert.Equal(2, result.Paths.Count);
        Assert.Equal(new[] { "local-0.lrc", "local-1.lrc" }, result.Paths.Select(Path.GetFileName));
        Assert.Contains("translated", File.ReadAllText(result.Paths[1]));
        Assert.True(LocalLyricWriter.HasExistingLyrics(Path.Combine(ws.Root, "local.flac"), options, ws.Settings));
    }

    private static Mock<IMusicApi> SongApi()
    {
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search(It.IsAny<string>(), SearchTypeEnum.SONG_ID)).Returns(Results(Candidate()));
        var song = TestWorkspace.Song("186008");
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["186008"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("186008", "186008", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        return api;
    }

    [AvaloniaFact]
    public void LargeFileListsVirtualizeAndKeepSelectionBound()
    {
        using var ws = new TestWorkspace();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => new Mock<IMusicApi>().Object);
        for (var i = 0; i < 2000; i++) vm.Items.Add(new LocalScanItemViewModel(Audio($"{i}.flac")));
        var view = new LocalMusicScanView();
        var window = TestWorkspace.Host(view, vm);
        window.Width = 840; window.Height = 720;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var list = view.FindControl<ListBox>("LocalFileList")!;
            Assert.InRange(list.GetRealizedContainers().Count(), 1, 50);
            list.SelectedItem = vm.Items[1999];
            list.ScrollIntoView(1999);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(vm.Items[1999], vm.SelectedItem);
            Assert.InRange(list.GetRealizedContainers().Count(), 1, 50);
            Assert.NotNull(list.ContainerFromItem(vm.Items[1999]));
        }
        finally { window.Close(); }
    }

    [Fact]
    public void MetadataAndCandidateLabelsSkipMissingParts()
    {
        var row = new LocalScanItemViewModel(new LocalAudioFile("1984.wav", "1984", "", "", 1000));
        Assert.Equal("1984 · 00:01", row.Metadata);
        Assert.Equal("1984", new LocalScanItemViewModel(new LocalAudioFile("1984.wav", "1984", "", "", 0)).Metadata);
        var candidate = new LocalSongCandidate(SearchSourceEnum.QQ_MUSIC, "97773", "晴天", "周杰伦", "", 269000, 0, false);
        Assert.DoesNotContain("·  ·", candidate.ToString());
        Assert.EndsWith("晴天 — 周杰伦 · 04:29 · 97773", candidate.ToString());
    }

    [AvaloniaFact]
    public async Task ReviewPaneShowsEmptyStateUntilAFileIsSelectedAndPreviewKeepsBatchProgress()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "local.flac"), "fake");
        var api = SongApi();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root };
        var view = new LocalMusicScanView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(view.FindControl<TKCode.Arc.EmptyState>("LocalFilesEmptyState")!.IsEffectivelyVisible);
            Assert.True(view.FindControl<TKCode.Arc.EmptyState>("ReviewEmptyState")!.IsEffectivelyVisible);
            Assert.False(view.FindControl<TKCode.Arc.Input>("MatchKeyword")!.IsEffectivelyVisible);

            await vm.ScanCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.False(view.FindControl<TKCode.Arc.EmptyState>("LocalFilesEmptyState")!.IsEffectivelyVisible);
            Assert.True(view.FindControl<TKCode.Arc.Input>("MatchKeyword")!.IsEffectivelyVisible);
            // A hidden ScrollViewer has not templated its content, so assert on the scroller itself.
            Assert.False(view.FindControl<ScrollViewer>("LyricsPreviewScroll")!.IsEffectivelyVisible);

            await vm.MatchSelectedCommand.ExecuteAsync(null);
            Assert.Equal((1, 1), (vm.Completed, vm.Total));
            await vm.PreviewCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal((1, 1), (vm.Completed, vm.Total));
            Assert.True(view.FindControl<ScrollViewer>("LyricsPreviewScroll")!.IsEffectivelyVisible);
            Assert.True(view.FindControl<TKCode.Arc.CodeBlock>("LyricsPreview")!.IsEffectivelyVisible);

            vm.SelectedItem = null;
            Dispatcher.UIThread.RunJobs();
            Assert.True(view.FindControl<TKCode.Arc.EmptyState>("ReviewEmptyState")!.IsEffectivelyVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task MatchingIntoAnOutputFolderRequiresTheFolderBeforeAnyRequest()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "local.flac"), "fake");
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero)
        { FolderPath = ws.Root, BesideAudio = false, OutputFolder = "" };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        Assert.Equal(LocalizationService.Get("LocalScan.InvalidFolder"), vm.Message);
        Assert.Equal(LocalScanState.Pending, Assert.Single(vm.Items).State);
        api.VerifyNoOtherCalls();
    }

    [AvaloniaFact]
    public void LocalMusicIsItsOwnSidebarPageAndNotADownloadManagerEntry()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var sidebar = Assert.Single(window.GetVisualDescendants().OfType<TKCode.Arc.Sidebar>());
            Assert.Contains(sidebar.Sections.SelectMany(x => x.Items), x => x.Key == MainWindowViewModel.PageLocal);
            var page = window.FindControl<LocalMusicScanView>("LocalPage")!;
            Assert.False(page.IsVisible);
            Assert.Null(vm.LocalMusicScanViewModel);

            vm.CurrentPageKey = MainWindowViewModel.PageBatch;
            Dispatcher.UIThread.RunJobs();
            var batch = window.FindControl<BatchSearchView>("BatchPage")!;
            Assert.Null(batch.FindControl<TKCode.Arc.Button>("ScanFolderButton"));

            sidebar.SelectedKey = MainWindowViewModel.PageLocal;
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.IsLocalPage);
            Assert.True(page.IsEffectivelyVisible);
            var local = Assert.IsType<LocalMusicScanViewModel>(vm.LocalMusicScanViewModel);
            Assert.Same(local, page.DataContext);

            // The page keeps its state across navigation, and its messages reach the window as toasts.
            var tips = new List<string>();
            vm.TipRaised += (message, _) => tips.Add(message);
            local.ReportError("folder picker failed");
            Assert.Equal(["folder picker failed"], tips);
            local.FolderPath = ws.Root;
            vm.CurrentPageKey = MainWindowViewModel.PageSearch;
            Dispatcher.UIThread.RunJobs();
            var g = MusicLyricApp.Core.AppShortcuts.Get(MusicLyricApp.Core.AppShortcuts.GoLocal).Gesture;
            var raw = g.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta) ? Avalonia.Input.RawInputModifiers.Meta : Avalonia.Input.RawInputModifiers.Control;
            Avalonia.Headless.HeadlessWindowExtensions.KeyPress(window, g.Key, raw, Avalonia.Input.PhysicalKey.None, null);
            Avalonia.Headless.HeadlessWindowExtensions.KeyRelease(window, g.Key, raw, Avalonia.Input.PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageLocal, vm.CurrentPageKey);
            Assert.Same(local, vm.LocalMusicScanViewModel);
            Assert.Equal(ws.Root, local.FolderPath);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public async Task EachOperationUsesTheCurrentSettingsWithoutChangingThem()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "local.flac"), "fake");
        var api = SongApi();
        using var vm = new LocalMusicScanViewModel(ws.Settings, _ => api.Object, new Reader(p => Audio(p)), TimeSpan.Zero) { FolderPath = ws.Root };
        await vm.ScanCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        await vm.MatchSelectedCommand.ExecuteAsync(null);
        // Changed on the settings page after the local page was first opened.
        ws.Settings.Param.OutputFileFormat = OutputFormatEnum.SRT;
        await vm.SaveSelectedCommand.ExecuteAsync(null);
        Assert.Equal(LocalScanState.Saved, Assert.Single(vm.Items).State);
        Assert.True(File.Exists(Path.Combine(ws.Root, "local.srt")));
        Assert.Equal(OutputFormatEnum.SRT, ws.Settings.Param.OutputFileFormat);
    }

    private static void WriteWave(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII);
        const int dataSize = 16000;
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000);
        writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataSize); writer.Write(new byte[dataSize]);
    }
}
