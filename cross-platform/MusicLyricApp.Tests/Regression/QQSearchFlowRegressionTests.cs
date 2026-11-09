using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicLyricAppTest.Regression;

public class QQSearchFlowRegressionTests
{
    // Keep wire fixtures independent of model serialization so incorrect property mappings are observable.
    private const string SearchBody = """
        {
          "song":{"list":[{"id":204422870,"mid":"song-mid-not-id","name":"name-not-title",
            "title":"歌曲标题","interval":215,"singer":[{"name":"歌手甲"},{"name":"歌手乙"}],
            "album":{"name":"歌曲所属专辑"}}]},
          "album":{"list":[{"albumID":12345,"albumMID":"album-mid-not-id","albumName":"专辑标题",
            "song_count":12,"publicTime":"2026-11-09","singer_list":[{"name":"歌手甲"},{"name":"歌手乙"}]}]},
          "songlist":{"list":[{"dissid":"987654321","dissname":"歌单标题","introduction":"歌单介绍",
            "song_count":37,"listennum":12345678901,"creator":{"name":"创建者","qq":123}}]}
        }
        """;

    [Theory]
    [InlineData(SearchTypeEnum.SONG_ID)]
    [InlineData(SearchTypeEnum.ALBUM_ID)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID)]
    public void NativeJsonIsConvertedByApiAndSelectedProviderBlurSearch(SearchTypeEnum type)
    {
        using var ws = new TestWorkspace();
        var native = new JsonResponseNativeApi(Envelope(JObject.Parse(SearchBody)));
        var api = new QQMusicApi(() => "", native);
        var keyword = Guid.NewGuid().ToString("N");

        var response = api.Search(keyword, type);
        Assert.True(response.IsSuccess(), response.ErrorMsg);
        AssertConverted(response.Data, type);

        var service = CreateSearchService(ws, api, type);
        var serviceKeyword = Guid.NewGuid().ToString("N");
        var results = service.BlurSearch(ws.Param("  " + serviceKeyword + "  "), ws.Settings);
        AssertConverted(Assert.Single(results), type);
        Assert.Equal(new[] { keyword, serviceKeyword }, native.Queries);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    public void EachBusinessFailureLevelRejectsEvenPopulatedResults(int code, int requestCode, int dataCode)
    {
        using var ws = new TestWorkspace();
        var native = new JsonResponseNativeApi(Envelope(JObject.Parse(SearchBody), code, requestCode, dataCode));
        var api = new QQMusicApi(() => "", native);

        var response = api.Search(Guid.NewGuid().ToString("N"), SearchTypeEnum.SONG_ID);
        Assert.False(response.IsSuccess());
        Assert.Equal(ErrorMsgConst.NETWORK_ERROR, response.ErrorMsg);

        var service = CreateSearchService(ws, api, SearchTypeEnum.SONG_ID);
        var error = Assert.Throws<MusicLyricException>(() =>
            service.BlurSearch(ws.Param(Guid.NewGuid().ToString("N")), ws.Settings));
        Assert.Equal(ErrorMsgConst.NETWORK_ERROR, error.Message);
        Assert.Equal(2, native.Queries.Count);
    }

    [Theory]
    [InlineData(SearchTypeEnum.SONG_ID, "song", false)]
    [InlineData(SearchTypeEnum.ALBUM_ID, "album", false)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID, "songlist", false)]
    [InlineData(SearchTypeEnum.SONG_ID, "song", true)]
    [InlineData(SearchTypeEnum.ALBUM_ID, "album", true)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID, "songlist", true)]
    public void SupportedEmptyBodiesSucceedAtApiButBlurSearchReportsEmpty(
        SearchTypeEnum type, string section, bool omitSection)
    {
        using var ws = new TestWorkspace();
        var body = new JObject();
        if (!omitSection)
            body[section] = new JObject { ["list"] = new JArray() };
        var native = new JsonResponseNativeApi(Envelope(body));
        var api = new QQMusicApi(() => "", native);

        var response = api.Search(Guid.NewGuid().ToString("N"), type);
        Assert.True(response.IsSuccess(), response.ErrorMsg);
        Assert.Equal(type, response.Data.SearchType);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, response.Data.SearchSource);
        Assert.True(response.Data.IsEmpty());

        var service = CreateSearchService(ws, api, type);
        var error = Assert.Throws<MusicLyricException>(() =>
            service.BlurSearch(ws.Param(Guid.NewGuid().ToString("N")), ws.Settings));
        Assert.Equal(ErrorMsgConst.SEARCH_RESULT_EMPTY, error.Message);
        Assert.Equal(2, native.Queries.Count);
    }

    private static SearchService CreateSearchService(TestWorkspace ws, QQMusicApi api, SearchTypeEnum type)
    {
        ws.Settings.Param.SearchSource = SearchSourceEnum.QQ_MUSIC;
        ws.Settings.Param.SearchType = type;
        ws.Settings.Config.AggregatedBlurSearch = false;
        return new SearchService(ws.Settings, new Dictionary<SearchSourceEnum, IMusicApi>
        {
            [SearchSourceEnum.QQ_MUSIC] = api
        });
    }

    private static string Envelope(JObject body, int code = 0, int requestCode = 0, int dataCode = 0) =>
        new JObject
        {
            ["code"] = code,
            ["req_1"] = new JObject
            {
                ["code"] = requestCode,
                ["data"] = new JObject { ["code"] = dataCode, ["body"] = body }
            }
        }.ToString(Formatting.None);

    private static void AssertConverted(SearchResultVo result, SearchTypeEnum type)
    {
        Assert.Equal(type, result.SearchType);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, result.SearchSource);
        Assert.False(result.IsEmpty());
        Assert.Equal(type == SearchTypeEnum.SONG_ID ? 1 : 0, result.SongVos.Count);
        Assert.Equal(type == SearchTypeEnum.ALBUM_ID ? 1 : 0, result.AlbumVos.Count);
        Assert.Equal(type == SearchTypeEnum.PLAYLIST_ID ? 1 : 0, result.PlaylistVos.Count);
        switch (type)
        {
            case SearchTypeEnum.SONG_ID:
                var song = Assert.Single(result.SongVos);
                Assert.Equal("204422870", song.DisplayId);
                Assert.Equal("歌曲标题", song.Title);
                Assert.Equal(new[] { "歌手甲", "歌手乙" }, song.AuthorName);
                Assert.Equal("歌曲所属专辑", song.AlbumName);
                Assert.Equal(215000L, song.Duration);
                break;
            case SearchTypeEnum.ALBUM_ID:
                var album = Assert.Single(result.AlbumVos);
                Assert.Equal("album-mid-not-id", album.DisplayId);
                Assert.Equal("专辑标题", album.AlbumName);
                Assert.Equal(new[] { "歌手甲", "歌手乙" }, album.AuthorName);
                Assert.Equal(12L, album.SongCount);
                Assert.Equal("2026-11-09", album.PublishTime);
                break;
            case SearchTypeEnum.PLAYLIST_ID:
                var playlist = Assert.Single(result.PlaylistVos);
                Assert.Equal("987654321", playlist.DisplayId);
                Assert.Equal("歌单标题", playlist.PlaylistName);
                Assert.Equal("创建者", playlist.AuthorName);
                Assert.Equal("歌单介绍", playlist.Description);
                Assert.Equal(37L, playlist.SongCount);
                Assert.Equal(12345678901L, playlist.PlayCount);
                break;
        }
    }

    private sealed class JsonResponseNativeApi(string response) : QQMusicNativeApi(() => "")
    {
        public List<string> Queries { get; } = [];

        protected override string SendJsonPost(string url, Dictionary<string, object> paramDict)
        {
            var request = JObject.FromObject(paramDict);
            Queries.Add((string)request["req_1"]!["param"]!["query"]!);
            return response;
        }
    }
}
