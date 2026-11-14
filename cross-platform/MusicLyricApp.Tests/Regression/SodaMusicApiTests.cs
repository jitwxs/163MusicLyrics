using System.Net;
using System.Text;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using Newtonsoft.Json.Linq;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// 汽水音乐离线用例：接口返回取自 Fixtures/Soda（真实响应裁剪），HTTP 由 <see cref="ResponseHandler"/> 拦截。
/// </summary>
public class SodaMusicApiTests
{
    private const string ZhTrackId = "7293547695373273090";
    private const string ForeignTrackId = "7144550907370276865";
    private const string AlbumId = "7144550895936604162";
    private const string PlaylistId = "7031082140935145486";
    private const string DeviceId = "7123456789012345678";

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Soda", name + ".json"));

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>每个用例用不同的歌曲 ID，避免进程级 GlobalCache 互相影响。</summary>
    private static string FreshId() => "9" + Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999);

    private sealed class Fake
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        public Func<HttpRequestMessage, string, HttpResponseMessage> Respond { get; set; } = (_, _) => Json("{}");

        public SodaMusicApi Api() => new(() => DeviceId, Native());

        public SodaMusicNativeApi Native() => new(() => DeviceId, () => new HttpClient(new ResponseHandler(request =>
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
            Requests.Add(request);
            Bodies.Add(body);
            return Respond(request, body);
        })));
    }

    private static Fake TrackFake(string fixture)
    {
        return new Fake { Respond = (r, _) => r.RequestUri!.AbsolutePath == "/luna/h5/seo_track" ? Json(Fixture(fixture)) : Json("{}") };
    }

    [Fact]
    public void SongMetadataComesFromSeoTrack()
    {
        var fake = TrackFake("seo_zh");
        var id = FreshId();

        var result = fake.Api().GetSongVo([id]);

        var song = Assert.Single(result).Value.Assert().Data;
        Assert.Equal(ZhTrackId, song.Id);
        Assert.Equal(id, song.DisplayId);
        Assert.Equal("八部众曲 (《新天龙八部》PC端游全门派主题曲)", song.Name);
        Assert.Equal(14, song.Singer.Length);
        Assert.Equal("根小八", song.Singer[0]);
        Assert.Equal("徐远书", song.Singer[^1]);
        Assert.Equal("八部众曲 (《新天龙八部》PC端游全门派主题曲)", song.Album);
        Assert.Equal(272842, song.Duration);
        Assert.Equal("2023-10-25", song.PublishDate);
        Assert.Equal("https://p3-luna.douyinpic.com/img/tos-cn-v-2774c002/ooq3QXwmPBEAmjAsABsyU0A74iAoiWADOfAEux~c5_500x500.jpg",
            song.Pics);

        var request = Assert.Single(fake.Requests);
        Assert.Equal("beta-luna.douyin.com", request.RequestUri!.Host);
        Assert.Contains("track_id=" + id, request.RequestUri.Query);
        Assert.Contains("device_platform=web", request.RequestUri.Query);
        Assert.StartsWith("Mozilla/5.0", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public void SongAndLyricShareOneRequest()
    {
        var fake = TrackFake("seo_zh");
        var api = fake.Api();
        var id = FreshId();

        var song = api.GetSongVo([id])[id].Assert().Data;
        api.GetLyricVo(song.Id, song.DisplayId, false).Assert();
        api.GetLyricVo(song.Id, song.DisplayId, true).Assert();

        Assert.Single(fake.Requests);
    }

    [Fact]
    public void UnknownSongIsReportedAsMissing()
    {
        var fake = TrackFake("seo_missing");
        var id = FreshId();

        var song = fake.Api().GetSongVo([id])[id];

        Assert.False(song.IsSuccess());
        Assert.Equal(ErrorMsgConst.SONG_NOT_EXIST, song.ErrorMsg);
    }

    [Fact]
    public void LineLyricsDropWordTagsAndKeepLineEndCues()
    {
        var id = FreshId();
        var lyric = TrackFake("seo_zh").Api().GetLyricVo(id, id, false).Assert().Data;

        Assert.Equal(SearchSourceEnum.SODA_MUSIC, lyric.SearchSource);
        var lines = lyric.Lyric.Split(Environment.NewLine);
        Assert.Equal("[00:01.470]醉卧江湖君莫笑纵死尤闻侠骨香", lines[0]);
        Assert.Equal("[00:05.840]", lines[1]);
        Assert.Equal("[00:06.270]不见青丝红颜老只留正气满乾坤", lines[2]);
        Assert.DoesNotContain("<", lyric.Lyric);
        Assert.Equal("", lyric.TranslateLyric);
    }

    [Fact]
    public void VerbatimLyricsUseAbsoluteWordTimes()
    {
        var id = FreshId();
        var lyric = TrackFake("seo_zh").Api().GetLyricVo(id, id, true).Assert().Data;

        var first = lyric.Lyric.Split(Environment.NewLine)[0];
        Assert.StartsWith("[00:01.470]醉[00:01.740]卧[00:01.940]江[00:02.060]湖", first);
        Assert.EndsWith("[00:05.560]香[00:05.840]", first);
    }

    [Fact]
    public void ForeignSongCarriesChineseTranslation()
    {
        var id = FreshId();
        var api = TrackFake("seo_foreign").Api();

        var lyric = api.GetLyricVo(id, id, false).Assert().Data;
        Assert.StartsWith("[00:00.876]Stuck in the bed I have nowhere to go", lyric.Lyric);
        Assert.StartsWith("[00:00.87]困在床上，我无处可去", lyric.TranslateLyric);

        // Verbatim mode follows NetEase: original only, since the translation has line timing only.
        var verbatim = api.GetLyricVo(id, id, true).Assert().Data;
        Assert.StartsWith("[00:00.876]Stuck [00:01.096]in [00:01.308]the ", verbatim.Lyric);
        Assert.Equal("", verbatim.TranslateLyric);
    }

    [Fact]
    public async Task OutputMergesOriginalAndTranslation()
    {
        var id = FreshId();
        var lyric = TrackFake("seo_foreign").Api().GetLyricVo(id, id, false).Assert().Data;
        var settings = new SettingBean();

        var output = string.Join(Environment.NewLine, await LyricUtils.GetOutputContent(lyric, settings));

        Assert.Contains("Stuck in the bed I have nowhere to go", output);
        Assert.Contains("困在床上，我无处可去", output);
    }

    [Fact]
    public void AlbumListsTracksWithSingers()
    {
        var fake = new Fake { Respond = (r, _) => r.RequestUri!.AbsolutePath == "/luna/albums/" + AlbumId ? Json(Fixture("album")) : Json("{}") };

        var album = fake.Api().GetAlbumVo(AlbumId).Assert().Data;

        Assert.Equal("对味", album.Name);
        Assert.Equal("WM China", album.Company);
        Assert.Equal("2022-09-21", album.TimePublic);
        Assert.Equal(["7144550907370276865", "7144550908838299650", "7144550897014556673"],
            album.SimpleSongVos.Select(s => s.DisplayId));
        Assert.Equal(["EchoDing"], album.SimpleSongVos[0].Singer);
    }

    [Fact]
    public void PlaylistFollowsCursorUntilAPageIsEmpty()
    {
        var page1 = Fixture("playlist");
        var page2 = JObject.Parse(page1);
        var tracks = (JArray)page2["media_resources"]!;
        tracks[0]!["entity"]!["track_wrapper"]!["track"]!["id"] = "1001";
        tracks[1]!["entity"]!["track_wrapper"]!["track"]!["id"] = ForeignTrackId[..^1] + "0"; // another id
        page2["next_cursor"] = "96";
        var page3 = JObject.Parse(page1);
        page3["media_resources"] = new JArray();
        page3["next_cursor"] = "144";

        var fake = new Fake
        {
            Respond = (_, body) => (string?)JObject.Parse(body)["cursor"] switch
            {
                null => Json(page1),
                "48" => Json(page2.ToString()),
                "96" => Json(page3.ToString()),
                _ => throw new InvalidOperationException("requested past the empty page")
            }
        };

        var playlist = fake.Api().GetPlaylistVo(PlaylistId + "1").Assert().Data;

        Assert.Equal("吉他伴奏｜一个浪漫雨天的咖啡馆", playlist.Name);
        Assert.Equal("你被黑暗敲打，恰恰说明你是光明本身。", playlist.Description);
        Assert.Equal(["7102080149947942926", "7102079997967337508", "1001", ForeignTrackId[..^1] + "0"],
            playlist.SimpleSongVos.Select(s => s.DisplayId));
        Assert.Equal(3, fake.Requests.Count);
        Assert.All(fake.Requests, r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method);
            Assert.Equal("/luna/playlist/detail", r.RequestUri!.AbsolutePath);
            Assert.Equal("Luna/19.1.0 Android", r.Headers.UserAgent.ToString());
        });
        Assert.Equal(PlaylistId + "1", (string?)JObject.Parse(fake.Bodies[0])["playlist_id"]);
    }

    [Fact]
    public void PlaylistPagingStopsOnARepeatedCursor()
    {
        var page = Fixture("playlist");
        var fake = new Fake { Respond = (_, _) => Json(page) };

        var playlist = fake.Api().GetPlaylistVo(PlaylistId + "2").Assert().Data;

        // Page 1, then cursor 48 once more; the repeated cursor ends the loop and duplicates are dropped.
        Assert.Equal(2, fake.Requests.Count);
        Assert.Equal(2, playlist.SimpleSongVos.Length);
    }

    [Fact]
    public void MissingPlaylistIsReported()
    {
        var fake = new Fake { Respond = (_, _) => Json("""{"status_code":1000005,"status_info":{"status_msg":"ERR_RESOURCE_NOT_FOUND"}}""") };

        var result = fake.Api().GetPlaylistVo(PlaylistId + "3");

        Assert.False(result.IsSuccess());
        Assert.Equal(ErrorMsgConst.PLAYLIST_NOT_EXIST, result.ErrorMsg);
    }

    [Fact]
    public void KeywordSearchUsesThePlatformChannelWithAClientToken()
    {
        var fake = new Fake
        {
            Respond = (r, _) => r.RequestUri!.Host switch
            {
                "open.qishui.com" => Json("""{"err_no":0,"client_token":"clt.test"}"""),
                "open.douyin.com" => Json(Fixture("search")),
                _ => throw new InvalidOperationException(r.RequestUri.ToString())
            }
        };

        var result = fake.Api().Search("天龙 " + FreshId(), SearchTypeEnum.SONG_ID).Assert().Data;

        Assert.Equal(SearchSourceEnum.SODA_MUSIC, result.SearchSource);
        Assert.Equal(3, result.SongVos.Count);
        var first = result.SongVos[0];
        Assert.Equal("7601885734266718244", first.DisplayId);
        Assert.Equal("太上感应篇", first.Title);
        Assert.Equal(["鹿北 林深见鹿"], first.AuthorName);
        Assert.Equal("太上感应篇", first.AlbumName);
        Assert.Equal(479960, first.Duration);

        Assert.Equal(2, fake.Requests.Count);
        var search = fake.Requests[1];
        Assert.Equal("/api/luna/v1/platform/search/", search.RequestUri!.AbsolutePath);
        Assert.Equal("LunaOpenSDK/1.0.10", search.Headers.UserAgent.ToString());
        Assert.Equal("clt.test", search.Headers.GetValues("access-token").Single());
        Assert.Contains("device_id=" + DeviceId, search.RequestUri.Query);
        var body = JObject.Parse(fake.Bodies[1]);
        Assert.StartsWith("天龙 ", (string?)body["q"]);
        Assert.Equal("track", (string?)body["search_type"]);
    }

    [Fact]
    public void ExpiredClientTokenIsRefreshedOnce()
    {
        var tokens = 0;
        var searches = 0;
        var fake = new Fake
        {
            Respond = (r, _) =>
            {
                if (r.RequestUri!.Host == "open.qishui.com") return Json($$"""{"err_no":0,"client_token":"clt.{{++tokens}}"}""");
                return ++searches == 1 ? Json("""{"err_no":28001008,"err_msg":"token expired"}""") : Json(Fixture("search"));
            }
        };

        var result = fake.Api().Search("expired " + FreshId(), SearchTypeEnum.SONG_ID).Assert().Data;

        Assert.Equal(3, result.SongVos.Count);
        Assert.Equal(2, tokens);
        Assert.Equal("clt.2", fake.Requests.Last().Headers.GetValues("access-token").Single());
    }

    [Fact]
    public void SearchStillRunsWithoutAToken()
    {
        var fake = new Fake
        {
            Respond = (r, _) => r.RequestUri!.Host == "open.qishui.com"
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : Json(Fixture("search"))
        };

        var result = fake.Api().Search("no token " + FreshId(), SearchTypeEnum.SONG_ID).Assert().Data;

        Assert.Equal(3, result.SongVos.Count);
        Assert.False(fake.Requests.Last().Headers.Contains("access-token"));
    }

    [Fact]
    public void SearchStillRunsWhenTheTokenRequestTimesOut()
    {
        // HttpClient reports its timeout as TaskCanceledException.
        var fake = new Fake
        {
            Respond = (r, _) => r.RequestUri!.Host == "open.qishui.com"
                ? throw new TaskCanceledException("token request timed out")
                : Json(Fixture("search"))
        };

        var result = fake.Api().Search("token timeout " + FreshId(), SearchTypeEnum.SONG_ID).Assert().Data;

        Assert.Equal(3, result.SongVos.Count);
        Assert.False(fake.Requests.Last().Headers.Contains("access-token"));
    }

    [Theory]
    [InlineData(SearchTypeEnum.ALBUM_ID)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID)]
    public void OnlySongsCanBeSearched(SearchTypeEnum type)
    {
        var fake = new Fake();

        var result = fake.Api().Search("任何 " + FreshId(), type);

        Assert.False(result.IsSuccess());
        Assert.Equal(ErrorMsgConst.SODA_SEARCH_SONG_ONLY, result.ErrorMsg);
        Assert.Empty(fake.Requests);
    }

    [Fact]
    public void SongLinkIsNotOffered()
    {
        var fake = new Fake();

        var result = fake.Api().GetSongLink(FreshId());

        Assert.False(result.IsSuccess());
        Assert.Empty(fake.Requests);
    }

    [Fact]
    public void EmptyResponseBodyIsANetworkFailure()
    {
        // Missing signature or rate limiting shows up as HTTP 200 with no body.
        var native = new Fake { Respond = (_, _) => Json("") }.Native();

        Assert.Throws<HttpRequestException>(() => native.GetTrack(FreshId()));
    }

    [Fact]
    public void KrcConversionHandlesTagLinesAndUntaggedLines()
    {
        const string krc = "[ti:Song]\n[1000,2000]纯文本\n[3000,1000]<0,500,0>A<500,500,0>B";

        Assert.Equal(string.Join(Environment.NewLine, "[ti:Song]", "[00:01.000]纯文本", "[00:03.000]", "[00:03.000]AB", "[00:04.000]", ""),
            VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4SodaMusic(krc));
        Assert.Equal(string.Join(Environment.NewLine, "[ti:Song]", "[00:01.000]纯文本[00:03.000]", "[00:03.000]A[00:03.500]B[00:04.000]", ""),
            VerbatimLyricUtils.DealVerbatimLyric4SodaMusic(krc));
        Assert.Equal("", VerbatimLyricUtils.DealVerbatimLyric4SodaMusic(null));
    }

    [Theory]
    [InlineData("https://music.douyin.com/qishui/share/track?track_id=7293547695373273090&hybrid_sdk_version=bullet", SearchTypeEnum.SONG_ID, "7293547695373273090")]
    [InlineData("https://music.douyin.com/qishui/share/album?album_id=7144550895936604162&hybrid_sdk_version=bullet&auto_play_bgm=1", SearchTypeEnum.ALBUM_ID, "7144550895936604162")]
    [InlineData("分享 https://music.douyin.com/qishui/share/playlist?playlist_id=7031082140935145486 来自汽水音乐", SearchTypeEnum.PLAYLIST_ID, "7031082140935145486")]
    public void ShareLinksAreRecognisedFromAnySource(string input, SearchTypeEnum type, string id)
    {
        var parsed = GlobalUtils.CheckInputId(input, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID);

        Assert.Equal(SearchSourceEnum.SODA_MUSIC, parsed.SearchSource);
        Assert.Equal(type, parsed.SearchType);
        Assert.Equal(id, parsed.QueryId);
    }

    [Theory]
    [InlineData(SearchTypeEnum.SONG_ID)]
    [InlineData(SearchTypeEnum.ALBUM_ID)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID)]
    public void PlainNumericIdUsesTheChosenType(SearchTypeEnum type)
    {
        var parsed = GlobalUtils.CheckInputId("7293547695373273090", SearchSourceEnum.SODA_MUSIC, type);

        Assert.Equal(SearchSourceEnum.SODA_MUSIC, parsed.SearchSource);
        Assert.Equal(type, parsed.SearchType);
        Assert.Equal("7293547695373273090", parsed.QueryId);
    }

    [Fact]
    public void NonNumericIdIsRejected()
    {
        Assert.Throws<MusicLyricApp.Core.MusicLyricException>(() =>
            GlobalUtils.CheckInputId("abc", SearchSourceEnum.SODA_MUSIC, SearchTypeEnum.SONG_ID));
    }

    [Fact]
    public void SearchServiceRegistersSodaAndKeepsOneDeviceId()
    {
        var settings = new SettingBean();
        Assert.Equal("", settings.Config.SodaMusicDeviceId);

        var service = new SearchService(settings);
        Assert.Equal(SearchSourceEnum.SODA_MUSIC, service.GetMusicApi(SearchSourceEnum.SODA_MUSIC).Source());

        var id = SearchService.SodaMusicDeviceId(settings);
        Assert.Matches("^7\\d{18}$", id);
        Assert.Equal(id, settings.Config.SodaMusicDeviceId);
        Assert.Equal(id, SearchService.SodaMusicDeviceId(settings));

        settings.Config.SodaMusicDeviceId = "broken";
        Assert.Matches("^7\\d{18}$", SearchService.SodaMusicDeviceId(settings));
    }
}
