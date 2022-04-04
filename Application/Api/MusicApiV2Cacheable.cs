using System.Collections.Generic;
using Application.Bean;
using Application.Cache;

namespace Application.Api
{
    public abstract class MusicApiV2Cacheable : IMusicApiV2
    {
        protected abstract IEnumerable<string> GetSongIdsFromAlbum0(string albumId);

        protected abstract Dictionary<string, SongVo> GetSongVo0(string[] songIds, out Dictionary<string, string> errorMsgDict);

        protected abstract LyricVo GetLyricVo0(string songId);
        
        public IEnumerable<string> GetSongIdsFromAlbum(string albumId)
        {
            if (GlobalCache.ContainsAlbumSongIds(albumId))
            {
                return GlobalCache.GetSongIdsFromAlbum(albumId);
            }

            var result = GetSongIdsFromAlbum0(albumId);
            if (result != null)
            {
                GlobalCache.PutAlbumSongIds(albumId, result);
            }

            return result;
        }

        public Dictionary<string, SongVo> GetSongVo(string[] songIds, out Dictionary<string, string> errorMsgDict)
        {
            var result = new Dictionary<string, SongVo>();
            var requestIds = new List<string>();
            
            foreach (var songId in songIds)
            {
                if (GlobalCache.ContainsSong(songId))
                {
                    result[songId] = GlobalCache.GetSong(songId);
                }
                else
                {
                    requestIds.Add(songId);
                }
            }
            
            foreach(var pair in GetSongVo0(requestIds.ToArray(), out errorMsgDict))
            {
                var songId = pair.Key;
                
                if (errorMsgDict[songId] != ErrorMsg.SUCCESS)
                {
                    continue;
                }
                
                result[songId] = pair.Value;
                GlobalCache.PutSong(songId, pair.Value);
            }

            return result;
        }

        public LyricVo GetLyricVo(string songId)
        {
            if (GlobalCache.ContainsLyric(songId))
            {
                return GlobalCache.GetLyric(songId);
            }

            var result = GetLyricVo0(songId);
            if (result != null)
            {
                GlobalCache.PutLyric(songId, result);
            }

            return result;
        }
    }
}