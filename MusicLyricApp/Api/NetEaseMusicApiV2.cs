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

        public NetEaseMusicApiV2()
        {
            _api = new NetEaseMusicNativeApi();
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
            // 优先从缓存中查询 Song，并将非命中的数据查询后加入缓存
            var songResp = GlobalCache.BatchQuery<string, Song>(CacheType.NET_EASE_SONG, songIds, out var notHitKeys0)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var pair in _api.GetSongs(notHitKeys0))
            {
                songResp.Add(pair.Key, pair.Value);
                GlobalCache.DoCache(CacheType.NET_EASE_SONG, pair.Key, pair.Value);
            }
            
            // 优先从缓存中查询 Datum，并将非命中的数据查询后加入缓存
            var datumResp = GlobalCache.BatchQuery<string, Datum>(CacheType.NET_EASE_DATUM, songIds, out var notHitKeys1)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var pair in _api.GetDatum(notHitKeys1))
            {
                datumResp.Add(pair.Key, pair.Value);
                GlobalCache.DoCache(CacheType.NET_EASE_DATUM, pair.Key, pair.Value);
            }

            var result = new Dictionary<string, ResultVo<SongVo>>();

            foreach (var pair in datumResp)
            {
                var songId = pair.Key;

                if (songResp.TryGetValue(songId, out var song))
                {
                    var datum = pair.Value;
                    
                    result[songId] = new ResultVo<SongVo>(new SongVo
                    {
                        Id = song.Id,
                        DisplayId = songId,
                        Links = datum.Url,
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
            
            if (resp == null || resp.Code != 200)
            {
                _logger.Error("NetEaseMusicApiV2 Search0 failed, resp: {Resp}", resp.ToJson());
                return ResultVo<SearchResultVo>.Failure(ErrorMsg.NETWORK_ERROR);
            }

            return new ResultVo<SearchResultVo>(resp.Result.Convert(searchType));
        }
    }
}