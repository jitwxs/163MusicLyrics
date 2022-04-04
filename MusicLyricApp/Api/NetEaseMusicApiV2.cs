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

        protected override Dictionary<string, SongVo> GetSongVo0(string[] songIds, out Dictionary<string, string> errorMsgDict)
        {
            errorMsgDict = new Dictionary<string, string>();

            var datumResp = _api.GetDatum(songIds);
            var songResp = _api.GetSongs(songIds);
            var result = new Dictionary<string, SongVo>();

            foreach (var pair in datumResp)
            {
                var songId = pair.Key;
                var datum = pair.Value;

                if (!songResp.TryGetValue(songId, out var song))
                {
                    errorMsgDict[songId] = ErrorMsg.SONG_NOT_EXIST;
                    continue;
                }

                result[songId] = new SongVo
                {
                    Links = datum.Url,
                    Name = song.Name,
                    Singer = ContractSinger(song.Ar),
                    Album = song.Al.Name,
                    Duration = song.Dt
                };
                errorMsgDict[songId] = ErrorMsg.SUCCESS;
            }

            return result;
        }

        protected override LyricVo GetLyricVo0(string songId)
        {
            var resp = _api.GetLyric(songId);

            if (resp.Code == 200)
            {
                return new LyricVo
                {
                    Lyric = resp.Lrc.Lyric ?? string.Empty,
                    TranslateLyric = resp.Tlyric.Lyric ?? string.Empty
                };
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