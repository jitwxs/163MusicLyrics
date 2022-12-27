using System;
using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Utils;
using NLog;

namespace MusicLyricApp.Api
{
    public class NetEaseMusicApiV2 : MusicApiV2Cacheable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly NetEaseMusicNativeApi _api;

        public NetEaseMusicApiV2(Func<string> cookieFunc)
        {
            _api = new NetEaseMusicNativeApi(cookieFunc);
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
                // cache song
                GlobalCache.DoCache(CacheType.NET_EASE_SONG, value => value.Id, resp.Playlist.Tracks);

                return new ResultVo<PlaylistVo>(resp.Convert());
            } 
            else if (resp.Code == 20001)
            {
                return ResultVo<PlaylistVo>.Failure(ErrorMsg.NEED_LOGIN);
            }
            else
            {
                return ResultVo<PlaylistVo>.Failure(ErrorMsg.PLAYLIST_NOT_EXIST);
            }
        }

        protected override ResultVo<AlbumVo> GetAlbumVo0(string albumId)
        {
            var resp = _api.GetAlbum(albumId);
            if (resp.Code == 200)
            {
                // cache song
                GlobalCache.DoCache(CacheType.NET_EASE_SONG, value => value.Id, resp.Songs);
                return new ResultVo<AlbumVo>(resp.Convert());
            }
            else
            {
                return ResultVo<AlbumVo>.Failure(ErrorMsg.ALBUM_NOT_EXIST);
            }
        }

        protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
        {
            // 从缓存中查询 Song，并将非命中的数据查询后加入缓存
            var cacheSongDict = GlobalCache
                .BatchQuery<string, Song>(CacheType.NET_EASE_SONG, songIds, out var notHitKeys)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var pair in _api.GetSongs(notHitKeys))
            {
                cacheSongDict.Add(pair.Key, pair.Value);
                // add cache
                GlobalCache.DoCache(CacheType.NET_EASE_SONG, pair.Key, pair.Value);
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
                        Singer = string.Join(",", song.Ar.Select(e => e.Name)),
                        Album = song.Al.Name,
                        Duration = song.Dt
                    });
                }
                else
                {
                    result[songId] = ResultVo<SongVo>.Failure(ErrorMsg.SONG_NOT_EXIST);
                }
            }

            return result;
        }

        protected override ResultVo<string> GetSongLink0(string songId)
        {
            var resp = _api.GetDatum(new[] { songId });

            resp.TryGetValue(songId, out var datum);

            if (datum?.Url == null)
            {
                return ResultVo<string>.Failure(ErrorMsg.SONG_URL_GET_SUCCESS);
            }
            else
            {
                return new ResultVo<string>(datum.Url);
            }
        }

        protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
        {
            // todo isVerbatim just not support
            var resp = _api.GetLyric(displayId);

            if (resp.Code != 200)
            {
                return ResultVo<LyricVo>.Failure(ErrorMsg.LRC_NOT_EXIST);
            }

            var lyricVo = new LyricVo();
            if (resp.Lrc != null)
            {
                lyricVo.SetLyric(resp.Lrc.Lyric);
            }

            if (resp.Tlyric != null)
            {
                lyricVo.SetTranslateLyric(resp.Tlyric.Lyric);
            }

            return new ResultVo<LyricVo>(lyricVo);
        }

        protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
        {
            var resp = _api.Search(keyword, searchType);

            if (resp == null)
            {
                _logger.Error("NetEaseMusicApiV2 Search0 failed, resp: {Resp}", resp.ToJson());
                return ResultVo<SearchResultVo>.Failure(ErrorMsg.NETWORK_ERROR);
            }

            return new ResultVo<SearchResultVo>(resp.Convert(searchType));
        }
    }
}