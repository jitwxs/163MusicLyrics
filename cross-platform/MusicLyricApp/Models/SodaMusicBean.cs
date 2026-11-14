using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MusicLyricApp.Models;

/// <summary>
/// 汽水音乐接口返回结构，只声明用到的字段。
/// App 侧接口（beta-luna.douyin.com）出错时带 status_code，成功时一般不带；
/// 车机开放平台接口（open.douyin.com）用 err_no。
/// </summary>
public static class SodaMusicBean
{
    /// <summary>未被收录的歌曲等资源。</summary>
    public const int StatusResourceNotFound = 1000005;

    /// <summary>平台接口 client_token 过期。</summary>
    public const int ErrTokenExpired = 28001008;

    public class AppResult
    {
        [JsonProperty("status_code")] public int StatusCode { get; set; }

        [JsonProperty("status_info")] public StatusInfo? StatusInfo { get; set; }

        public bool IsSuccess() => StatusCode == 0;

        public string StatusMessage() => StatusInfo?.StatusMsg ?? StatusCode.ToString();
    }

    public class StatusInfo
    {
        [JsonProperty("status_msg")] public string? StatusMsg { get; set; }
    }

    public class ImageUrl
    {
        [JsonProperty("uri")] public string? Uri { get; set; }

        [JsonProperty("urls")] public string[]? Urls { get; set; }

        /// <summary>完整图片地址，500x500。</summary>
        public string ToUrl()
        {
            var host = Urls?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            return string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(Uri)
                ? string.Empty
                : host + Uri + "~c5_500x500.jpg";
        }
    }

    public class Artist
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }
    }

    public class Album
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }

        /// <summary>Unix 秒。</summary>
        [JsonProperty("release_date")] public long ReleaseDate { get; set; }

        [JsonProperty("url_cover")] public ImageUrl? UrlCover { get; set; }
    }

    public class Track
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }

        /// <summary>毫秒。</summary>
        [JsonProperty("duration")] public long Duration { get; set; }

        [JsonProperty("album")] public Album? Album { get; set; }

        [JsonProperty("artists")] public List<Artist>? Artists { get; set; }

        public string[] SingerNames() => (Artists ?? [])
            .Select(a => a?.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToArray();
    }

    /// <summary>GET /luna/h5/seo_track：歌曲信息、歌词、翻译一次返回。</summary>
    public class SeoTrackResult : AppResult
    {
        [JsonProperty("lyric")] public Lyric? Lyric { get; set; }

        [JsonProperty("seo_track")] public SeoTrack? SeoTrack { get; set; }

        public Track? Track => SeoTrack?.Track;
    }

    public class SeoTrack
    {
        [JsonProperty("track")] public Track? Track { get; set; }
    }

    public class Lyric
    {
        /// <summary>krc：[行开始,行时长]&lt;字偏移,字时长,0&gt;字…，字偏移相对行开始。</summary>
        [JsonProperty("content")] public string? Content { get; set; }

        [JsonProperty("type")] public string? Type { get; set; }

        /// <summary>语言 → LRC，目前只见过 cn。</summary>
        [JsonProperty("translations")] public Dictionary<string, string>? Translations { get; set; }
    }

    /// <summary>GET /luna/albums/{id}</summary>
    public class AlbumResult : AppResult
    {
        [JsonProperty("album_info")] public AlbumInfo? AlbumInfo { get; set; }

        [JsonProperty("tracks")] public List<Track>? Tracks { get; set; }
    }

    public class AlbumInfo : Album
    {
        [JsonProperty("artists")] public List<Artist>? Artists { get; set; }

        [JsonProperty("company")] public string? Company { get; set; }

        [JsonProperty("count_tracks")] public long CountTracks { get; set; }
    }

    /// <summary>POST /luna/playlist/detail，按 next_cursor 翻页。</summary>
    public class PlaylistResult : AppResult
    {
        [JsonProperty("playlist")] public Playlist? Playlist { get; set; }

        [JsonProperty("media_resources")] public List<MediaResource>? MediaResources { get; set; }

        [JsonProperty("next_cursor")] public string? NextCursor { get; set; }
    }

    public class Playlist
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("title")] public string? Title { get; set; }

        [JsonProperty("desc")] public string? Desc { get; set; }

        [JsonProperty("owner")] public PlaylistOwner? Owner { get; set; }
    }

    public class PlaylistOwner
    {
        [JsonProperty("nickname")] public string? Nickname { get; set; }
    }

    public class MediaResource
    {
        [JsonProperty("entity")] public MediaEntity? Entity { get; set; }
    }

    public class MediaEntity
    {
        [JsonProperty("track_wrapper")] public TrackWrapper? TrackWrapper { get; set; }
    }

    public class TrackWrapper
    {
        [JsonProperty("track")] public Track? Track { get; set; }
    }

    /// <summary>车机开放平台接口公共返回。</summary>
    public class PlatformResult
    {
        [JsonProperty("err_no")] public int ErrNo { get; set; }

        [JsonProperty("err_msg")] public string? ErrMsg { get; set; }
    }

    public class TokenResult : PlatformResult
    {
        [JsonProperty("client_token")] public string? ClientToken { get; set; }
    }

    /// <summary>POST /api/luna/v1/platform/search/</summary>
    public class SearchResult : PlatformResult
    {
        [JsonProperty("data")] public SearchData? Data { get; set; }

        /// <summary>按 "{type}s" 选组，没有时退回第一组。</summary>
        public SearchGroup? Group(string type)
        {
            var groups = Data?.ResultGroups ?? [];
            return groups.FirstOrDefault(g => g?.Id == type + "s") ?? groups.FirstOrDefault(g => g != null);
        }
    }

    public class SearchData
    {
        [JsonProperty("result_groups")] public List<SearchGroup>? ResultGroups { get; set; }
    }

    public class SearchGroup
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("has_more")] public bool HasMore { get; set; }

        [JsonProperty("next_cursor")] public string? NextCursor { get; set; }

        [JsonProperty("data")] public List<SearchItem>? Data { get; set; }
    }

    public class SearchItem
    {
        [JsonProperty("entity")] public SearchEntity? Entity { get; set; }
    }

    public class SearchEntity
    {
        [JsonProperty("track")] public SearchTrack? Track { get; set; }
    }

    public class SearchTrack
    {
        [JsonProperty("base_info")] public SearchTrackBaseInfo? BaseInfo { get; set; }

        [JsonProperty("related_info")] public SearchTrackRelatedInfo? RelatedInfo { get; set; }
    }

    public class SearchTrackBaseInfo
    {
        [JsonProperty("id")] public string? Id { get; set; }

        [JsonProperty("name")] public string? Name { get; set; }

        [JsonProperty("duration_ms")] public long DurationMs { get; set; }
    }

    public class SearchTrackRelatedInfo
    {
        [JsonProperty("album_link")] public Album? AlbumLink { get; set; }

        [JsonProperty("artist_links")] public List<Artist>? ArtistLinks { get; set; }
    }
}
