using System.Net;
using System.Text;
using MusicLyricApp.Core.Service;
using Newtonsoft.Json.Linq;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// 汽水音乐接入前的可行性探测：只访问公开的网页分享页（music.douyin.com/qishui/share/*），
/// 不读取用户设置或 Cookie，不调用需要客户端签名的私有接口。
/// 运行：MUSICLYRIC_LIVE_TESTS=1 dotnet test --filter 'FullyQualifiedName~SodaMusicFeasibility'
/// </summary>
[Trait("Category", "Live")]
public class SodaMusicFeasibilityTests(ITestOutputHelper output)
{
    private const string ShareBase = "https://music.douyin.com/qishui/share/";

    // 有逐字歌词、多歌手、作词作曲信息的单曲
    private const string TrackId = "7293547695373273090";

    // 外文单曲，用于确认是否有翻译歌词
    private const string ForeignTrackId = "7144550907370276865";

    private const string AlbumId = "7144550895936604162";

    // 短链（指向上面的专辑）
    private const string ShortLink = "https://qishui.douyin.com/s/69HXcAV/";

    private const string BrowserUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    [LiveFact]
    public async Task TrackSharePageProvidesSongMetadata()
    {
        var option = (await LoadRouterData($"track?track_id={TrackId}"))
            .SelectToken("loaderData.track_page.audioWithLyricsOption")!;

        Assert.Equal(0, option.Value<int>("status_code"));
        var info = option["trackInfo"]!;
        Assert.Equal(TrackId, info.Value<string>("id"));
        Assert.False(string.IsNullOrWhiteSpace(info.Value<string>("name")));

        // 时长：trackInfo.duration 为毫秒，外层 duration 为秒
        var durationMs = info.Value<long>("duration");
        Assert.InRange(durationMs, 60_000, 600_000);
        Assert.Equal(durationMs / 1000.0, option.Value<double>("duration"), 0);

        var artists = info["artists"]!.Select(a => a.Value<string>("name")).ToList();
        Assert.True(artists.Count > 1);
        Assert.All(artists, a => Assert.False(string.IsNullOrWhiteSpace(a)));

        var album = info["album"]!;
        Assert.False(string.IsNullOrWhiteSpace(album.Value<string>("id")));
        Assert.False(string.IsNullOrWhiteSpace(album.Value<string>("name")));
        Assert.True(album.Value<long>("release_date") > 0); // Unix 秒

        var cover = album["url_cover"]!;
        Assert.False(string.IsNullOrWhiteSpace(cover.Value<string>("uri")));
        Assert.NotEmpty(cover["urls"]!);

        output.WriteLine($"name={info["name"]}, artists={string.Join("/", artists)}, album={album["name"]}, " +
                         $"durationMs={durationMs}, cover={cover["urls"]![0]}{cover["uri"]}");
    }

    [LiveFact]
    public async Task TrackSharePageProvidesLineAndWordTimedLyrics()
    {
        var lyrics = (await LoadRouterData($"track?track_id={TrackId}"))
            .SelectToken("loaderData.track_page.audioWithLyricsOption.lyrics")!;

        Assert.Equal("krc", lyrics.Value<string>("lyricType"));
        var sentences = lyrics["sentences"]!.ToList();
        Assert.True(sentences.Count > 10);

        // 开头是作曲/作词信息行
        Assert.StartsWith("作曲", sentences[0].Value<string>("text"));

        var wordTimed = 0;
        long previousStart = -1;
        foreach (var sentence in sentences)
        {
            var start = sentence.Value<long>("startMs");
            var end = sentence.Value<long>("endMs");
            Assert.True(end >= start);
            Assert.True(start >= previousStart, "sentences 应按时间排序");
            previousStart = start;

            var words = sentence["words"]?.ToList() ?? [];
            if (words.Count <= 1) continue;

            wordTimed++;
            // 逐字文本拼接后等于整句，逐字时间落在整句区间内
            Assert.Equal(sentence.Value<string>("text"), string.Concat(words.Select(w => w.Value<string>("text"))));
            Assert.All(words, w =>
            {
                Assert.InRange(w.Value<long>("startMs"), start, end);
                Assert.InRange(w.Value<long>("endMs"), start, end);
            });
        }

        Assert.True(wordTimed > sentences.Count / 2, "大部分歌词行应有逐字时间");
        output.WriteLine($"sentences={sentences.Count}, wordTimed={wordTimed}");
        output.WriteLine(ToLrc(sentences.Take(8)));
    }

