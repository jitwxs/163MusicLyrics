using System.Net;
using System.Net.Http;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using Newtonsoft.Json;

namespace MusicLyricAppTest.Regression;

public class CodeQualityRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeRequestsPreserveEncodingHeadersAndCookie(bool json)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var api = new TestNativeApi("session=test", handler);
        Assert.Equal("測試 response", api.Post(json));
        Assert.Equal("session=test", handler.Cookie);
        Assert.Equal("https://music.example/", handler.Referrer);
        Assert.Equal(BaseNativeApi.Useragent, handler.UserAgent);
        Assert.Equal(json ? "application/json" : "application/x-www-form-urlencoded", handler.MediaType);
        if (json)
            Assert.Equal("中文 & +", JsonConvert.DeserializeObject<Dictionary<string, string>>(handler.Body)!["query"]);
        else
            Assert.Equal("query=%E4%B8%AD%E6%96%87+%26+%2B", handler.Body);
    }

    [Fact]
    public void NativeHttpFailureThrowsAndBlankCookieUsesDefault()
    {
        var handler = new RecordingHandler(HttpStatusCode.Forbidden);
        Assert.Throws<HttpRequestException>(() => new TestNativeApi("", handler).Post(false));
        Assert.StartsWith("NMTID=", handler.Cookie);
    }

    [Fact]
    public void EmptyJsonDoesNotPretendToBeAValidPayload()
    {
        Assert.Throws<JsonSerializationException>(() => "null".ToEntity<SettingBean>());
        Assert.Throws<JsonSerializationException>(() => "null".ToEntityList<Song>());
        Assert.Empty("[]".ToEntityList<Song>());
    }

    [Fact]
    public void FailurePayloadThrowsItsBusinessError()
    {
        var failure = ResultVo<SongVo>.Failure("missing song");
        Assert.False(failure.IsSuccess());
        Assert.Contains("missing song", failure.ToJson());
        Assert.DoesNotContain("Data", failure.ToJson());
        Assert.Equal("missing song", Assert.Throws<MusicLyricException>(() => failure.Data).Message);
    }

    [Fact]
    public void PlaylistBaseAndDerivedViewsShareDeserializedMembers()
    {
        var playlist = """{"name":"歌單","creator":{"nickname":"作者"}}""".ToEntity<Playlist>();
        SimplePlaylist simple = playlist;
        Assert.Equal("歌單", simple.Name);
        Assert.Same(simple.Creator, playlist.Creator);
        Assert.Equal("作者", new PlaylistResult { Playlist = playlist }.Convert([]).AuthorName);
    }

    [Fact]
    public void MissingOptionalReleaseAndSearchMetadataRemainUsable()
    {
        Assert.Contains("DownloadCount: 0", UpdateCheckService.BuildReleaseDescription(new GitHubInfo()));
        var search = """{"songCount":1,"songs":[{"id":"1","name":"歌"}]}""".ToEntity<SearchResult>();
        var song = Assert.Single(search.Convert(SearchTypeEnum.SONG_ID).SongVos);
        Assert.Equal("歌", song.Title);
        Assert.Empty(song.AlbumName);
        Assert.Empty(song.AuthorName);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"code\":0,\"req_1\":{\"code\":0}}")]
    [InlineData("{\"code\":0,\"req_1\":{\"code\":0,\"data\":{\"code\":0}}}")]
    public void MissingQQSearchPayloadReturnsBusinessFailure(string json)
    {
        var api = new QQMusicApi(() => "", new JsonNativeApi(json));
        var result = api.Search(Guid.NewGuid().ToString("N"), SearchTypeEnum.SONG_ID);
        Assert.False(result.IsSuccess());
        Assert.Equal(ErrorMsgConst.NETWORK_ERROR, result.ErrorMsg);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"code\":0,\"req\":{\"code\":0,\"data\":{}},\"req_0\":{\"code\":0,\"data\":{}}}")]
    public void MissingQQDownloadAddressReturnsEmptyLink(string json)
    {
        Assert.Empty(new JsonNativeApi(json).GetSongLink("song-mid").Data);
    }

    private sealed class JsonNativeApi(string json) : QQMusicNativeApi(() => "")
    {
        protected override string SendJsonPost(string url, Dictionary<string, object> paramDict) => json;
    }

    private sealed class TestNativeApi(string cookie, RecordingHandler handler)
        : BaseNativeApi(() => cookie, () => new HttpClient(handler))
    {
        protected override string HttpRefer() => "https://music.example/";
        public string Post(bool json) => json
            ? SendJsonPost("https://music.example/api", new() { ["query"] = "中文 & +" })
            : SendPost("https://music.example/api", new() { ["query"] = "中文 & +" });
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string Cookie { get; private set; } = "";
        public string Referrer { get; private set; } = "";
        public string UserAgent { get; private set; } = "";
        public string MediaType { get; private set; } = "";
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Cookie = Assert.Single(request.Headers.GetValues("Cookie"));
            Referrer = request.Headers.Referrer!.ToString();
            UserAgent = request.Headers.UserAgent.ToString();
            MediaType = request.Content!.Headers.ContentType!.MediaType!;
            Body = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("測試 response") };
        }
    }
}
