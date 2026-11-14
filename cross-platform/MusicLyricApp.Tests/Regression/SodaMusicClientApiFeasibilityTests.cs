using System.Text;
using MusicLyricApp.Core.Service;
using Newtonsoft.Json.Linq;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// 汽水音乐"模拟客户端"接口可行性探测（游客态，无 Cookie、无签名头）。
/// 参考 github.com/fansa91/qinyin：App 侧 beta-luna.douyin.com 与车机开放平台 open.douyin.com。
/// 运行：MUSICLYRIC_LIVE_TESTS=1 dotnet test --filter 'FullyQualifiedName~SodaMusicClientApiFeasibility'
/// </summary>
[Trait("Category", "Live")]
public class SodaMusicClientApiFeasibilityTests(ITestOutputHelper output)
{
    private const string AppHost = "https://beta-luna.douyin.com";
    private const string AppUserAgent = "Luna/19.1.0 Android";
    private const string WebUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36";

    private const string PlatformHost = "https://open.douyin.com";
    private const string PlatformTokenUrl = "https://open.qishui.com/api/luna/v1/platform/client/token/get/";
    private const string SdkUserAgent = "LunaOpenSDK/1.0.10";

    private const string TrackId = "7293547695373273090";
    private const string ForeignTrackId = "7144550907370276865";
    private const string AlbumId = "7144550895936604162";

