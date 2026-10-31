using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using NLog;

namespace MusicLyricApp.Core.Service.Music;

public class NetEaseMusicApi : MusicCacheableApi
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        
    private readonly NetEaseMusicNativeApi _api;

    public NetEaseMusicApi(Func<string> cookieFunc, NetEaseMusicNativeApi? api = null)
    {
        _api = api ?? new NetEaseMusicNativeApi(cookieFunc);
    }

    protected override SearchSourceEnum Source0()
    {
        return SearchSourceEnum.NET_EASE_MUSIC;
    }

    protected override ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId)
    {
        var resp = _api.GetPlaylist(playlistId);

        if (resp.Code == 200)
        {
            var songIds = resp.Playlist.TrackIds.Select(e => e.Id.ToString()).ToArray();

            SimpleSongVo[] simpleSongVos;
            if (songIds.Length > 0)
            {
                simpleSongVos = new SimpleSongVo[songIds.Length];
                var songVo = GetSongVo0(songIds);
                for (var i = 0; i < songIds.Length; i++)
                {
                    simpleSongVos[i] = songVo[songIds[i]].Data;
                }
            }
            else
            {
                simpleSongVos = [];
            }
                
            return new ResultVo<PlaylistVo>(resp.Convert(simpleSongVos));
        } 
        else if (resp.Code == 20001)
        {
            return ResultVo<PlaylistVo>.Failure(ErrorMsgConst.NEED_LOGIN);
        }
        else
        {
            return ResultVo<PlaylistVo>.Failure(ErrorMsgConst.PLAYLIST_NOT_EXIST);
        }
    }

    protected override ResultVo<AlbumVo> GetAlbumVo0(string albumId)
    {
        var resp = _api.GetAlbum(albumId);
        if (resp.Code == 200)
        {
            // cache song
            GlobalCache.DoCache(Source(), CacheType.NET_EASE_SONG, value => value.Id, resp.Songs);
            return new ResultVo<AlbumVo>(resp.Convert());
        }
        else
        {
            return ResultVo<AlbumVo>.Failure(ErrorMsgConst.ALBUM_NOT_EXIST);
        }
    }

    protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
    {
        // 从缓存中查询 Song，并将非命中的数据查询后加入缓存
        var cacheSongDict = GlobalCache
            .BatchQuery<Song>(Source(), CacheType.NET_EASE_SONG, songIds, out var notHitKeys)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var pair in _api.GetSongs(notHitKeys))
        {
            cacheSongDict.Add(pair.Key, pair.Value);
            // add cache
            GlobalCache.DoCache(Source(), CacheType.NET_EASE_SONG, pair.Key, pair.Value);
        }

        var result = new Dictionary<string, ResultVo<SongVo>>();

        foreach (var songId in songIds)
        {
            cacheSongDict.TryGetValue(songId, out var song);

            if (song != null)
            {
                result[songId] = new ResultVo<SongVo>(new SongVo
                {
                    Id = song.Id,
                    DisplayId = songId,
                    Pics = song.Al.PicUrl,
                    Name = song.Name,
                    Singer = song.Ar.Select(e => e.Name).ToArray(),
                    Album = song.Al.Name,
                    Duration = song.Dt,
                    PublishDate = song.PublishTime > 0
                        ? GlobalUtils.FormatDate(song.PublishTime).Split(' ')[0]
                        : string.Empty
                });
            }
            else
            {
                result[songId] = ResultVo<SongVo>.Failure(ErrorMsgConst.SONG_NOT_EXIST);
            }
        }

        return result;
    }

    protected override ResultVo<string> GetSongLink0(string songId)
    {
        var resp = _api.GetDatum([songId]);

        resp.TryGetValue(songId, out var datum);

        if (datum?.Url == null)
        {
            return ResultVo<string>.Failure(ErrorMsgConst.SONG_URL_GET_FAILED);
        }
        else
        {
            return new ResultVo<string>(datum.Url);
        }
    }

    protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
    {
        var resp = _api.GetLyric(displayId);

        if (resp.Code != 200)
        {
            return ResultVo<LyricVo>.Failure(ErrorMsgConst.LRC_NOT_EXIST);
        }
        
        var vo = new LyricVo
        {
            SearchSource = SearchSourceEnum.NET_EASE_MUSIC
        };
        
        if (isVerbatim)
        {
            if (resp.Yrc != null)
            {
                vo.Lyric = VerbatimLyricUtils.DealVerbatimLyric4NetEaseMusic(WebUtility.HtmlDecode(resp.Yrc.Lyric));
            }
            // not support translate && Transliteration in common mode
        }
        else
        {
            if (resp.Lrc != null)
            {
                vo.Lyric = WebUtility.HtmlDecode(resp.Lrc.Lyric);
            }
            if (resp.Tlyric != null)
            {
                vo.TranslateLyric = WebUtility.HtmlDecode(resp.Tlyric.Lyric);
            }
            if (resp.Romalrc != null)
            {
                vo.TransliterationLyric = WebUtility.HtmlDecode(resp.Romalrc.Lyric);
            }
        }

        return new ResultVo<LyricVo>(vo);
    }

    protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
    {
        var resp = _api.Search(keyword, searchType);

        if (resp.IsSuccess())
        {
            return new ResultVo<SearchResultVo>(resp.Data.Convert(searchType));
        }
            
        _logger.Error("NetEaseMusicApi Search0 failed, resp: {Resp}", resp.ToJson());

        return ResultVo<SearchResultVo>.Failure(resp.ErrorMsg);
    }
}
