using System;
using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Api.Music
{
    public class QQMusicApi : MusicCacheableApi
    {
        private readonly QQMusicNativeApi _api;
        
        public QQMusicApi(Func<string> cookieAction)
        {
            _api = new QQMusicNativeApi(cookieAction);
        }

        protected override SearchSourceEnum Source0()
        {
            return SearchSourceEnum.QQ_MUSIC;
        }

        protected override ResultVo<PlaylistVo> GetPlaylistVo0(string playlistId)
        {
            var resp = _api.GetPlaylist(playlistId);
            if (resp.Code == 0)
            {
                // cache song
                GlobalCache.DoCache(Source(), CacheType.QQ_MUSIC_SONG, value => value.Id, resp.Cdlist[0].SongList);
                GlobalCache.DoCache(Source(), CacheType.QQ_MUSIC_SONG, value => value.Mid, resp.Cdlist[0].SongList);
                
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
            if (resp.Code == 0)
            {
                return new ResultVo<AlbumVo>(resp.Convert());
            }
            else
            {
                return ResultVo<AlbumVo>.Failure(ErrorMsg.ALBUM_NOT_EXIST);
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
                    return resp.IsIllegal() ? ResultVo<QQMusicBean.Song>.Failure(ErrorMsg.SONG_NOT_EXIST) : new ResultVo<QQMusicBean.Song>(resp.Data[0]);
                }
                
                var songRes = GlobalCache.Process(Source(), CacheType.QQ_MUSIC_SONG, songId, SongCacheFunc);
                if (songRes.IsSuccess())
                {
                    result[songId] = new ResultVo<SongVo>(new SongVo
                    {
                        Id = songRes.Data.Id,
                        DisplayId = songRes.Data.Mid,
                        Pics = $"https://y.qq.com/music/photo_new/T002R800x800M000{songRes.Data.Album.Pmid}.jpg",
                        // #212 QQ music need use title attribute as song name
                        Name = GlobalUtils.GetOrDefault(songRes.Data.Title, songRes.Data.Name),
                        Singer = songRes.Data.Singer.Select(e => e.Name).ToArray(),
                        Album = songRes.Data.Album.Name,
                        Duration = songRes.Data.Interval * 1000
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
            // #214 QQ music verbatim API only use for verbatim lyrics, it's translate lyrics maybe not intact,
            // so for search by verbatim, we also need query translate lyrics by common API
            QQMusicBean.LyricResult resp;
            if (isVerbatim)
            {
                resp = _api.GetVerbatimLyric(id);
                if (resp.Code == 0)
                {
                    var resp2 = _api.GetLyric(displayId);
                    if (resp2.Code == 0)
                    {
                        resp.Trans = resp2.Trans;
                    }
                }
            }
            else
            {
                resp = _api.GetLyric(displayId);
            }
            
            return resp.Code == 0 ? new ResultVo<LyricVo>(resp.ToVo()) : ResultVo<LyricVo>.Failure(ErrorMsg.LRC_NOT_EXIST);
        }

        protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
        {
            var resp = _api.Search(keyword, searchType);

            if (resp.Code == 0 && resp.Req_1.Code == 0 && resp.Req_1.Data.Code == 0)
            {
                return new ResultVo<SearchResultVo>(resp.Req_1.Data.Body.Convert(searchType));
            }
            
            return ResultVo<SearchResultVo>.Failure(ErrorMsg.NETWORK_ERROR);
        }
    }
}