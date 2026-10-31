using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class QQMusicNativeApiTests
{
    [Theory]
    [InlineData(SearchTypeEnum.SONG_ID, 0)]
    [InlineData(SearchTypeEnum.ALBUM_ID, 2)]
    [InlineData(SearchTypeEnum.PLAYLIST_ID, 3)]
    public void KeywordSearchSendsRequiredCommAndSearchParameters(SearchTypeEnum searchType, int expectedType)
    {
        var api = new CapturingQQMusicNativeApi();

        var response = api.Search("周杰伦", searchType);

        Assert.Equal(0, response.Code);
        Assert.Equal("https://u.y.qq.com/cgi-bin/musicu.fcg", api.Url);
        var request = JObject.Parse(api.Body!);
        Assert.Equal(24, (int?)request["comm"]?["ct"]);
        Assert.Equal(0, (int?)request["comm"]?["cv"]);
        Assert.Equal("0", (string?)request["comm"]?["uin"]);
        Assert.Equal("json", (string?)request["comm"]?["format"]);
        Assert.Equal("yqq", (string?)request["comm"]?["platform"]);
        Assert.Equal("DoSearchForQQMusicDesktop", (string?)request["req_1"]?["method"]);
        Assert.Equal("music.search.SearchCgiService", (string?)request["req_1"]?["module"]);
        Assert.Equal("周杰伦", (string?)request["req_1"]?["param"]?["query"]);
        Assert.Equal(expectedType, (int?)request["req_1"]?["param"]?["search_type"]);
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
