using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicLyricApp.Bean;
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

        protected override IEnumerable<string> GetSongIdsFromAlbum0(string albumId)
        {
            var resp = _api.GetAlbum(albumId);

            if (resp.Code == 0)
            {
                return resp.Data.List.Select(albumSong => albumSong.Songmid);
            }

            throw new MusicLyricException(ErrorMsg.ALBUM_NOT_EXIST);
        }

        protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
        {
            var result = new Dictionary<string, ResultVo<SongVo>>();
            
            foreach (var songId in songIds)
            {
                var resp = _api.GetSong(songId);

                if (resp.Code != 0 || resp.Data.Length == 0)
                {
                    result[songId] = new ResultVo<SongVo>(null, ErrorMsg.SONG_NOT_EXIST);
                    continue;
                }

                var song = resp.Data[0];

                var links = _api.GetSongLink(songId);

                result[songId] = new ResultVo<SongVo>(new SongVo
                {
                    Id = song.Id,
                    DisplayId = song.Mid,
                    Links = links,
                    Pics = BuildPicUrl(song.Album),
                    Name = song.Name,
                    Singer = ContractSinger(song.Singer),
                    Album = song.Album.Name,
                    Duration = song.Interval * 1000
                });
            }
            
            return result;
        }

        protected override LyricVo GetLyricVo0(SongVo songVo, bool isVerbatim)
        {
            var resp = isVerbatim ? _api.GetVerbatimLyric(songVo.Id) : _api.GetLyric(songVo.DisplayId);

            if (resp.Code != 0)
            {
                throw new MusicLyricException(ErrorMsg.LRC_NOT_EXIST);
            }
            
            var lyricVo = new LyricVo();
            
            lyricVo.SetLyric(resp.Lyric);
            lyricVo.SetTranslateLyric(resp.Trans);
           
            return lyricVo;
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