    // 每次运行固定一个自造 device_id（19 位数字，7 开头）
    private static readonly string DeviceId = "7" + string.Concat(Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(10)));

    [LiveFact]
    public async Task PlatformKeywordSearchReturnsTracks()
    {
        var body = await PlatformSearch("晴天 周杰伦", "track");

        Assert.Equal(0, body.Value<int>("err_no"));
        var tracks = SearchEntities(body, "track").Select(e => e["track"]).OfType<JObject>().ToList();
        Assert.NotEmpty(tracks);

        var first = tracks[0];
        output.WriteLine($"track keys: {string.Join(",", first.Properties().Select(p => p.Name))}");
        var artistAlbumPaths = first.Descendants().OfType<JProperty>()
            .Where(p => p.Value is JValue && (p.Path.Contains("artist") || p.Path.Contains("album")) &&
                        (p.Name is "id" or "name" or "title"))
            .Select(p => $"{p.Path}={p.Value}").Take(20);
        output.WriteLine(string.Join("\n", artistAlbumPaths));

        foreach (var t in tracks.Take(8))
            output.WriteLine($"{t.SelectToken("base_info.id")} | {t.SelectToken("base_info.name")} | " +
                             $"{string.Join("/", t.SelectTokens("related_info.artist_links[*].name").Take(3))} | " +
                             $"{t.SelectToken("related_info.album_link.name")} | {t.SelectToken("base_info.duration_ms")}");

        Assert.All(tracks, t => Assert.False(string.IsNullOrWhiteSpace(t.SelectToken("base_info.id")?.ToString())));
        Assert.Contains(tracks, t => t.SelectToken("base_info.name")!.ToString().Contains("晴天"));
    }

    [LiveFact]
    public async Task PlatformSearchPaginates()
    {
        var page1 = await PlatformSearch("周杰伦", "track");
        var group = SearchGroup(page1, "track");
        Assert.NotNull(group);
        output.WriteLine($"group keys: {string.Join(",", group!.Properties().Select(p => p.Name))}");
        var data = (JObject)page1["data"]!;
        output.WriteLine($"data keys: {string.Join(",", data.Properties().Select(p => p.Name))}");

        var next = group.Value<string>("next_cursor") ?? data.Value<string>("next_cursor");
        Assert.False(string.IsNullOrEmpty(next), "无翻页游标");

        var page2 = await PlatformSearch("周杰伦", "track", next!);
        var ids1 = SearchEntities(page1, "track").Select(e => e.SelectToken("track.base_info.id")?.ToString()).ToHashSet();
        var ids2 = SearchEntities(page2, "track").Select(e => e.SelectToken("track.base_info.id")?.ToString()).ToList();
        // Results shift a little between requests (each search has a new search_id), so pages may overlap.
        Assert.NotEmpty(ids2);
        output.WriteLine($"page1={ids1.Count}, page2={ids2.Count}, next={next}");
    }

    [LiveFact]
    public async Task PlatformAlbumAndPlaylistSearch()
    {
        foreach (var type in new[] { "album", "playlist" })
        {
            var body = await PlatformSearch("周杰伦", type);
            var groups = body.SelectToken("data.result_groups") as JArray;
            var entities = SearchEntities(body, type);
            output.WriteLine($"{type}: err_no={body["err_no"]}, groups={groups?.Count}, " +
                             $"groupIds={string.Join(",", groups?.Select(g => g["id"]) ?? [])}, entities={entities.Count}");
            foreach (var e in entities.Take(3))
                output.WriteLine($"  {e[type]?["id"]} | {e[type]?["name"] ?? e[type]?["title"]}");
        }
    }

    [LiveFact]
    public async Task AppPlaylistDetailReturnsTracks()
    {
        // 先从首页推荐拿一个真实歌单 ID（douyin.com 网页上的歌单 ID 在 App 侧不存在）
        var mix = await AppPost("/luna/discover/mix", new JObject { ["cursor"] = "", ["count"] = 10 });
        var playlistId = mix.Descendants().OfType<JProperty>()
            .Where(p => p.Name == "playlist" && p.Value is JObject o && o["id"] != null)
            .Select(p => p.Value["id"]!.ToString()).FirstOrDefault();
        output.WriteLine($"mix keys: {string.Join(",", mix.Properties().Select(p => p.Name))}, status={mix["status_code"]}, playlistId={playlistId}");
        Assert.False(string.IsNullOrEmpty(playlistId));

        var body = await AppPost("/luna/playlist/detail", new JObject { ["playlist_id"] = playlistId });

        var playlist = (JObject?)body["playlist"];
        output.WriteLine($"top keys: {string.Join(",", body.Properties().Select(p => p.Name))}");
        output.WriteLine($"status: {body["status_code"]} {body["status_info"]}");
        Assert.NotNull(playlist);
        output.WriteLine($"title={playlist!["title"]}, count={playlist["count_tracks"]}, next={body["next_cursor"]}");

        var tracks = (body["media_resources"] as JArray ?? [])
            .Select(m => m.SelectToken("entity.track_wrapper.track")).OfType<JObject>().ToList();
        Assert.NotEmpty(tracks);
        foreach (var t in tracks.Take(3)) output.WriteLine($"  {t["id"]} | {t["name"]}");
    }

    [LiveFact]
    public async Task AppAlbumDetailReturnsTracks()
    {
        var body = await AppGet($"/luna/albums/{AlbumId}", "");

        output.WriteLine($"top keys: {string.Join(",", body.Properties().Select(p => p.Name))}");
        Assert.Equal(AlbumId, body.SelectToken("album_info.id")?.ToString());
        var tracks = body["tracks"] as JArray;
        Assert.NotNull(tracks);
        Assert.NotEmpty(tracks!);
        output.WriteLine($"album={body.SelectToken("album_info.name")}, tracks={tracks!.Count}");
    }

    [LiveFact]
    public async Task AppSeoTrackReturnsLyricContent()
    {
        foreach (var id in new[] { TrackId, ForeignTrackId })
        {
            var body = await AppGet("/luna/h5/seo_track", $"track_id={id}&device_platform=web");
            output.WriteLine($"[{id}] top keys: {string.Join(",", body.Properties().Select(p => p.Name))}");

            var lyric = body["lyric"] as JObject;
            Assert.NotNull(lyric);
            output.WriteLine($"[{id}] lyric keys: {string.Join(",", lyric!.Properties().Select(p => p.Name))}");
            foreach (var p in lyric.Properties().Where(p => p.Name != "content"))
                output.WriteLine($"  {p.Name} = {Truncate(p.Value.ToString(), 200)}");

            var content = lyric.Value<string>("content");
            Assert.False(string.IsNullOrWhiteSpace(content));
            output.WriteLine(Truncate(content!, 500));

            // 记录所有含 trans/lang 的字段名，评估翻译可用性
            var hits = body.Descendants().OfType<JProperty>()
                .Where(p => p.Name.Contains("trans", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.Contains("lang", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{p.Path}={Truncate(p.Value.ToString(), 80)}").Distinct().Take(20).ToList();
            output.WriteLine($"[{id}] trans/lang fields: {(hits.Count == 0 ? "(none)" : string.Join(" ; ", hits))}");
        }
    }

    private async Task<JObject> PlatformSearch(string query, string type, string cursor = "0")
    {
        var token = await PlatformClientToken();
        var searchId = Guid.NewGuid().ToString();
        var payload = new JObject
        {
            ["q"] = query, ["cursor"] = cursor, ["search_id"] = searchId, ["from_search_id"] = searchId,
            ["search_method"] = "input", ["search_type"] = type, ["search_scene"] = "normal",
            ["scene_name"] = "search", ["ab_param"] = "", ["last_viewed_media"] = new JObject(),
            ["common_params"] = new JObject(),
        };

        using var client = NetworkClientFactory.CreateHttpClient(20);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{PlatformHost}/api/luna/v1/platform/search/?{PlatformParams()}");
        request.Headers.TryAddWithoutValidation("User-Agent", SdkUserAgent);
        if (token != null) request.Headers.TryAddWithoutValidation("access-token", token);
        request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {Truncate(text, 300)}");
        Assert.False(string.IsNullOrWhiteSpace(text), "响应为空");
        return JObject.Parse(text);
    }

    private static string? _token;

    private async Task<string?> PlatformClientToken()
    {
        if (_token != null) return _token;
        using var client = NetworkClientFactory.CreateHttpClient(20);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{PlatformTokenUrl}?{PlatformParams()}&trigger_type=cold_start");
        request.Headers.TryAddWithoutValidation("User-Agent", SdkUserAgent);
        using var response = await client.SendAsync(request);
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        output.WriteLine($"client_token err_no={body["err_no"]}, has_token={!string.IsNullOrEmpty(body.Value<string>("client_token"))}");
        return _token = body.Value<int?>("err_no") == 0 ? body.Value<string>("client_token") : null;
    }

    private static string PlatformParams()
    {
        var p = new Dictionary<string, string>
        {
            ["aid"] = "765213", ["app_id"] = "765213", ["app_name"] = "byte_autoservice_lite",
            ["version_code"] = "100010", ["version_name"] = "1.0.10", ["channel"] = "bydSong",
            ["sdk_aid"] = "753999", ["caller_name"] = "luna_open_sdk", ["sdk_version_code"] = "100000100",
            ["sdk_version_name"] = "0.0.1", ["sdk_channel"] = "Test", ["vehicle_type"] = "BYD",
            ["device_brand"] = "BYD", ["device_type"] = "BYD", ["device_platform"] = "android",
            ["resolution"] = "1920x1080", ["os_version"] = "13", ["ac"] = "wifi",
            ["device_id"] = DeviceId, ["did"] = DeviceId, ["iid"] = DeviceId, ["vehicle_id"] = "qishui-" + DeviceId,
            ["_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["_nonce"] = Random.Shared.Next(1_000_000_000).ToString(),
        };
        return string.Join("&", p.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
    }

    private static JObject? SearchGroup(JObject body, string type)
    {
        var groups = (body.SelectToken("data.result_groups") as JArray)?.OfType<JObject>().ToList() ?? [];
        return groups.FirstOrDefault(g => g.Value<string>("id") == type + "s") ?? groups.FirstOrDefault();
    }

    private static List<JObject> SearchEntities(JObject body, string type) =>
        (SearchGroup(body, type)?["data"] as JArray ?? []).Select(i => i["entity"]).OfType<JObject>().ToList();

    private static async Task<JObject> AppPost(string path, JObject payload)
    {
        using var client = NetworkClientFactory.CreateHttpClient(20);
        using var request = new HttpRequestMessage(HttpMethod.Post, AppHost + path);
        request.Headers.TryAddWithoutValidation("User-Agent", AppUserAgent);
        request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {Truncate(text, 300)}");
        Assert.False(string.IsNullOrWhiteSpace(text), "响应为空");
        return JObject.Parse(text);
    }

    private static async Task<JObject> AppGet(string path, string query)
    {
        using var client = NetworkClientFactory.CreateHttpClient(20);
        using var request = new HttpRequestMessage(HttpMethod.Get, AppHost + path + (query.Length > 0 ? "?" + query : ""));
        request.Headers.TryAddWithoutValidation("User-Agent", WebUserAgent);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {Truncate(text, 300)}");
        Assert.False(string.IsNullOrWhiteSpace(text), "响应为空");
        return JObject.Parse(text);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
