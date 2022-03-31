using System;
using System.Collections.Generic;
using NLog;

namespace 网易云歌词提取
{
    public static class NetEaseMusicCache
    {
        private static readonly Dictionary<long, SaveVo> SaveVoCache = new Dictionary<long, SaveVo>();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool ContainsSaveVo(long songId)
        {
            return SaveVoCache.ContainsKey(songId);
        }

        public static SaveVo GetSaveVo(long songId)
        {
            SaveVoCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutSaveVo(long songId, SaveVo saveVo)
        {
            SaveVoCache.Add(songId, saveVo);
        }
        
        /* --------------------------------------------------------------------------------------------------------- */
        
        private static readonly Dictionary<long, Song> SongCache = new Dictionary<long, Song>();

        public static bool ContainsSong(long songId)
        {
            return SongCache.ContainsKey(songId);
        }

        public static Song GetSong(long songId)
        {
            SongCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutSong(long songId, Song song)
        {
            SongCache.Add(songId, song);
        }
        
        /* --------------------------------------------------------------------------------------------------------- */
        
        private static readonly Dictionary<long, Datum> DatumCache = new Dictionary<long, Datum>();

        public static bool ContainsDatum(long songId)
        {
            return DatumCache.ContainsKey(songId);
        }

        public static Datum GetDatum(long songId)
        {
            DatumCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutDatum(long songId, Datum datum)
        {
            DatumCache.Add(songId, datum);
        }

        /* --------------------------------------------------------------------------------------------------------- */
        
        private static readonly Dictionary<long, AlbumResult> AlbumCache = new Dictionary<long, AlbumResult>();

        public static bool ContainsAlbum(long albumId)
        {
            return AlbumCache.ContainsKey(albumId);
        }

        public static AlbumResult GetAlbum(long albumId)
        {
            AlbumCache.TryGetValue(albumId, out var result);
            return result;
        }

        public static void PutAlbum(long albumId, AlbumResult albumResult)
        {
            if (albumResult == null)
            {
                var ex = new ArgumentNullException(nameof(albumResult));
                _logger.Error(ex);
                throw ex;
            }
            AlbumCache.Add(albumId, albumResult);
        }
        
        /* --------------------------------------------------------------------------------------------------------- */
        
        private static readonly Dictionary<long, LyricResult> LyricCache = new Dictionary<long, LyricResult>();

        public static bool ContainsLyric(long songId)
        {
            return LyricCache.ContainsKey(songId);
        }

        public static LyricResult GetLyric(long songId)
        {
            LyricCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutLyric(long songId, LyricResult albumResult)
        {
            if (albumResult == null)
            {
                var ex = new ArgumentNullException(nameof(albumResult));
                _logger.Error(ex);
                throw ex;
            }
            LyricCache.Add(songId, albumResult);
        }
    }
}