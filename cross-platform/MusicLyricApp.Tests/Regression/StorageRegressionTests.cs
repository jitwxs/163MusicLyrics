using System.Net;
using System.Net.Http;
using Moq;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class StorageRegressionTests
{
    [Theory]
    [InlineData(OutputFormatEnum.LRC, OutputEncodingEnum.UTF_8, ".lrc")]
    [InlineData(OutputFormatEnum.LRC, OutputEncodingEnum.UTF_8_BOM, ".lrc")]
    [InlineData(OutputFormatEnum.LRC, OutputEncodingEnum.UTF_32, ".lrc")]
    [InlineData(OutputFormatEnum.LRC, OutputEncodingEnum.UNICODE, ".lrc")]
    [InlineData(OutputFormatEnum.SRT, OutputEncodingEnum.UTF_8, ".srt")]
    public async Task SavesActualFilesInConfiguredFormatAndEncoding(OutputFormatEnum format, OutputEncodingEnum encoding, string ext)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Param.OutputFileFormat = format;
        ws.Settings.Param.Encoding = encoding;
        var song = TestWorkspace.Song();
        Assert.Equal("保存完毕，成功 1 跳过 0", await ws.Storage().SaveResult(TestWorkspace.Result(song), ws.Settings, ws.Window.Object));
        var file = Assert.Single(ws.CreatedFiles);
        Assert.EndsWith(ext, file);
        var enc = GlobalUtils.GetEncoding(encoding);
        var expected = Assert.Single(await LyricUtils.GetOutputContent(song.LyricVo, ws.Settings));
        Assert.Equal(enc.GetPreamble().Concat(enc.GetBytes(expected)).ToArray(), File.ReadAllBytes(file));
    }

    [Theory]
    [InlineData(FileConflictStrategyEnum.OVERWRITE, " ({n})", "测试歌曲123 - 歌手甲,歌手乙.lrc")]
    [InlineData(FileConflictStrategyEnum.APPEND_NUMBER, " ({n})", "测试歌曲123 - 歌手甲,歌手乙 (1).lrc")]
    [InlineData(FileConflictStrategyEnum.APPEND_NUMBER, "_copy", "测试歌曲123 - 歌手甲,歌手乙_copy1.lrc")]
    [InlineData(FileConflictStrategyEnum.APPEND_NUMBER, "", "测试歌曲123 - 歌手甲,歌手乙 (1).lrc")]
    public async Task ExistingFileConflictPolicy(FileConflictStrategyEnum strategy, string suffix, string expected)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.FileConflictStrategy = strategy;
        ws.Settings.Config.FileConflictSuffixPattern = suffix;
        var original = Path.Combine(ws.Root, "测试歌曲123 - 歌手甲,歌手乙.lrc");
        File.WriteAllText(original, "old contents which are longer than replacement");
        await ws.Storage().SaveResult(TestWorkspace.Result(TestWorkspace.Song()), ws.Settings, ws.Window.Object);
        Assert.Equal(expected, Path.GetFileName(Assert.Single(ws.CreatedFiles)));
        Assert.Equal("[00:01.000]测试歌词", File.ReadAllText(ws.CreatedFiles[0]));
        if (strategy == FileConflictStrategyEnum.APPEND_NUMBER) Assert.StartsWith("old", File.ReadAllText(original));
    }

    [Fact]
    public async Task SingleSaveSkipsPureMusicAndEmptyLyrics()
    {
        using var ws = new TestWorkspace();
        foreach (var text in new[] { "", "[00:00.000]纯音乐，请欣赏" })
            await Assert.ThrowsAsync<MusicLyricException>(() => ws.Storage().SaveResult(TestWorkspace.Result(TestWorkspace.Song(lyric: text)), ws.Settings, ws.Window.Object));
        Assert.Empty(ws.CreatedFiles);
        ws.Settings.Config.IgnorePureMusicInSave = false;
        await ws.Storage().SaveResult(TestWorkspace.Result(TestWorkspace.Song(lyric: "[00:00.000]纯音乐，请欣赏")), ws.Settings, ws.Window.Object);
        Assert.Single(ws.CreatedFiles);
    }

    [Fact]
    public async Task BatchSaveReportsSuccessAndSkippedItems()
    {
        using var ws = new TestWorkspace();
        var result = TestWorkspace.Result(TestWorkspace.Song(), TestWorkspace.Song("456", ""));
        Assert.Equal("保存完毕，成功 1 跳过 1", await ws.Storage().SaveResult(result, ws.Settings, ws.Window.Object));
        Assert.Single(ws.CreatedFiles);
        Assert.Contains("456", result.ConsoleOutput);
        Assert.Contains(ErrorMsgConst.LRC_NOT_EXIST, result.ConsoleOutput);
    }

    [Fact]
    public async Task SeparateLanguagesProduceSeparateFiles()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.OutputLyricTypes = "0,1";
        ws.Settings.Config.SeparateFileForIsolated = true;
        ws.Settings.Param.ShowLrcType = ShowLrcTypeEnum.ISOLATED;
        var song = TestWorkspace.Song();
        song.LyricVo.TranslateLyric = "[00:01.000]translated";
        await ws.Storage().SaveResult(TestWorkspace.Result(song), ws.Settings, ws.Window.Object);
        Assert.Equal(2, ws.CreatedFiles.Count);
        Assert.EndsWith("-0.lrc", ws.CreatedFiles[0]);
        Assert.EndsWith("-1.lrc", ws.CreatedFiles[1]);
        Assert.Equal("[00:01.000]translated", File.ReadAllText(ws.CreatedFiles[1]));
    }

    [Fact]
    public async Task CancelledFolderSelectionDoesNotWrite()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.LastSaveFolderPath = "";
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<Avalonia.Platform.Storage.FolderPickerOpenOptions>()))
            .ReturnsAsync(Array.Empty<Avalonia.Platform.Storage.IStorageFolder>());
        await Assert.ThrowsAsync<MusicLyricException>(() => ws.Storage().SaveResult(TestWorkspace.Result(TestWorkspace.Song()), ws.Settings, ws.Window.Object));
        Assert.Empty(ws.CreatedFiles);
    }

    [Fact]
    public async Task SelectingFolderPersistsItInIsolatedConfig()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.LastSaveFolderPath = "";
        await ws.Storage().SelectFolderAndRememberAsync(ws.Settings, ws.Window.Object);
        Assert.Equal(ws.Root, ws.Storage().ReadAppConfig().Config.LastSaveFolderPath.TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public void CopyCoverAndAudioLinksUseClipboard()
    {
        using var ws = new TestWorkspace();
        var song = TestWorkspace.Song();
        var storage = ws.Storage();
        Assert.Equal(ErrorMsgConst.SONG_PIC_GET_SUCCESS, storage.SaveSongPic(TestWorkspace.Result(song), ws.Settings, ws.Window.Object));
        Assert.Equal(song.SongVo.Pics, ws.Clipboard);
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongLink("123")).Returns(new ResultVo<string>("https://test.invalid/song.mp3"));
        storage.SetSearchService(ws.Search(api));
        Assert.Equal(ErrorMsgConst.SONG_URL_GET_SUCCESS, storage.SaveSongLink(TestWorkspace.Result(song), ws.Settings, ws.Window.Object));
        Assert.Equal("https://test.invalid/song.mp3", ws.Clipboard);
    }

    [Fact]
    public void BatchCoverLinksAppearInConsole()
    {
        using var ws = new TestWorkspace();
        var result = TestWorkspace.Result(TestWorkspace.Song(), TestWorkspace.Song("456"));
        ws.Storage().SaveSongPic(result, ws.Settings, ws.Window.Object);
        Assert.Contains("picLink", result.ConsoleOutput);
        Assert.Contains("123", result.ConsoleOutput);
        Assert.Contains("456", result.ConsoleOutput);
    }

    [Theory]
    [InlineData("https://test.invalid/song.flac", "audio/mpeg", ".flac")]
    [InlineData("https://test.invalid/stream", "audio/mp4", ".m4a")]
    [InlineData("https://test.invalid/stream", "audio/ogg", ".ogg")]
    [InlineData("https://test.invalid/stream", "audio/mpeg", ".mp3")]
    public async Task DownloadAudioWritesExactResponseBytes(string url, string mime, string extension)
    {
        using var ws = new TestWorkspace();
        byte[] payload = [1, 2, 3, 0, 255];
        var handler = new ResponseHandler(_ => ResponseHandler.Bytes(payload, mime));
        var storage = ws.Storage(_ => new HttpClient(handler, false));
        var result = TestWorkspace.Result(TestWorkspace.Song());
        result.SongLink = url;
        await storage.DownloadSongLink(result, ws.Settings, ws.Window.Object);
        Assert.EndsWith(extension, Assert.Single(ws.CreatedFiles));
        Assert.Equal(payload, File.ReadAllBytes(ws.CreatedFiles[0]));
        Assert.Equal(url, Assert.Single(handler.Requests).AbsoluteUri);
    }

    [Fact]
    public async Task DownloadCoverWritesExactBytes()
    {
        using var ws = new TestWorkspace();
        byte[] payload = [137, 80, 78, 71];
        var storage = ws.Storage(_ => new HttpClient(new ResponseHandler(_ => ResponseHandler.Bytes(payload, "image/png"))));
        await storage.DownloadSongPic(TestWorkspace.Result(TestWorkspace.Song()), ws.Settings, ws.Window.Object);
        Assert.EndsWith("-cover.png", Assert.Single(ws.CreatedFiles));
        Assert.Equal(payload, File.ReadAllBytes(ws.CreatedFiles[0]));
    }

    [Fact]
    public async Task FailedAudioDownloadIsReportedAndDoesNotCreateFile()
    {
        using var ws = new TestWorkspace();
        var storage = ws.Storage(_ => new HttpClient(new ResponseHandler(_ => ResponseHandler.Bytes([], code: HttpStatusCode.NotFound))));
        var result = TestWorkspace.Result(TestWorkspace.Song());
        result.SongLink = "https://test.invalid/missing";
        await Assert.ThrowsAsync<HttpRequestException>(() => storage.DownloadSongLink(result, ws.Settings, ws.Window.Object));
        Assert.Empty(ws.CreatedFiles);
    }

    [Fact]
    public async Task NoSearchResultProducesUserFacingErrors()
    {
        using var ws = new TestWorkspace();
        var storage = ws.Storage();
        await Assert.ThrowsAsync<MusicLyricException>(() => storage.SaveResult(TestWorkspace.Result(), ws.Settings, ws.Window.Object));
        await Assert.ThrowsAsync<MusicLyricException>(() => storage.DownloadSongLink(TestWorkspace.Result(), ws.Settings, ws.Window.Object));
        await Assert.ThrowsAsync<MusicLyricException>(() => storage.DownloadSongPic(TestWorkspace.Result(), ws.Settings, ws.Window.Object));
        Assert.Throws<MusicLyricException>(() => storage.SaveSongPic(TestWorkspace.Result(), ws.Settings, ws.Window.Object));
    }

    [Fact]
    public void OutputFilenameExpandsVariablesAndLimitsSingers()
    {
        var song = TestWorkspace.Song();
        Assert.Equal("001_123_测试歌曲123_歌手甲_测试专辑", GlobalUtils.GetOutputName(song, "$fillLength(${index},0,3)_${id}_${name}_${singer}_${album}", "+", 1));
    }
}
