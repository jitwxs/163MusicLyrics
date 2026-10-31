using Moq;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class SearchRegressionTests
{
    [Theory]
    [InlineData("123", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID, "123")]
    [InlineData("003AbC", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID, "003AbC")]
    [InlineData("https://music.163.com/#/song?id=123&userid=9", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID, "123")]
    [InlineData("https://music.163.com/album?id=456", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.ALBUM_ID, "456")]
    [InlineData("https://music.163.com/playlist?id=789", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.PLAYLIST_ID, "789")]
    [InlineData("https://y.qq.com/n/ryqq/songDetail/003AbC", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID, "003AbC")]
    [InlineData("https://y.qq.com/n/ryqq/albumDetail/003AbC", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.ALBUM_ID, "003AbC")]
    [InlineData("https://y.qq.com/n/ryqq/playlist/123", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.PLAYLIST_ID, "123")]
    public void ParsesSupportedIdsAndLinks(string text, SearchSourceEnum source, SearchTypeEnum type, string expected)
    {
        var actual = GlobalUtils.CheckInputId(text, source, SearchTypeEnum.SONG_ID);
        Assert.Equal(expected, actual.QueryId);
        Assert.Equal(source, actual.SearchSource);
        Assert.Equal(type, actual.SearchType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-song")]
    [InlineData("https://music.163.com/song?id=")]
    public void InvalidInputIsRejected(string text) => Assert.Throws<MusicLyricException>(() =>
        GlobalUtils.CheckInputId(text, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));

    [Fact]
    public void LinkOverridesSelectedProvider()
    {
        var input = GlobalUtils.CheckInputId("https://y.qq.com/n/ryqq/songDetail/003AbC", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.PLAYLIST_ID);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, input.SearchSource);
        Assert.Equal(SearchTypeEnum.SONG_ID, input.SearchType);
    }

    [Fact]
    public void CommaInputReplacesPreviousSelectionAndIgnoresEmptyTokens()
    {
        using var ws = new TestWorkspace();
        var service = ws.Search(new(MockBehavior.Strict));
        var param = ws.Param(" 123, ,456,, ");
        param.SongIds.Add(TestWorkspace.Input("999"));
        service.InitSongIds(param, ws.Settings);
        Assert.Equal(new[] { "123", "456" }, param.SongIds.Select(x => x.SongId));
    }

    [Fact]
    public void DirectoryScanAcceptsLegalFilenamesOnly()
    {
        using var ws = new TestWorkspace();
        File.WriteAllText(Path.Combine(ws.Root, "123.mp3"), "");
        File.WriteAllText(Path.Combine(ws.Root, "456.flac"), "");
        File.WriteAllText(Path.Combine(ws.Root, "readme.txt"), "");
        var param = ws.Param(ws.Root);
        ws.Search(new(MockBehavior.Strict)).InitSongIds(param, ws.Settings);
        Assert.Equal(new[] { "123", "456" }, param.SongIds.Select(x => x.SongId).Order());
    }

    [Theory]
    [InlineData(SearchTypeEnum.ALBUM_ID)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID)]
    public void CollectionExpandsIntoSongs(SearchTypeEnum type)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Param.SearchType = type;
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        SimpleSongVo[] songs = [new() { DisplayId = "11" }, new() { DisplayId = "22" }];
        if (type == SearchTypeEnum.ALBUM_ID)
            api.Setup(x => x.GetAlbumVo("123")).Returns(new ResultVo<AlbumVo>(new() { SimpleSongVos = songs }));
        else
            api.Setup(x => x.GetPlaylistVo("123")).Returns(new ResultVo<PlaylistVo>(new() { SimpleSongVos = songs }));
        var param = ws.Param("123");
        ws.Search(api).InitSongIds(param, ws.Settings);
        Assert.Equal(new[] { "11", "22" }, param.SongIds.Select(x => x.SongId));
        Assert.All(param.SongIds, x => Assert.Equal("123", x.QueryId));
        api.VerifyAll();
    }

    [Fact]
    public void MissingAlbumPropagatesReadableError()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Param.SearchType = SearchTypeEnum.ALBUM_ID;
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetAlbumVo("123")).Returns(ResultVo<AlbumVo>.Failure("missing album"));
        Assert.Equal("missing album", Assert.Throws<MusicLyricException>(() => ws.Search(api).InitSongIds(ws.Param("123"), ws.Settings)).Message);
    }

    [Fact]
    public async Task SearchRendersMetadataAndReusesDiskCacheWithoutNetwork()
    {
        using var ws = new TestWorkspace();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        var song = TestWorkspace.Song();
        api.Setup(x => x.GetSongVo(It.Is<string[]>(ids => ids.SequenceEqual(new[] { "123" }))))
            .Returns(new Dictionary<string, ResultVo<SongVo>> { ["123"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("123", "123", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        var service = ws.Search(api);
        var param = ws.Param("123");
        service.InitSongIds(param, ws.Settings);
        var result = service.SearchSongs(param.SongIds, ws.Settings);
        Assert.True(result["123"].IsSuccess());
        Assert.Equal(0, service.LastLocalCacheHitCount);
        var view = TestWorkspace.Result();
        await service.RenderSearchResult(param, view, ws.Settings, result);
        Assert.Equal("测试歌曲123", view.SongName);
        Assert.Equal("歌手甲,歌手乙", view.Singer);
        Assert.Equal("01:05", view.SongDuration);
        Assert.Equal("2020-01-01", view.PublishDate);
        Assert.Equal("网易云", view.SongSource);
        Assert.Equal("[00:01.000]测试歌词", view.ConsoleOutput);
        var offline = ws.Search(new(MockBehavior.Strict));
        Assert.True(offline.SearchSongs(param.SongIds, ws.Settings)["123"].IsSuccess());
        Assert.Equal(1, offline.LastLocalCacheHitCount);
        api.VerifyAll();
    }

    [Fact]
    public void FailedSongDoesNotDiscardSuccessfulSong()
    {
        using var ws = new TestWorkspace();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        var song = TestWorkspace.Song();
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>>
        { ["123"] = new(song.SongVo), ["456"] = ResultVo<SongVo>.Failure("not found") });
        api.Setup(x => x.GetLyricVo("123", "123", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        var result = ws.Search(api).SearchSongs([TestWorkspace.Input(), TestWorkspace.Input("456")], ws.Settings);
        Assert.True(result["123"].IsSuccess());
        Assert.Equal("not found", result["456"].ErrorMsg);
    }

    [Theory]
    [InlineData(VerbatimLyricModeEnum.DISABLE, false)]
    [InlineData(VerbatimLyricModeEnum.STANDARD_MODE, true)]
    [InlineData(VerbatimLyricModeEnum.A2_MODE, true)]
    public void SearchRequestsConfiguredLyricMode(VerbatimLyricModeEnum mode, bool expected)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.VerbatimLyricMode = mode;
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        var song = TestWorkspace.Song();
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["123"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("123", "123", expected)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        Assert.True(ws.Search(api).SearchSongs([TestWorkspace.Input()], ws.Settings)["123"].IsSuccess());
        api.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FuzzySearchSupportsSelectedAndAggregatedProviders(bool aggregate)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.AggregatedBlurSearch = aggregate;
        var net = new Mock<IMusicApi>(MockBehavior.Strict);
        var qq = new Mock<IMusicApi>(MockBehavior.Strict);
        var found = new SearchResultVo();
        found.SongVos.Add(new() { DisplayId = "123" });
        net.Setup(x => x.Search("test", SearchTypeEnum.SONG_ID)).Returns(new ResultVo<SearchResultVo>(found));
        if (aggregate) qq.Setup(x => x.Search("test", SearchTypeEnum.SONG_ID)).Returns(ResultVo<SearchResultVo>.Failure("unavailable"));
        Assert.Same(found, Assert.Single(ws.Search(net, qq).BlurSearch(ws.Param(" test "), ws.Settings)));
        net.VerifyAll();
        qq.VerifyAll();
    }

    [Fact]
    public void EmptyFuzzyResultHasReadableError()
    {
        using var ws = new TestWorkspace();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.Search("test", SearchTypeEnum.SONG_ID)).Returns(new ResultVo<SearchResultVo>(new()));
        var service = ws.Search(api);
        Assert.Equal(ErrorMsgConst.SEARCH_RESULT_EMPTY, Assert.Throws<MusicLyricException>(() => service.BlurSearch(ws.Param("test"), ws.Settings)).Message);
        Assert.Equal(ErrorMsgConst.INPUT_CONENT_EMPLT, Assert.Throws<MusicLyricException>(() => service.BlurSearch(ws.Param(" "), ws.Settings)).Message);
    }
}
