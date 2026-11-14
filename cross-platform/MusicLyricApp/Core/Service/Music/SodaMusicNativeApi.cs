using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using Newtonsoft.Json.Linq;
using NLog;

namespace MusicLyricApp.Core.Service.Music;

/// <summary>
/// 汽水音乐游客态接口，无 Cookie、无签名：
/// App 侧 beta-luna.douyin.com 提供歌曲/歌词/专辑/歌单，车机开放平台 open.douyin.com 提供关键词搜索。
/// 均为非公开接口，返回结构可能随时变化。
/// </summary>
public class SodaMusicNativeApi(Func<string> deviceIdFunc, Func<HttpClient>? httpClientFactory = null)
{
    private const string AppHost = "https://beta-luna.douyin.com";
    private const string AppUserAgent = "Luna/19.1.0 Android";
    private const string WebUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36";

    private const string PlatformSearchUrl = "https://open.douyin.com/api/luna/v1/platform/search/";
    private const string PlatformTokenUrl = "https://open.qishui.com/api/luna/v1/platform/client/token/get/";
    private const string SdkUserAgent = "LunaOpenSDK/1.0.10";

    /// <summary>歌单最多翻页次数，避免异常游标导致死循环。</summary>
    private const int MaxPlaylistPages = 50;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly object _tokenLock = new();
    private string? _clientToken;

    public SodaMusicBean.SeoTrackResult GetTrack(string trackId)
    {
        return AppGet("/luna/h5/seo_track?device_platform=web&track_id=" + Uri.EscapeDataString(trackId))
            .ToEntity<SodaMusicBean.SeoTrackResult>();
    }

    public SodaMusicBean.AlbumResult GetAlbum(string albumId)
    {
        return AppGet("/luna/albums/" + Uri.EscapeDataString(albumId)).ToEntity<SodaMusicBean.AlbumResult>();
    }

    /// <summary>歌单详情，翻完所有页后把曲目合并到第一页结果里。</summary>
    public SodaMusicBean.PlaylistResult GetPlaylist(string playlistId)
    {
        var first = GetPlaylistPage(playlistId, null);
        if (!first.IsSuccess() || first.Playlist == null)
        {
            return first;
        }

        var tracks = new List<SodaMusicBean.MediaResource>(first.MediaResources ?? []);
        var seenCursors = new HashSet<string>();
        var cursor = first.NextCursor;
        var lastCount = tracks.Count;

        for (var page = 1; page < MaxPlaylistPages && lastCount > 0 &&
                           !string.IsNullOrEmpty(cursor) && seenCursors.Add(cursor); page++)
        {
            var next = GetPlaylistPage(playlistId, cursor);
            if (!next.IsSuccess())
            {
                _logger.Warn("SodaMusic playlist {Id} page {Cursor} failed: {Msg}", playlistId, cursor,
                    next.StatusMessage());
                break;
            }

            var items = next.MediaResources ?? [];
            tracks.AddRange(items);
            lastCount = items.Count;
            cursor = next.NextCursor;
        }

        first.MediaResources = tracks;
        first.NextCursor = null;
        return first;
    }

    private SodaMusicBean.PlaylistResult GetPlaylistPage(string playlistId, string? cursor)
    {
        var body = new JObject { ["playlist_id"] = playlistId };
        if (!string.IsNullOrEmpty(cursor))
        {
            body["cursor"] = cursor;
        }

        return AppPost("/luna/playlist/detail", body).ToEntity<SodaMusicBean.PlaylistResult>();
    }

    /// <summary>单曲关键词搜索（游客态下专辑、歌单搜索恒为空）。</summary>
    public SodaMusicBean.SearchResult SearchTrack(string keyword, string cursor = "0")
    {
        var searchId = Guid.NewGuid().ToString();
        var body = new JObject
        {
            ["q"] = keyword,
            ["cursor"] = cursor,
            ["search_id"] = searchId,
            ["from_search_id"] = searchId,
            ["search_method"] = "input",
            ["search_type"] = "track",
            ["search_scene"] = "normal",
            ["scene_name"] = "search",
            ["ab_param"] = "",
            ["last_viewed_media"] = new JObject(),
            ["common_params"] = new JObject(),
        };

        for (var attempt = 0; ; attempt++)
        {
            var token = ClientToken();
            var result = PlatformPost(PlatformSearchUrl, body, token).ToEntity<SodaMusicBean.SearchResult>();
            if (result.ErrNo == SodaMusicBean.ErrTokenExpired && attempt == 0)
            {
                lock (_tokenLock) _clientToken = null;
                continue;
            }

            return result;
        }
    }

