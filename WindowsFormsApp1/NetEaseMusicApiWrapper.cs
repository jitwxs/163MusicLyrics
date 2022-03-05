using System.Collections.Generic;

namespace 网易云歌词提取
{
    public class NetEaseMusicApiWrapper : INeteaseMusicApi
    {
        private readonly NetEaseMusicApi _netEaseMusicApi;

        public NetEaseMusicApiWrapper()
        {
            _netEaseMusicApi = new NetEaseMusicApi();
        }

        public Dictionary<long, Datum> GetDatum(long[] songIds, long bitrate = 999000)
        {
            var result = new Dictionary<long, Datum>();

            var needRequestIds = new List<long>();

            foreach (var songId in songIds)
            {
                if (NetEaseMusicCache.ContainsDatum(songId))
                {
                    result.Add(songId, NetEaseMusicCache.GetDatum(songId));
                }
                else
                {
                    needRequestIds.Add(songId);
                }
            }

            if (needRequestIds.Count > 0)
            {
                var requestResult = _netEaseMusicApi.GetDatum(needRequestIds.ToArray(), bitrate);
                foreach (KeyValuePair<long, Datum> kvp in requestResult)
                {
                    NetEaseMusicCache.PutDatum(kvp.Key, kvp.Value);
                    result.Add(kvp.Key, kvp.Value);
                }
            }

            return result;
        }

        public Dictionary<long, Song> GetSongs(long[] songIds)
        {
            var result = new Dictionary<long, Song>();
            var requestIds = new List<long>();
            
            foreach (var songId in songIds)
            {
                if (NetEaseMusicCache.ContainsSong(songId))
                {
                    result[songId] = NetEaseMusicCache.GetSong(songId);
                }
                else
                {
                    requestIds.Add(songId);
                }
            }
            
            foreach(var pair in _netEaseMusicApi.GetSongs(requestIds.ToArray()))
            {
                result[pair.Key] = pair.Value;
                NetEaseMusicCache.PutSong(pair.Key, pair.Value);
            }

            return result;
        }

        public AlbumResult GetAlbum(long albumId)
        {
            if (NetEaseMusicCache.ContainsAlbum(albumId))
            {
                return NetEaseMusicCache.GetAlbum(albumId);
            }

            var result = _netEaseMusicApi.GetAlbum(albumId);
            if (result != null)
            {
                NetEaseMusicCache.PutAlbum(albumId, result);
            }

            return result;
        }

        public LyricResult GetLyric(long songId)
        {
            if (NetEaseMusicCache.ContainsLyric(songId))
            {
                return NetEaseMusicCache.GetLyric(songId);
            }

            var result = _netEaseMusicApi.GetLyric(songId);
            if (result != null)
            {
                NetEaseMusicCache.PutLyric(songId, result);
            }

            return result;
        }
    }
}