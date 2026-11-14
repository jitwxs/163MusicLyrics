using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core;

/// <summary>
/// In-memory results of the music APIs. Thread-safe: the online lyrics page reads and writes it on the UI thread
/// while local search matches songs on background threads. A value two threads miss at once may be fetched twice;
/// the fetch runs outside any lock, so one slow request never holds up the others.
/// </summary>
public static class GlobalCache
{
    private static readonly ConcurrentDictionary<CacheType, ConcurrentDictionary<string, object>> Cache = new();

    public static ResultVo<T> Process<T>(object keyPrefix, CacheType cacheType, string key, Func<ResultVo<T>> cacheFunc)
    {
        var cache = Query<T>(keyPrefix, cacheType, key);
        if (cache != null)
        {
            return new ResultVo<T>(cache);
        }

        var res = cacheFunc.Invoke();
        if (res.IsSuccess() && res.Data is not null)
        {
            DoCache(keyPrefix, cacheType, key, res.Data);
        }

        return res;
    }

    public static Dictionary<string, TValue> BatchQuery<TValue>(object keyPrefix, CacheType cacheType,
        IEnumerable<string> keys, out string[] notHitKeys0)
    {
        var result = new Dictionary<string, TValue>();
        var notHitKeys = new List<string>();

        foreach (var key in keys)
        {
            var value = Query<TValue>(keyPrefix, cacheType, key);
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

    public static T? Query<T>(object keyPrefix, CacheType cacheType, string originKey)
    {
        var key = AddPrefix(keyPrefix, originKey);

        if (Cache.TryGetValue(cacheType, out var dictionary) && dictionary.TryGetValue(key, out var result))
        {
            return (T)result;
        }
        else
        {
            return default;
        }
    }

    public static void DoCache<TValue>(object keyPrefix, CacheType cacheType, Func<TValue, string> keyFunc,
        IEnumerable<TValue> values) where TValue : notnull
    {
        foreach (var value in values)
        {
            var key = keyFunc.Invoke(value);
            DoCache(keyPrefix, cacheType, key, value);
        }
    }

    public static void DoCache(object keyPrefix, CacheType cacheType, string originKey, object value)
    {
        Cache.GetOrAdd(cacheType, static _ => new ConcurrentDictionary<string, object>())
            [AddPrefix(keyPrefix, originKey)] = value;
    }

    private static string AddPrefix(object prefix, string key)
    {
        return $"{prefix}^{key}";
    }

    private static string DelPrefix(string key)
    {
        return key.Split('^')[1];
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
    /// 汽水音乐歌曲详情（含歌词）
    /// </summary>
    SODA_MUSIC_TRACK,

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