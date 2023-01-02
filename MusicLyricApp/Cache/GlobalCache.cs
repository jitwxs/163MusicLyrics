using System;
using System.Collections.Generic;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Cache
{
    public static class GlobalCache
    {
        private static readonly Dictionary<CacheType, Dictionary<object, object>> Cache =
            new Dictionary<CacheType, Dictionary<object, object>>();

        public static ResultVo<T> Process<T>(CacheType cacheType, object key, Func<ResultVo<T>> cacheFunc)
        {
            var cache = Query<T>(cacheType, key);
            if (cache != null)
            {
                return new ResultVo<T>(cache);
            }

            var res = cacheFunc.Invoke();
            if (res.IsSuccess())
            {
                DoCache(cacheType, key, res.Data);
            }

            return res;
        }

        public static Dictionary<TKey, TValue> BatchQuery<TKey, TValue>(CacheType cacheType, IEnumerable<TKey> keys,
            out TKey[] notHitKeys0)
        {
            var result = new Dictionary<TKey, TValue>();
            var notHitKeys = new List<TKey>();

            foreach (var key in keys)
            {
                var value = Query<TValue>(cacheType, key);
                if (value == null)
                {
                    notHitKeys.Add(key);
                }
                else
                {
                    result[key] = value;
                }
            }

            notHitKeys0 = notHitKeys.ToArray();

            return result;
        }

        public static T Query<T>(CacheType cacheType, object key)
        {
            if (Cache.ContainsKey(cacheType) && Cache[cacheType].ContainsKey(key))
            {
                return (T)Cache[cacheType][key];
            }
            else
            {
                return default;
            }
        }

        public static void DoCache<TValue>(CacheType cacheType, Func<TValue, object> keyFunc,
            IEnumerable<TValue> values)
        {
            foreach (var value in values)
            {
                var key = keyFunc.Invoke(value);
                DoCache(cacheType, key, value);
            }
        }

        public static void DoCache(CacheType cacheType, object key, object value)
        {
            if (!Cache.ContainsKey(cacheType))
            {
                Cache.Add(cacheType, new Dictionary<object, object>());
            }

            // add the key if it's non-existent.
            Cache[cacheType][key] = value;
        }
    }

    public enum CacheType
    {
        /// <summary>
        /// 网易云音乐歌曲
        /// </summary>
        NET_EASE_SONG,

        /// <summary>
        /// 网易云音乐 DATUM
        /// </summary>
        NET_EASE_DATUM,

        /// <summary>
        /// QQ 音乐歌曲
        /// </summary>
        QQ_MUSIC_SONG,

        /// <summary>
        /// 歌曲
        /// </summary>
        SONG_VO,
        
        /// <summary>
        /// 歌曲链接
        /// </summary>
        SONG_LINK,

        /// <summary>
        /// 歌词
        /// </summary>
        LYRIC_VO,

        /**
         * 专辑
         */
        ALBUM_VO,

        /**
         * 歌单
         */
        PLAYLIST_VO,

        /// <summary>
        /// 查询结果
        /// </summary>
        SEARCH_RESULT_VO,
        
        /// <summary>
        /// 翻译
        /// </summary>
        TRANSLATE,
    }
}