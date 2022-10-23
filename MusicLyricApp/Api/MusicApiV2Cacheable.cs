using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;

namespace MusicLyricApp.Api
{
    public abstract class MusicApiV2Cacheable : IMusicApiV2
    {
        protected abstract IEnumerable<string> GetSongIdsFromAlbum0(string albumId);

        protected abstract Dictionary<string, ResultVo<SongVo>> GetSongVo0(string[] songIds);

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

        public Dictionary<string, ResultVo<SongVo>> GetSongVo(string[] songIds)
        {
            var result = new Dictionary<string, ResultVo<SongVo>>();
            var requestIds = new List<string>();
            
            foreach (var songId in songIds)
            {
                if (GlobalCache.ContainsSong(songId))
                {
                    result[songId] = new ResultVo<SongVo>(GlobalCache.GetSong(songId));
                }
                else
                {
                    requestIds.Add(songId);
                }
            }
            
            foreach(var pair in GetSongVo0(requestIds.ToArray()))
            {
                var songId = pair.Key;
                var resultVo = pair.Value;
                
                if (resultVo.IsSuccess())
                {
                    GlobalCache.PutSong(songId, resultVo.Data);
                }
                
                result[songId] = pair.Value;
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