    [LiveFact]
    public async Task ForeignTrackSharePageHasNoTranslatedLyrics()
    {
        var option = (await LoadRouterData($"track?track_id={ForeignTrackId}"))
            .SelectToken("loaderData.track_page.audioWithLyricsOption")!;

        Assert.Equal(0, option.Value<int>("status_code"));
        var lyrics = (JObject)option["lyrics"]!;
        Assert.NotEmpty(lyrics["sentences"]!);

        var lyricKeys = lyrics.Properties().Select(p => p.Name).ToHashSet();
        var sentenceKeys = lyrics["sentences"]!.SelectMany(s => ((JObject)s).Properties().Select(p => p.Name)).ToHashSet();
        output.WriteLine($"lyrics keys={string.Join(",", lyricKeys)}; sentence keys={string.Join(",", sentenceKeys)}");

        // 结论记录：分享页只给原文，没有翻译/音译字段
        Assert.DoesNotContain(lyricKeys.Concat(sentenceKeys), k => k.Contains("trans", StringComparison.OrdinalIgnoreCase));
    }

    [LiveFact]
    public async Task UnknownTrackIdIsReportedAsNotFound()
    {
        var page = (await LoadRouterData("track?track_id=1"))
            .SelectToken("loaderData.track_page")!;

        Assert.NotEqual(0, page.SelectToken("audioWithLyricsOption.status_code")!.Value<int>());
        Assert.True(page.Value<bool>("happenError"));
        Assert.True(string.IsNullOrEmpty(page.SelectToken("audioWithLyricsOption.trackInfo.id")?.Value<string>()));
    }

    [LiveFact]
    public async Task AlbumSharePageListsAllTracks()
    {
        var page = (await LoadRouterData($"album?album_id={AlbumId}")).SelectToken("loaderData.album_page")!;

        var albumInfo = page["albumInfo"]!;
        Assert.Equal(AlbumId, albumInfo.Value<string>("id"));
        Assert.False(string.IsNullOrWhiteSpace(albumInfo.Value<string>("name")));

        var tracks = page["trackList"]!.ToList();
        Assert.NotEmpty(tracks);
        Assert.All(tracks, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Value<string>("id")));
            Assert.False(string.IsNullOrWhiteSpace(t.Value<string>("name")));
            Assert.Equal(AlbumId, t.SelectToken("album.id")!.Value<string>());
        });

        output.WriteLine($"album={albumInfo["name"]}, tracks={tracks.Count}, " +
                         $"albumInfo keys={string.Join(",", ((JObject)albumInfo).Properties().Select(p => p.Name))}");
    }

    [LiveFact]
    public async Task ShortShareLinkRedirectsToShareUrlWithId()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);

        using var response = await client.GetAsync(ShortLink);

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.MovedPermanently,
            $"unexpected status {response.StatusCode}");
        var location = response.Headers.Location!;
        output.WriteLine(location.ToString());
        Assert.StartsWith(ShareBase + "album", location.ToString());
        Assert.Contains($"album_id={AlbumId}", location.Query);
    }

    [LiveFact]
    public async Task PlaylistSharePageStructure()
    {
        // 公开歌单 ID（来自 douyin.com/qishui/playlist/ 页面）；只记录结构，不断言有内容
        var page = (await LoadRouterData("playlist?playlist_id=7465862548135577652"))
            .SelectToken("loaderData.playlist_page")!;

        Assert.NotNull(page["playlistInfo"]);
        Assert.NotNull(page["medias"]);
        output.WriteLine($"title={page.SelectToken("playlistInfo.title")}, medias={page["medias"]!.Count()}");
    }

    private static async Task<JObject> LoadRouterData(string pathAndQuery)
    {
        using var client = NetworkClientFactory.CreateHttpClient(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);

        var html = await client.GetStringAsync(ShareBase + pathAndQuery);
        return JObject.Parse(ExtractRouterDataJson(html));
    }

    /// <summary>按括号配对截取 _ROUTER_DATA 对象，避免正则在字符串内的 "};" 处截断。</summary>
    private static string ExtractRouterDataJson(string html)
    {
        var marker = html.IndexOf("_ROUTER_DATA", StringComparison.Ordinal);
        Assert.True(marker >= 0, "页面中未找到 _ROUTER_DATA");
        var start = html.IndexOf('{', marker);

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < html.Length; i++)
        {
            var c = html[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
            }
            else if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return html.Substring(start, i - start + 1);
        }

        throw new InvalidOperationException("_ROUTER_DATA JSON 不完整");
    }

    private static string ToLrc(IEnumerable<JToken> sentences)
    {
        var sb = new StringBuilder();
        foreach (var s in sentences)
        {
            var t = TimeSpan.FromMilliseconds(s.Value<long>("startMs"));
            sb.Append($"[{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds:000}]").AppendLine(s.Value<string>("text"));
        }

        return sb.ToString();
    }
}
