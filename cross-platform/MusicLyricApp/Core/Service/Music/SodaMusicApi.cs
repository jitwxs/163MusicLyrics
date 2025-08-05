using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using System.Linq; // Added for Select

namespace MusicLyricApp.Core.Service.Music;

public class SodaMusicApi : MusicCacheableApi
{
    protected override SearchSourceEnum Source0() => SearchSourceEnum.SODA_MUSIC;

    protected override ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId)
    {
        return ResultVo<PlaylistVo>.Failure(ErrorMsgConst.FUNCTION_NOT_SUPPORT);
    }

    protected override ResultVo<AlbumVo> GetAlbumVo0(string albumId)
    {
        return ResultVo<AlbumVo>.Failure(ErrorMsgConst.FUNCTION_NOT_SUPPORT);
    }

    protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
    {
        // 只支持单曲，songIds[0] 为分享链接
        var result = new Dictionary<string, ResultVo<SongVo>>();
        foreach (var songId in songIds)
        {
            var info = GetSodaMusicInfo(songId, out var debugInfo);
            if (info != null)
            {
                result[songId] = new ResultVo<SongVo>(info.Item1);
            }
            else
            {
                result[songId] = ResultVo<SongVo>.Failure(debugInfo);
            }
        }
        return result;
    }

    protected override ResultVo<string> GetSongLink0(string songId)
    {
        var info = GetSodaMusicInfo(songId, out var debugInfo);
        if (info != null)
        {
            return new ResultVo<string>(info.Item1.Pics); // 用 Pics 字段临时存放音频直链
        }
        return ResultVo<string>.Failure(debugInfo);
    }

    protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
    {
        var info = GetSodaMusicInfo(id, out var debugInfo);
        if (info != null)
        {
            return new ResultVo<LyricVo>(info.Item2);
        }
        return ResultVo<LyricVo>.Failure(debugInfo);
    }

    protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
    {
        // 只支持单曲分享链接
        var info = GetSodaMusicInfo(keyword, out var debugInfo);
        if (info != null)
        {
            var result = new SearchResultVo
            {
                SearchSource = SearchSourceEnum.SODA_MUSIC,
                SearchType = SearchTypeEnum.SONG_ID
            };
            result.SongVos.Add(new SearchResultVo.SongSearchResultVo
            {
                DisplayId = keyword,
                Title = info.Item1.Name,
                AuthorName = info.Item1.Singer,
                AlbumName = info.Item1.Album,
                Duration = info.Item1.Duration
            });
            return new ResultVo<SearchResultVo>(result);
        }
        return ResultVo<SearchResultVo>.Failure(debugInfo);
    }

    /// <summary>
    /// 获取汽水音乐信息，返回 (SongVo, LyricVo)
    /// </summary>
    private static Tuple<SongVo, LyricVo> GetSodaMusicInfo(string shareUrl, out string debugInfo)
    {
        debugInfo = $"[收到的shareUrl]: {shareUrl}\n";
        if (string.IsNullOrWhiteSpace(shareUrl) || !Uri.IsWellFormedUriString(shareUrl, UriKind.Absolute))
        {
            debugInfo += $"[错误] 输入的链接不是合法的绝对URL: {shareUrl}";
            return null;
        }
        try
        {
            // 在线获取页面源码，模拟浏览器UA，自动重定向
            var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true };
            using var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            var html = client.GetStringAsync(shareUrl).Result;
            debugInfo += $"[HTML Length]: {html.Length}\n";

            // soda-test.py: match = re.search(r'_ROUTER_DATA\s*=\s*({.*});', response.text, re.DOTALL)
            // 这里用贪婪匹配，确保提取完整 JSON
            // 使用非贪婪正则，确保只提取第一个 JSON 对象
            var match = Regex.Match(html, @"_ROUTER_DATA\s*=\s*({.*?});", RegexOptions.Singleline);
            if (!match.Success)
            {
                debugInfo += "[正则未命中 _ROUTER_DATA]，尝试 Substring 提取\n";
                var start = html.IndexOf("_ROUTER_DATA = ");
                if (start >= 0)
                {
                    start += "_ROUTER_DATA = ".Length;
                    var end = html.IndexOf(";", start);
                    if (end > start)
                    {
                        var json = html.Substring(start, end - start).Trim();
                        while (json.EndsWith(";") || json.EndsWith("\n") || json.EndsWith("\r"))
                            json = json.Substring(0, json.Length - 1).TrimEnd();
                        debugInfo += $"[最终传入ParseSodaJson的JSON末尾100字符]: {json.Substring(Math.Max(0, json.Length - 100))}\n";
                        return ParseSodaJson(json, shareUrl, out debugInfo);
                    }
                    debugInfo += "[Substring未找到分号结尾]";
                }
                else
                {
                    debugInfo += "[未找到 _ROUTER_DATA = 位置]";
                }
                return null;
            }
            else
            {
                var jsonStr = match.Groups[1].Value;
                debugInfo += $"[正则JSON片段末尾100字符]: {jsonStr.Substring(Math.Max(0, jsonStr.Length - 100))}\n";
                return ParseSodaJson(jsonStr, shareUrl, out debugInfo);
            }
        }
        catch (Exception ex)
        {
            debugInfo += $"[异常]: {ex.Message}\n{ex.StackTrace}";
            return null;
        }
    }

    // 完全参考 soda-test.py 的 JSON 字段提取
    private static Tuple<SongVo, LyricVo> ParseSodaJson(string json, string shareUrl, out string debugInfo)
    {
        debugInfo = "";
        try
        {
            var obj = JsonUtils.ToJObject(json);
            debugInfo += $"[JObject keys]: {string.Join(",", obj.Properties().Select(p => p.Name))}\n";
            // 按页面结构，直接用 SelectToken
            var option = obj.SelectToken("loaderData.track_page.audioWithLyricsOption");
            if (option == null)
            {
                debugInfo += "[audioWithLyricsOption] 路径为 null\n";
                return null;
            }

            // 字段提取
            var trackId = option.Value<string>("track_id"); // 仅debug用
            var trackName = option.Value<string>("trackName") ?? option.Value<string>("name");
            var artistName = option.Value<string>("artistName");
            var duration = option.Value<double?>("duration") ?? 0;
            var url = option.Value<string>("url");
            var lyricsToken = option.SelectToken("lyrics.sentences");
            string lyric = "";
            if (lyricsToken != null && lyricsToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            {
                // 拼接为标准LRC格式
                var sb = new System.Text.StringBuilder();
                foreach (var l in lyricsToken)
                {
                    var startMs = l.Value<long?>("startMs") ?? 0;
                    var text = l.Value<string>("text") ?? "";
                    var timestamp = MsToLrcTimestamp(startMs);
                    sb.Append(timestamp).Append(text).Append("\n");
                }
                lyric = sb.ToString().TrimEnd();
            }
            else
            {
                // 兼容无逐句结构
                lyric = option.SelectToken("lyrics.text")?.ToString() ?? "";
            }

            // 构造 SongVo，所有key都用分享链接
            var songVo = new SongVo
            {
                Id = shareUrl,
                DisplayId = shareUrl,
                Name = trackName ?? "",
                Singer = string.IsNullOrWhiteSpace(artistName) ? new string[0] : new[] { artistName },
                Album = "",
                Pics = "",
                Duration = (long)(duration * 1000) // 转为ms
            };

            // 构造 LyricVo
            var lyricVo = new LyricVo
            {
                SearchSource = SearchSourceEnum.SODA_MUSIC,
                Lyric = lyric,
                TranslateLyric = "",
                TransliterationLyric = "",
                Duration = (long)(duration * 1000)
            };

            debugInfo += $"[track_id]: {trackId}\n";
            debugInfo += $"[trackName]: {trackName}, [artistName]: {artistName}, [duration]: {duration}\n";
            debugInfo += $"[lyric preview]: {lyric?.Substring(0, Math.Min(100, lyric.Length))}\n";
            debugInfo += $"[lyric length]: {lyric?.Length ?? 0}\n";

            return Tuple.Create(songVo, lyricVo);
        }
        catch (Exception ex)
        {
            debugInfo += $"[Parse异常]: {ex.Message}\n{ex.StackTrace}";
            return null;
        }
    }

    // 新增：毫秒转LRC时间戳
    private static string MsToLrcTimestamp(long ms)
    {
        var totalSeconds = ms / 1000;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var milliseconds = ms % 1000;
        return $"[{minutes:D2}:{seconds:D2}.{milliseconds:D3}]";
    }
} 