    /// <summary>尽力获取 client_token；拿不到时搜索也能裸调，所以失败只记日志。</summary>
    private string? ClientToken()
    {
        lock (_tokenLock)
        {
            if (_clientToken != null)
            {
                return _clientToken;
            }
        }

        try
        {
            var text = Send(new HttpRequestMessage(HttpMethod.Get,
                PlatformTokenUrl + "?" + PlatformParams() + "&trigger_type=cold_start"), SdkUserAgent);
            var result = text.ToEntity<SodaMusicBean.TokenResult>();
            if (result.ErrNo == 0 && !string.IsNullOrEmpty(result.ClientToken))
            {
                lock (_tokenLock) _clientToken = result.ClientToken;
                return result.ClientToken;
            }

            _logger.Warn("SodaMusic client token failed: {ErrNo} {Msg}", result.ErrNo, result.ErrMsg);
        }
        // TaskCanceledException is HttpClient's timeout: a slow token server must not fail the search either.
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or Newtonsoft.Json.JsonException)
        {
            _logger.Warn(ex, "SodaMusic client token failed");
        }

        return null;
    }

    /// <summary>车机 SDK 公共参数。</summary>
    private string PlatformParams()
    {
        var deviceId = deviceIdFunc.Invoke();
        var parameters = new Dictionary<string, string>
        {
            ["aid"] = "765213",
            ["app_id"] = "765213",
            ["app_name"] = "byte_autoservice_lite",
            ["version_code"] = "100010",
            ["version_name"] = "1.0.10",
            ["channel"] = "bydSong",
            ["sdk_aid"] = "753999",
            ["caller_name"] = "luna_open_sdk",
            ["sdk_version_code"] = "100000100",
            ["sdk_version_name"] = "0.0.1",
            ["sdk_channel"] = "Test",
            ["vehicle_type"] = "BYD",
            ["device_brand"] = "BYD",
            ["device_type"] = "BYD",
            ["device_platform"] = "android",
            ["resolution"] = "1920x1080",
            ["os_version"] = "13",
            ["ac"] = "wifi",
            ["device_id"] = deviceId,
            ["did"] = deviceId,
            ["iid"] = deviceId,
            ["vehicle_id"] = "qishui-" + deviceId,
            ["_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["_nonce"] = Random.Shared.Next(1_000_000_000).ToString(),
        };
        return string.Join("&", parameters.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value)));
    }

    /// <summary>新的设备号：19 位数字，7 开头。</summary>
    public static string NewDeviceId()
    {
        return "7" + string.Concat(Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(10)));
    }

    private string AppGet(string pathAndQuery)
    {
        return Send(new HttpRequestMessage(HttpMethod.Get, AppHost + pathAndQuery), WebUserAgent);
    }

    private string AppPost(string path, JObject body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, AppHost + path)
        {
            Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8,
                "application/json")
        };
        return Send(request, AppUserAgent);
    }

    private string PlatformPost(string url, JObject body, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url + "?" + PlatformParams())
        {
            Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8,
                "application/json")
        };
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("access-token", token);
        }

        return Send(request, SdkUserAgent);
    }

    private string Send(HttpRequestMessage request, string userAgent)
    {
        using (request)
        {
            using var client = httpClientFactory?.Invoke() ?? NetworkClientFactory.CreateHttpClient();
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                // 风控或缺少签名时接口返回 200 + 空 body
                throw new HttpRequestException("SodaMusic returned an empty response: " + request.RequestUri?.AbsolutePath);
            }

            return text;
        }
    }
}
