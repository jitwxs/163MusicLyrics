using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Api
{
    public class QQMusicApiV2 : MusicApiV2Cacheable
    {
        private readonly QQMusicNativeApi _api;
        
        public QQMusicApiV2()
        {
            _api = new QQMusicNativeApi();
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
                GlobalCache.DoCache(CacheType.QQ_MUSIC_SONG, value => value.Id, resp.Cdlist[0].SongList);
                GlobalCache.DoCache(CacheType.QQ_MUSIC_SONG, value => value.Mid, resp.Cdlist[0].SongList);
                
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
                ResultVo<QQMusicBean.Song> SongCacheFunc(int e)
                {
                    var resp = _api.GetSong(songId);
                    return resp.IsIllegal() ? ResultVo<QQMusicBean.Song>.Failure(ErrorMsg.SONG_NOT_EXIST) : new ResultVo<QQMusicBean.Song>(resp.Data[0]);
                }
                
                var songRes = GlobalCache.Process(CacheType.QQ_MUSIC_SONG, songId, SongCacheFunc);
                if (!songRes.IsSuccess())
                {
                    result[songId] = ResultVo<SongVo>.Failure(songRes.ErrorMsg);
                    continue;
                }
                
                var linkRes = GlobalCache.Process(CacheType.QQ_MUSIC_SONG_LINK, songId, e => _api.GetSongLink(songId));
                if (!linkRes.IsSuccess())
                {
                    result[songId] = ResultVo<SongVo>.Failure(linkRes.ErrorMsg);
                    continue;
                }

                result[songId] = new ResultVo<SongVo>(new SongVo
                {
                    Id = songRes.Data.Id,
                    DisplayId = songRes.Data.Mid,
                    Links = linkRes.Data,
                    Pics = BuildPicUrl(songRes.Data.Album),
                    Name = songRes.Data.Name,
                    Singer = ContractSinger(songRes.Data.Singer),
                    Album = songRes.Data.Album.Name,
                    Duration = songRes.Data.Interval * 1000
                });
            }
            
            return result;
        }

        protected override ResultVo<LyricVo> GetLyricVo0(string id, string displayId, bool isVerbatim)
        {
            var resp = isVerbatim ? _api.GetVerbatimLyric(id) : _api.GetLyric(displayId);

            if (resp.Code != 0)
            {
                return ResultVo<LyricVo>.Failure(ErrorMsg.LRC_NOT_EXIST);
            }
            
            var lyricVo = new LyricVo();
            lyricVo.SetLyric(resp.Lyric);
            lyricVo.SetTranslateLyric(resp.Trans);
           
            return new ResultVo<LyricVo>(lyricVo);
        }

        protected override ResultVo<SearchResultVo> Search0(string keyword, SearchTypeEnum searchType)
        {
            // todo not support
            throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
        }

        /// <summary>
        /// 拼接歌手名
        /// </summary>
        private static string ContractSinger(QQMusicBean.Singer[] singers)
        {
            if (singers == null || !singers.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var one in singers)
            {
                sb.Append(one.Name).Append(',');
            }

            return sb.Remove(sb.Length - 1, 1).ToString();
        }

        private static string BuildPicUrl(QQMusicBean.Album album)
        {
            return $"https://y.qq.com/music/photo_new/T002R800x800M000{album.Pmid}.jpg";
        }
    }
}