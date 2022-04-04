using System.Collections.Generic;
using System.Linq;
using Application.Bean;
using Application.Exception;
using Application.Utils;
using NLog;

namespace Application.Api
{
    public class NetEaseMusicApiV2 : IMusicApiV2
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly INetEaseMusicApi _api;

        public NetEaseMusicApiV2()
        {
            _api = new NetEaseMusicApiWrapper();
        }

        public IEnumerable<long> GetSongIdsFromAlbum(long albumId)
        {
            var resp = _api.GetAlbum(albumId);

            if (resp.Code == 200)
            {
                return resp.Songs.Select(song => song.Id);
            }

            _logger.Error("NetEaseMusicApiV2 GetSongIdsFromAlbum failed, resp: {Resp}", resp.ToJson());

            throw new MusicLyricException(ErrorMsg.INPUT_ALBUM_ILLEGAL);
        }

        public Dictionary<long, SongVo> GetSongVo(long[] songIds, out Dictionary<long, string> errorMsgDict)
        {
            errorMsgDict = new Dictionary<long, string>();

            var datumResp = _api.GetDatum(songIds);
            var songResp = _api.GetSongs(songIds);
            var result = new Dictionary<long, SongVo>();

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
                    Singer = NetEaseMusicUtils.ContractSinger(song.Ar),
                    Album = song.Al.Name,
                    Duration = song.Dt
                };
                errorMsgDict[songId] = ErrorMsg.SUCCESS;
            }

            return result;
        }

        public LyricVo GetLyricVo(long songId)
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
    }
}