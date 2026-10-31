using System.Net.Http;
using Avalonia.Media;
using Moq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricAppTest.Regression;

public class AdditionalFeatureRegressionTests
{
    [Theory]
    [InlineData(VerbatimLyricModeEnum.STANDARD_MODE, "[00:01.000]你[00:01.500]好[00:02.000]")]
    [InlineData(VerbatimLyricModeEnum.A2_MODE, "[00:01.000]<00:01.000>你<00:01.500>好<00:02.000>")]
    public async Task NetEaseWordTimingsRenderInBothSupportedModes(VerbatimLyricModeEnum mode, string expected)
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        settings.Config.VerbatimLyricMode = mode;
        var normalized = VerbatimLyricUtils.DealVerbatimLyric4NetEaseMusic("[1000,1000](1000,500,0)你(1500,500,0)好");
        var output = await LyricUtils.GetOutputContent(new LyricVo { Lyric = normalized }, settings);
        Assert.Equal(expected, Assert.Single(output));
    }

    [Fact]
    public void QQWordTimingsConvertToOrdinaryAndVerbatimLyrics()
    {
        const string input = "[1000,1000]你(1000,500)好(1500,500)";
        Assert.Equal("[00:01.000]你好" + Environment.NewLine + "[00:02.000]" + Environment.NewLine,
            VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4QQMusic(input));
        Assert.Equal("[00:01.000]你[00:01.500]好[00:02.000]" + Environment.NewLine, VerbatimLyricUtils.DealVerbatimLyric4QQMusic(input));
    }

    [Fact]
    public async Task PinyinOutputPreservesTimestamps()
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "5";
        var output = await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]我们一起唱歌快乐的生活" }, settings);
        var text = Assert.Single(output);
        Assert.StartsWith("[00:01.000]", text);
        Assert.DoesNotContain("我们", text);
        Assert.Contains("chàng gē", text);
    }

    [Theory]
    [InlineData(false, false, "Red")]
    [InlineData(true, false, "Chocolate")]
    [InlineData(true, true, "LimeGreen")]
    public void AvailabilityLampReflectsMissingTranslations(bool original, bool translated, string color)
    {
        var settings = new SettingBean();
        var song = TestWorkspace.Song(lyric: original ? "original" : "");
        song.LyricVo.TranslateLyric = translated ? "translation" : "";
        var vm = new SignalLampViewModel();
        vm.UpdateLampInfo(new() { ["123"] = new(song) }, settings);
        Assert.Equal(Color.Parse(color), Assert.IsAssignableFrom<ISolidColorBrush>(vm.LampColor).Color);
        Assert.Contains(original ? "原文: 1" : "原文: 0", vm.Details);
    }

    [Fact]
    public async Task SaveWithExtraDownloadsWritesLyricsCoverAndAudio()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.DownloadCoverAndSongLinkOnSave = true;
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongLink("123")).Returns(new ResultVo<string>("https://test.invalid/song.mp3"));
        byte[] payload = [1, 2, 3, 4];
        var handler = new ResponseHandler(_ => ResponseHandler.Bytes(payload));
        var storage = ws.Storage(_ => new HttpClient(handler, false));
        storage.SetSearchService(ws.Search(api));
        await storage.SaveResult(TestWorkspace.Result(TestWorkspace.Song()), ws.Settings, ws.Window.Object);
        Assert.Equal(3, ws.CreatedFiles.Count);
        Assert.Single(ws.CreatedFiles, x => x.EndsWith(".lrc"));
        Assert.Equal(payload, File.ReadAllBytes(Assert.Single(ws.CreatedFiles, x => x.EndsWith("-cover.png"))));
        Assert.Equal(payload, File.ReadAllBytes(Assert.Single(ws.CreatedFiles, x => x.EndsWith(".mp3"))));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task OptionalDownloadFailureDoesNotLoseSavedLyrics()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.DownloadCoverAndSongLinkOnSave = true;
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongLink("123")).Returns(ResultVo<string>.Failure("unavailable"));
        var storage = ws.Storage(_ => new HttpClient(new ResponseHandler(_ => throw new HttpRequestException("offline"))));
        storage.SetSearchService(ws.Search(api));
        Assert.Equal("保存完毕，成功 1 跳过 0", await storage.SaveResult(TestWorkspace.Result(TestWorkspace.Song()), ws.Settings, ws.Window.Object));
        Assert.Equal("[00:01.000]测试歌词", File.ReadAllText(Assert.Single(ws.CreatedFiles, x => x.EndsWith(".lrc"))));
    }

    [Fact]
    public void StandardAndA2ModesShareRawWordTimingsCache()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.VerbatimLyricMode = VerbatimLyricModeEnum.STANDARD_MODE;
        LocalSongCacheService.SaveCache(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", TestWorkspace.Song());
        ws.Settings.Config.VerbatimLyricMode = VerbatimLyricModeEnum.A2_MODE;
        Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", out _));
        ws.Settings.Config.VerbatimLyricMode = VerbatimLyricModeEnum.DISABLE;
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", out _));
    }

    [Fact]
    public void ReleaseDescriptionIncludesVersionAuthorAndNotes()
    {
        var text = UpdateCheckService.BuildReleaseDescription(new GitHubInfo
        {
            TagName = "v7.3", Author = new Author { Login = "tester" }, Body = "release notes",
            PublishedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), Assets = [new Asset { DownloadCount = 42 }]
        });
        Assert.Contains("v7.3", text);
        Assert.Contains("tester", text);
        Assert.Contains("42", text);
        Assert.Contains("release notes", text);
    }
}
