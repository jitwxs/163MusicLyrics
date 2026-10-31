using System;
using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Service.Music;

public class QQMusicApi : MusicCacheableApi
{
    private readonly QQMusicNativeApi _api;

    public QQMusicApi(Func<string> cookieAction, QQMusicNativeApi? api = null)
    {
        _api = api ?? new QQMusicNativeApi(cookieAction);
    }

    protected override SearchSourceEnum Source0()
    {
        return SearchSourceEnum.QQ_MUSIC;
    }

    protected override ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId)
    {
        var resp = _api.GetPlaylist(playlistId);
        var playlist = resp?.Cdlist?.FirstOrDefault(item => item != null);
        if (resp?.Code != 0 || playlist == null)
        {
            return ResultVo<PlaylistVo>.Failure(ErrorMsgConst.PLAYLIST_NOT_EXIST);
        }

        playlist.SongList = (playlist.SongList ?? [])
            .Where(song => song != null && !string.IsNullOrWhiteSpace(song.Id) && !string.IsNullOrWhiteSpace(song.Mid))
            .ToArray();

        foreach (var song in playlist.SongList)
        {
            GlobalCache.DoCache(Source(), CacheType.QQ_MUSIC_SONG, song.Id, song);
            GlobalCache.DoCache(Source(), CacheType.QQ_MUSIC_SONG, song.Mid, song);
        }

        return new ResultVo<PlaylistVo>(resp.Convert());
    }

    protected override ResultVo<AlbumVo> GetAlbumVo0(string albumId)
    {
        var resp = _api.GetAlbum(albumId);
        if (resp.Code == 0)
        {
            return new ResultVo<AlbumVo>(resp.Convert());
        }
        else
        {
            return ResultVo<AlbumVo>.Failure(ErrorMsgConst.ALBUM_NOT_EXIST);
        }
    }

    protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
    {
        var result = new Dictionary<string, ResultVo<SongVo>>();

        foreach (var songId in songIds)
        {
            ResultVo<QQMusicBean.Song> SongCacheFunc()
            {
                var resp = _api.GetSong(songId);
                return resp == null || resp.IsIllegal()
                    ? ResultVo<QQMusicBean.Song>.Failure(ErrorMsgConst.SONG_NOT_EXIST)
                    : new ResultVo<QQMusicBean.Song>(resp.Data[0]);
            }

            var songRes = GlobalCache.Process(Source(), CacheType.QQ_MUSIC_SONG, songId, SongCacheFunc);
            if (songRes.IsSuccess())
            {
                result[songId] = new ResultVo<SongVo>(new SongVo
                {
                    Id = songRes.Data.Id,
                    DisplayId = songRes.Data.Mid,
                    Pics = string.IsNullOrWhiteSpace(songRes.Data.Album?.Pmid)
                        ? string.Empty
                        : $"https://y.qq.com/music/photo_new/T002R800x800M000{songRes.Data.Album.Pmid}.jpg",
                    // #212 QQ music need use title attribute as song name
                    Name = GlobalUtils.GetOrDefault(songRes.Data.Title, songRes.Data.Name ?? string.Empty),
                    Singer = songRes.Data.Singer?.Where(singer => singer != null)
                        .Select(singer => singer.Name ?? string.Empty).ToArray() ?? [],
                    Album = songRes.Data.Album?.Name ?? string.Empty,
                    Duration = songRes.Data.Interval * 1000,
                    PublishDate = songRes.Data.Time_public ?? string.Empty
                });
            }
            else
            {
                result[songId] = ResultVo<SongVo>.Failure(songRes.ErrorMsg);
            }
        }

        return result;
    }

    protected override ResultVo<string> GetSongLink0(string songId)
    {
        return _api.GetSongLink(songId);
    }

    protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
    {
        var resp = _api.GetLyric(id);
        if (resp.Code != 0)
        {
            return ResultVo<LyricVo>.Failure(ErrorMsgConst.LRC_NOT_EXIST);
        }

        var vo = new LyricVo
        {
            SearchSource = SearchSourceEnum.QQ_MUSIC
        };

        if (isVerbatim)
        {
            vo.Lyric = VerbatimLyricUtils.DealVerbatimLyric4QQMusic(resp.Lyric);
            vo.TranslateLyric = VerbatimLyricUtils.DealVerbatimLyric4QQMusic(resp.Trans);
            vo.TransliterationLyric = VerbatimLyricUtils.DealVerbatimLyric4QQMusic(resp.Roma);
        }
        else
        {
            vo.Lyric = VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4QQMusic(resp.Lyric);
            vo.TranslateLyric = VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4QQMusic(resp.Trans);
            vo.TransliterationLyric = VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4QQMusic(resp.Roma);
        }

        return new ResultVo<LyricVo>(vo);
    }

    protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
    {
        var resp = _api.Search(keyword, searchType);

        if (resp.Code == 0 && resp.Req_1.Code == 0 && resp.Req_1.Data.Code == 0)
        {
            return new ResultVo<SearchResultVo>(resp.Req_1.Data.Body.Convert(searchType));
        }

        return ResultVo<SearchResultVo>.Failure(ErrorMsgConst.NETWORK_ERROR);
    }
}
