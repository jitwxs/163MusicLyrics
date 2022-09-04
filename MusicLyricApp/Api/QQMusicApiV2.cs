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

        protected override IEnumerable<string> GetSongIdsFromAlbum0(string albumId)
        {
            var resp = _api.GetAlbum(albumId);

            if (resp.Code == 0)
            {
                return resp.Data.List.Select(albumSong => albumSong.Songmid);
            }

            throw new MusicLyricException(ErrorMsg.ALBUM_NOT_EXIST);
        }

        protected override Dictionary<string, SongVo> GetSongVo0(string[] songIds, out Dictionary<string, string> errorMsgDict)
        {
            errorMsgDict = new Dictionary<string, string>();
            
            var result = new Dictionary<string, SongVo>();
            
            foreach (var songId in songIds)
            {
                var resp = _api.GetSong(songId);

                if (resp.Code != 0 || resp.Data.Length == 0)
                {
                    errorMsgDict[songId] = ErrorMsg.SONG_NOT_EXIST;
                    continue;
                }

                var song = resp.Data[0];

               var links = _api.GetSongLink(songId);

                result[songId] = new SongVo
                {
                    Links = links,
                    Pics = BuildPicUrl(song.Album),
                    Name = song.Name,
                    Singer = ContractSinger(song.Singer),
                    Album = song.Album.Name,
                    Duration = song.Interval * 1000
                };
                errorMsgDict[songId] = ErrorMsg.SUCCESS;
            }
            
            return result;
        }

        protected override LyricVo GetLyricVo0(string songId)
        {
            var resp = _api.GetLyric(songId);

            if (resp.Code != 0)
            {
                throw new MusicLyricException(ErrorMsg.LRC_NOT_EXIST);
            }
            
            var lyricVo = new LyricVo();
            
            lyricVo.SetLyric(resp.Lyric);
            lyricVo.SetTranslateLyric(resp.Trans);
           
            return lyricVo;
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
            return "https://y.qq.com/music/photo_new/T002R300x300M000" + album.Pmid + ".jpg";
        }
    }
}