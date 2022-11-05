using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
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

        protected override IEnumerable<string> GetSongIdsFromAlbum0(string albumId)
        {
            var resp = _api.GetAlbum(albumId);

            if (resp.Code == 200)
            {
                return resp.Songs.Select(song => song.Id);
            }

            _logger.Error("NetEaseMusicApiV2 GetSongIdsFromAlbum failed, resp: {Resp}", resp.ToJson());

            throw new MusicLyricException(ErrorMsg.ALBUM_NOT_EXIST);
        }

        protected override Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds)
        {
            var datumResp = _api.GetDatum(songIds);
            var songResp = _api.GetSongs(songIds);
            var result = new Dictionary<string, ResultVo<SongVo>>();

            foreach (var pair in datumResp)
            {
                var songId = pair.Key;

                if (songResp.TryGetValue(songId, out var song))
                {
                    var datum = pair.Value;
                    
                    result[songId] = new ResultVo<SongVo>(new SongVo
                    {
                        Id = long.Parse(song.Id),
                        DisplayId = songId,
                        Links = datum.Url,
                        Pics = song.Al.PicUrl,
                        Name = song.Name,
                        Singer = ContractSinger(song.Ar),
                        Album = song.Al.Name,
                        Duration = song.Dt
                    });
                }
                else
                {
                    result[songId] = new ResultVo<SongVo>(null, ErrorMsg.SONG_NOT_EXIST);
                }
            }

            return result;
        }

        protected override LyricVo GetLyricVo0(SongVo songVo, bool isVerbatim)
        {
            // todo isVerbatim just not support
            var resp = _api.GetLyric(songVo.DisplayId);

            if (resp.Code == 200)
            {
                var lyricVo = new LyricVo();
                if (resp.Lrc != null)
                {
                    lyricVo.SetLyric(resp.Lrc.Lyric);
                }
                if (resp.Tlyric != null)
                {
                    lyricVo.SetTranslateLyric(resp.Tlyric.Lyric);
                }

                return lyricVo;
            }

            _logger.Error("NetEaseMusicApiV2 GetLyricVo failed, resp: {Resp}", resp.ToJson());

            throw new MusicLyricException(ErrorMsg.LRC_NOT_EXIST);
        }
        
        /// <summary>
        /// 拼接歌手名
        /// </summary>
        public static string ContractSinger(List<Ar> arList)
        {
            if (arList == null || !arList.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var ar in arList)
            {
                sb.Append(ar.Name).Append(',');
            }

            return sb.Remove(sb.Length - 1, 1).ToString();
        }
    }
}