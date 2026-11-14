using System;
using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using NLog;

namespace MusicLyricApp.Core.Service.Music;

/// <summary>
/// 汽水音乐。歌曲 ID 即 track_id（纯数字），内部 ID 与展示 ID 相同。
/// 支持单曲/专辑/歌单获取、逐行与逐字歌词、中文翻译、单曲关键词搜索；不支持歌曲直链和专辑/歌单搜索。
/// </summary>
public class SodaMusicApi : MusicCacheableApi
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly SodaMusicNativeApi _api;

    public SodaMusicApi(Func<string> deviceIdFunc, SodaMusicNativeApi? api = null)
    {
        _api = api ?? new SodaMusicNativeApi(deviceIdFunc);
    }

    protected override SearchSourceEnum Source0()
    {
        return SearchSourceEnum.SODA_MUSIC;
    }

    protected override ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId)
    {
        var resp = _api.GetPlaylist(playlistId);
        if (!resp.IsSuccess() || resp.Playlist == null)
        {
            _logger.Warn("SodaMusic playlist {Id} failed: {Msg}", playlistId, resp.StatusMessage());
            return ResultVo<PlaylistVo>.Failure(ErrorMsgConst.PLAYLIST_NOT_EXIST);
        }

        var songs = (resp.MediaResources ?? [])
            .Select(m => m?.Entity?.TrackWrapper?.Track)
            .Where(t => !string.IsNullOrWhiteSpace(t?.Id))
            .Select(t => ToSimpleSongVo(t!))
            .GroupBy(s => s.Id).Select(g => g.First())
            .ToArray();

        return new ResultVo<PlaylistVo>(new PlaylistVo
        {
            Name = resp.Playlist.Title ?? string.Empty,
            AuthorName = resp.Playlist.Owner?.Nickname ?? string.Empty,
            Description = resp.Playlist.Desc ?? string.Empty,
            SimpleSongVos = songs
        });
    }

    protected override ResultVo<AlbumVo> GetAlbumVo0(string albumId)
    {
        var resp = _api.GetAlbum(albumId);
        if (!resp.IsSuccess() || resp.AlbumInfo == null)
        {
            _logger.Warn("SodaMusic album {Id} failed: {Msg}", albumId, resp.StatusMessage());
            return ResultVo<AlbumVo>.Failure(ErrorMsgConst.ALBUM_NOT_EXIST);
        }

        return new ResultVo<AlbumVo>(new AlbumVo
        {
            Name = resp.AlbumInfo.Name ?? string.Empty,
            Company = resp.AlbumInfo.Company ?? string.Empty,
            TimePublic = FormatReleaseDate(resp.AlbumInfo.ReleaseDate),
            SimpleSongVos = (resp.Tracks ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t?.Id))
                .Select(ToSimpleSongVo)
                .ToArray()
        });
    }

    protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
    {
        var result = new Dictionary<string, ResultVo<SongVo>>();
        foreach (var songId in songIds)
        {
            var track = GetTrack(songId);
            result[songId] = track.IsSuccess()
                ? new ResultVo<SongVo>(ToSongVo(songId, track.Data.Track!))
                : ResultVo<SongVo>.Failure(track.ErrorMsg);
        }

        return result;
    }

    protected override ResultVo<string> GetSongLink0(string songId)
    {
        // 完整音源需要会员登录和客户端签名，不提供
        return ResultVo<string>.Failure(ErrorMsgConst.FUNCTION_NOT_SUPPORT);
    }

    protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
    {
        var track = GetTrack(displayId);
        if (!track.IsSuccess())
        {
            return ResultVo<LyricVo>.Failure(track.ErrorMsg);
        }

        var lyric = track.Data.Lyric;
        var vo = new LyricVo
        {
            SearchSource = SearchSourceEnum.SODA_MUSIC,
            Duration = track.Data.Track?.Duration ?? 0
        };

        if (lyric == null || string.IsNullOrWhiteSpace(lyric.Content))
        {
            return new ResultVo<LyricVo>(vo);
        }

        if (isVerbatim)
        {
            // 与网易云一致：逐字模式只输出原文，翻译只有逐行时间轴
            vo.Lyric = VerbatimLyricUtils.DealVerbatimLyric4SodaMusic(lyric.Content);
        }
        else
        {
            vo.Lyric = VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4SodaMusic(lyric.Content);
            vo.TranslateLyric = TranslationOf(lyric);
        }

        return new ResultVo<LyricVo>(vo);
    }

    protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
    {
        if (searchType != SearchTypeEnum.SONG_ID)
        {
            return ResultVo<SearchResultVo>.Failure(ErrorMsgConst.SODA_SEARCH_SONG_ONLY);
        }

        var resp = _api.SearchTrack(keyword);
        if (resp.ErrNo != 0)
        {
            _logger.Error("SodaMusic search failed, errNo: {ErrNo}, msg: {Msg}", resp.ErrNo, resp.ErrMsg);
            return ResultVo<SearchResultVo>.Failure(ErrorMsgConst.NETWORK_ERROR);
        }

        var vo = new SearchResultVo
        {
            SearchSource = SearchSourceEnum.SODA_MUSIC,
            SearchType = SearchTypeEnum.SONG_ID
        };

        foreach (var track in (resp.Group("track")?.Data ?? []).Select(i => i?.Entity?.Track))
        {
            var info = track?.BaseInfo;
            if (string.IsNullOrWhiteSpace(info?.Id))
            {
                continue;
            }

            vo.SongVos.Add(new SearchResultVo.SongSearchResultVo
            {
                DisplayId = info.Id,
                Title = info.Name ?? string.Empty,
                AuthorName = (track!.RelatedInfo?.ArtistLinks ?? [])
                    .Select(a => a?.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToArray(),
                AlbumName = track.RelatedInfo?.AlbumLink?.Name ?? string.Empty,
                Duration = info.DurationMs
            });
        }

        return new ResultVo<SearchResultVo>(vo);
    }

    /// <summary>歌曲详情与歌词来自同一个接口，缓存后歌词不再重复请求。</summary>
    private ResultVo<SodaMusicBean.SeoTrackResult> GetTrack(string trackId)
    {
        return GlobalCache.Process(Source(), CacheType.SODA_MUSIC_TRACK, trackId, () =>
        {
            var resp = _api.GetTrack(trackId);
            if (resp.IsSuccess() && !string.IsNullOrWhiteSpace(resp.Track?.Id))
            {
                return new ResultVo<SodaMusicBean.SeoTrackResult>(resp);
            }

            _logger.Warn("SodaMusic track {Id} failed: {Msg}", trackId, resp.StatusMessage());
            return ResultVo<SodaMusicBean.SeoTrackResult>.Failure(ErrorMsgConst.SONG_NOT_EXIST);
        });
    }

    private static string TranslationOf(SodaMusicBean.Lyric lyric)
    {
        var translations = lyric.Translations;
        if (translations == null || translations.Count == 0)
        {
            return string.Empty;
        }

        return translations.TryGetValue("cn", out var cn) && !string.IsNullOrWhiteSpace(cn)
            ? cn
            : translations.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    private static SimpleSongVo ToSimpleSongVo(SodaMusicBean.Track track)
    {
        return new SimpleSongVo
        {
            Id = track.Id!,
            DisplayId = track.Id!,
            Name = track.Name ?? string.Empty,
            Singer = track.SingerNames()
        };
    }

    private static SongVo ToSongVo(string songId, SodaMusicBean.Track track)
    {
        return new SongVo
        {
            Id = track.Id ?? songId,
            DisplayId = songId,
            Name = track.Name ?? string.Empty,
            Singer = track.SingerNames(),
            Album = track.Album?.Name ?? string.Empty,
            Pics = track.Album?.UrlCover?.ToUrl() ?? string.Empty,
            Duration = track.Duration,
            PublishDate = FormatReleaseDate(track.Album?.ReleaseDate ?? 0)
        };
    }

    /// <summary>release_date 为 Unix 秒，按东八区输出 yyyy-MM-dd。</summary>
    private static string FormatReleaseDate(long seconds)
    {
        return seconds > 0 ? GlobalUtils.FormatDate(seconds * 1000).Split(' ')[0] : string.Empty;
    }
}
