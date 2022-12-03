using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Cache
{
    public static class GlobalCache
    {
        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, IEnumerable<string>> AlbumSongIdsCache =
            new Dictionary<string, IEnumerable<string>>();

        public static bool ContainsAlbumSongIds(string albumId)
        {
            return AlbumSongIdsCache.ContainsKey(albumId);
        }

        public static IEnumerable<string> GetSongIdsFromAlbum(string albumId)
        {
            AlbumSongIdsCache.TryGetValue(albumId, out var result);
            return result;
        }

        public static void PutAlbumSongIds(string albumId, IEnumerable<string> songIds)
        {
            AlbumSongIdsCache.Add(albumId, songIds);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, SongVo> SongCache = new Dictionary<string, SongVo>();

        public static bool ContainsSong(string songId)
        {
            return SongCache.ContainsKey(songId);
        }

        public static SongVo GetSong(string songId)
        {
            SongCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutSong(string songId, SongVo song)
        {
            SongCache.Add(songId, song);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, SaveVo> SaveVoCache = new Dictionary<string, SaveVo>();

        public static bool ContainsSaveVo(string songId, bool isVerbatimLyric)
        {
            var cacheKey = GlobalUtils.GetSongKey(songId, isVerbatimLyric);
            
            return SaveVoCache.ContainsKey(cacheKey);
        }

        public static SaveVo GetSaveVo(string songId, bool isVerbatimLyric)
        {
            var cacheKey = GlobalUtils.GetSongKey(songId, isVerbatimLyric);
            
            SaveVoCache.TryGetValue(cacheKey, out var result);
            return result;
        }

        public static void PutSaveVo(string songId, bool isVerbatimLyric, SaveVo saveVo)
        {
            var cacheKey = GlobalUtils.GetSongKey(songId, isVerbatimLyric);
            
            SaveVoCache.Add(cacheKey, saveVo);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, LyricVo> LyricCache = new Dictionary<string, LyricVo>();

        public static bool ContainsLyric(string songId)
        {
            return LyricCache.ContainsKey(songId);
        }

        public static LyricVo GetLyric(string songId)
        {
            LyricCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutLyric(string songId, LyricVo albumResult)
        {
            LyricCache.Add(songId, albumResult);
        }
        
        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, SearchResultVo> searchResultVoCache = new Dictionary<string, SearchResultVo>();

        public static bool ContainsSearchResultVo(string keyword)
        {
            return searchResultVoCache.ContainsKey(keyword);
        }

        public static SearchResultVo GetSearchResultVo(string keyword)
        {
            searchResultVoCache.TryGetValue(keyword, out var result);
            return result;
        }

        public static void PutSearchResultVo(string keyword, SearchResultVo vo)
        {
            searchResultVoCache.Add(keyword, vo);
        }
    }
}