using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class QQMusicNativeApiTests
{
    [Theory]
    [InlineData(SearchTypeEnum.SONG_ID, 0, "周杰伦")]
    [InlineData(SearchTypeEnum.ALBUM_ID, 2, "周杰伦")]
    [InlineData(SearchTypeEnum.PLAYLIST_ID, 3, "周杰伦")]
    [InlineData(SearchTypeEnum.SONG_ID, 0, "从头再来")]
    [InlineData(SearchTypeEnum.ALBUM_ID, 2, "从头再来")]
    [InlineData(SearchTypeEnum.PLAYLIST_ID, 3, "从头再来")]
    public void KeywordSearchUsesDesktopClientParametersToAvoidEmptyResults(
        SearchTypeEnum searchType, int expectedType, string keyword)
    {
        var api = new CapturingQQMusicNativeApi();

        var response = api.Search(keyword, searchType);

        Assert.Equal(0, response.Code);
        Assert.Equal("https://u.y.qq.com/cgi-bin/musicu.fcg", api.Url);
        var request = JObject.Parse(api.Body!);
        Assert.Equal(20, (int?)request["comm"]?["ct"]);
        Assert.Equal(1859, (int?)request["comm"]?["cv"]);
        Assert.Equal("0", (string?)request["comm"]?["uin"]);
        Assert.Equal("json", (string?)request["comm"]?["format"]);
        Assert.Equal("yqq", (string?)request["comm"]?["platform"]);
        Assert.Equal("DoSearchForQQMusicDesktop", (string?)request["req_1"]?["method"]);
        Assert.Equal("music.search.SearchCgiService", (string?)request["req_1"]?["module"]);
        Assert.Equal(keyword, (string?)request["req_1"]?["param"]?["query"]);
        Assert.Equal(expectedType, (int?)request["req_1"]?["param"]?["search_type"]);
        Assert.Equal("20", (string?)request["req_1"]?["param"]?["num_per_page"]);
        Assert.Equal("1", (string?)request["req_1"]?["param"]?["page_num"]);
    }

    private sealed class CapturingQQMusicNativeApi : QQMusicNativeApi
    {
        public string? Url { get; private set; }
        public string? Body { get; private set; }

        public CapturingQQMusicNativeApi() : base(() => "") { }

        protected override string SendJsonPost(string url, Dictionary<string, object> paramDict)
        {
            Url = url;
            Body = JsonConvert.SerializeObject(paramDict);
            return "{\"code\":0,\"req_1\":{\"code\":0,\"data\":{\"code\":0,\"body\":{\"song\":{\"list\":[]}}}}}";
        }
    }
}
