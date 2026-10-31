using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Utils;

namespace MusicLyricApp.Models;

// 双语歌词类型
public enum ShowLrcTypeEnum
{
    STAGGER = 0,
    ISOLATED = 1,
    MERGE = 2,
}

// 搜索来源
public enum SearchSourceEnum
{
    NET_EASE_MUSIC = 0,
    QQ_MUSIC = 1
}

// 搜索类型
public enum SearchTypeEnum
{
    SONG_ID = 0,
    ALBUM_ID = 1,
    PLAYLIST_ID = 2,
}

public enum VerbatimLyricModeEnum
{
    DISABLE = 0,
    STANDARD_MODE = 1,
    A2_MODE = 2,
}

// 强制两位类型
public enum DotTypeEnum
{
    DOWN = 0,
    HALF_UP = 1
}

// 输出文件格式
public enum OutputEncodingEnum
{
    UTF_8 = 0,
    UTF_8_BOM = 1,
    UTF_32 = 2,
    UNICODE = 3
}

public enum OutputFormatEnum
{
    LRC = 0,

    SRT = 1
}

// 译文缺省规则
public enum TransLyricLostRuleEnum
{
    IGNORE = 0,
    EMPTY_LINE = 1,
    FILL_ORIGIN = 2,
}

// 中文处理策略
public enum ChineseProcessRuleEnum
{
    IGNORE = 0,
    SIMPLIFIED_CHINESE = 1,
    TRADITIONAL_CHINESE = 2,
}

public enum LyricsTypeEnum
{
    ORIGIN = 0,
    ORIGIN_TRANS = 1,
    CHINESE = 2,
    ENGLISH = 3,
    TRANSLITERATION = 4,
    PINYIN = 5,
}

public enum ThemeModeEnum
{
    FOLLOW_SYSTEM = 0,
    LIGHT = 1,
    DARK = 2,
}

public enum LanguageEnum
{
    /// <summary>
    /// 其他
    /// </summary>
    OTHER = 0,

    /// <summary>
    /// 英语
    /// </summary>
    ENGLISH = 1,

    /// <summary>
    /// 日语
    /// </summary>
    JAPANESE = 2,

    /// <summary>
    /// 韩语
    /// </summary>
    KOREAN = 3,

    /// <summary>
    /// 俄语
    /// </summary>
    RUSSIAN = 4,

    /// <summary>
    /// 法语
    /// </summary>
    FRENCH = 5,

    /// <summary>
    /// 意大利语
    /// </summary>
    ITALIAN = 6,

    /// <summary>
    /// 汉语
    /// </summary>
    CHINESE = 7,
}

/**
 * 错误码
 */
public static class ErrorMsgConst
{
    private static string T(string key) => MusicLyricApp.Core.Localization.LocalizationService.Get(key);
    public static string SUCCESS => T("Error.SUCCESS");
    public static string SEARCH_RESULT_EMPTY => T("Error.SEARCH_RESULT_EMPTY");
    public static string MUST_SEARCH_BEFORE_SAVE => T("Error.MUST_SEARCH_BEFORE_SAVE");
    public static string MUST_SEARCH_BEFORE_GET_SONG_URL => T("Error.MUST_SEARCH_BEFORE_GET_SONG_URL");
    public static string MUST_SEARCH_BEFORE_GET_SONG_PIC => T("Error.MUST_SEARCH_BEFORE_GET_SONG_PIC");
    public static string INPUT_CONENT_EMPLT => T("Error.INPUT_CONENT_EMPLT");
    public static string INPUT_ID_ILLEGAL => T("Error.INPUT_ID_ILLEGAL");
    public static string PLAYLIST_NOT_EXIST => T("Error.PLAYLIST_NOT_EXIST");
    public static string ALBUM_NOT_EXIST => T("Error.ALBUM_NOT_EXIST");
    public static string SONG_NOT_EXIST => T("Error.SONG_NOT_EXIST");
    public static string LRC_NOT_EXIST => T("Error.LRC_NOT_EXIST");
    public static string FUNCTION_NOT_SUPPORT => T("Error.FUNCTION_NOT_SUPPORT");
    public static string SONG_URL_GET_SUCCESS => T("Error.SONG_URL_GET_SUCCESS");
    public static string SONG_URL_GET_FAILED => T("Error.SONG_URL_GET_FAILED");
    public static string SONG_PIC_GET_SUCCESS => T("Error.SONG_PIC_GET_SUCCESS");
    public static string SONG_PIC_GET_FAILED => T("Error.SONG_PIC_GET_FAILED");
    public static string DEPENDENCY_LOSS => T("Error.DEPENDENCY_LOSS");
    public static string SAVE_COMPLETE => T("Error.SAVE_COMPLETE");
    public static string NEED_LOGIN => T("Error.NEED_LOGIN");
    public static string PURE_MUSIC_IGNORE_SAVE => T("Error.PURE_MUSIC_IGNORE_SAVE");
    public static string TRANSLATE_LANGUAGE_NOT_SUPPORT => T("Error.TRANSLATE_LANGUAGE_NOT_SUPPORT");
    public static string NOT_EXIST_TRANSLATE_API => T("Error.NOT_EXIST_TRANSLATE_API");
    public static string CAIYUN_TRANSLATE_AUTH_FAILED => T("Error.CAIYUN_TRANSLATE_AUTH_FAILED");
    public static string BAIDU_TRANSLATE_AUTH_FAILED => T("Error.BAIDU_TRANSLATE_AUTH_FAILED");
    public static string GET_LATEST_VERSION_FAILED => T("Error.GET_LATEST_VERSION_FAILED");
    public static string THIS_IS_LATEST_VERSION => T("Error.THIS_IS_LATEST_VERSION");
    public static string SYSTEM_ERROR => T("Error.SYSTEM_ERROR");
    public static string NETWORK_ERROR => T("Error.NETWORK_ERROR");
    public static string API_RATE_LIMIT => T("Error.API_RATE_LIMIT");
    public static string STORAGE_FOLDER_ERROR => T("Error.STORAGE_FOLDER_ERROR");
}

/// <summary>
/// 封装单首歌曲的持久化信息
/// </summary>
public class SaveVo(int index, SongVo songVo, LyricVo lyricVo)
{
    public int Index { get; } = index;

    public SongVo SongVo { get; } = songVo;

    public LyricVo LyricVo { get; } = lyricVo;
}

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResultVo
{
    public SearchTypeEnum SearchType { get; set; }

    public SearchSourceEnum SearchSource { get; set; }

    public readonly List<SongSearchResultVo> SongVos = [];

    public readonly List<AlbumSearchResultVo> AlbumVos = [];

    public readonly List<PlaylistResultVo> PlaylistVos = [];

    public bool IsEmpty()
    {
        return SongVos.Count == 0 && AlbumVos.Count == 0 && PlaylistVos.Count == 0;
    }

    public class SongSearchResultVo
    {
        public string DisplayId { get; set; }

        public string Title { get; set; }

        public string[] AuthorName { get; set; }

        public string AlbumName { get; set; }

        /// <summary>
        /// 歌曲时长 ms
        /// </summary>
        public long Duration { get; set; }
    }

    public class AlbumSearchResultVo
    {
        public string DisplayId { get; set; }

        public string AlbumName { get; set; }

        public string[] AuthorName { get; set; }

        /// <summary>
        /// 歌曲数量
        /// </summary>
        public long SongCount { get; set; }

        /// <summary>
        /// 发行时间
        /// </summary>
        public string PublishTime { get; set; }
    }

    public class PlaylistResultVo
    {
        public string DisplayId { get; set; }

        public string PlaylistName { get; set; }

        public string AuthorName { get; set; }

        /// <summary>
        /// 歌单描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 播放数量
        /// </summary>
        public long PlayCount { get; set; }

        /// <summary>
        /// 歌曲数量
        /// </summary>
        public long SongCount { get; set; }
    }
}

/// <summary>
/// 歌单信息
/// </summary>
public class PlaylistVo
{
    /// <summary>
    /// 歌单名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 作者名
    /// </summary>
    public string AuthorName { get; set; }

    /// <summary>
    /// 歌单描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 歌曲信息
    /// </summary>
    public SimpleSongVo[] SimpleSongVos { get; set; }
}

/// <summary>
/// 专辑信息
/// </summary>
public class AlbumVo
{
    /// <summary>
    /// 专辑名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 发行公司
    /// </summary>
    public string Company { get; set; }

    /// <summary>
    /// 专辑描述
    /// </summary>
    public string Desc { get; set; }

    /// <summary>
    /// 歌曲信息
    /// </summary>
    public SimpleSongVo[] SimpleSongVos { get; set; }

    /// <summary>
    /// 发布时间，eg: 2005-07-08
    /// </summary>
    public string TimePublic { get; set; }
}

/// <summary>
/// 歌曲简略信息
/// </summary>
public class SimpleSongVo
{
    /// <summary>
    /// 内部 ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 前端展示的 ID
    /// </summary>
    public string DisplayId { get; set; }

    /// <summary>
    /// 歌曲名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 歌手名
    /// </summary>
    public string[] Singer { get; set; }
}

/// <summary>
/// 歌曲信息
/// </summary>
public class SongVo : SimpleSongVo
{
    /// <summary>
    /// 所属专辑名
    /// </summary>
    public string Album { get; set; }

    /// <summary>
    /// 歌曲封面 Url
    /// </summary>
    public string Pics { get; set; }

    /// <summary>
    /// 歌曲时长 ms
    /// </summary>
    public long Duration { get; set; }

    public string PublishDate { get; set; } = "";
}

/// <summary>
/// 歌词信息
/// </summary>
public class LyricVo
{
    /// <summary>
    /// 音乐提供商
    /// </summary>
    public SearchSourceEnum SearchSource;

    /// <summary>
    /// 歌词内容
    /// </summary>
    public string Lyric = "";

    /// <summary>
    /// 译文歌词内容
    /// </summary>
    public string TranslateLyric = "";

    /// <summary>
    /// 音译歌词内容
    /// </summary>
    public string TransliterationLyric = "";

    /// <summary>
    /// 歌曲时长 ms
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 歌词不存在判断
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Lyric) &&
               string.IsNullOrEmpty(TranslateLyric) &&
               string.IsNullOrEmpty(TransliterationLyric);
    }

    /// <summary>
    /// 纯音乐判断
    /// </summary>
    /// <returns></returns>
    public bool IsPureMusic()
    {
        // 原文歌词不为空 && 译文歌词必须为空
        if (string.IsNullOrEmpty(Lyric) || !string.IsNullOrEmpty(TranslateLyric))
        {
            return false;
        }

        if (SearchSource == SearchSourceEnum.NET_EASE_MUSIC)
        {
            return Lyric.Contains("纯音乐，请欣赏");
        }

        if (SearchSource == SearchSourceEnum.QQ_MUSIC)
        {
            return Lyric.Contains("此歌曲为没有填词的纯音乐，请您欣赏");
        }

        return false;
    }
}

public class LyricTimestamp : IComparable
{
    public long TimeOffset { get; }

    public LyricTimestamp(long millisecond)
    {
        TimeOffset = millisecond;
    }

    public LyricTimestamp Add(long millisecond)
    {
        return new LyricTimestamp(TimeOffset + millisecond);
    }

    /// <summary>
    /// 初始化 LyricTimestamp
    /// </summary>
    /// <param name="timestamp">[mm:ss.SSS] or [mm:ss] or [mm:ss:SSS]</param>
    public LyricTimestamp(string timestamp)
    {
        long timeOffset = 0; // 默认值
        
        if (!string.IsNullOrWhiteSpace(timestamp) && timestamp[0] == '[' && timestamp[timestamp.Length - 1] == ']')
        {
            timestamp = timestamp.Substring(1, timestamp.Length - 2);

            var split = timestamp.Split(':');

            var minute = GlobalUtils.ToInt(split[0], 0);

            if (split.Length > 1)
            {
                int second = 0, millisecond = 0;
                
                // 处理秒和毫秒部分，支持点号分隔和冒号分隔
                if (split[1].Contains('.'))
                {
                    // 处理 [mm:ss.SS] 或 [mm:ss.SSS] 格式
                    var secondMilliSplit = split[1].Split('.');
                    second = GlobalUtils.ToInt(secondMilliSplit[0], 0);
                    
                    if (secondMilliSplit.Length > 1)
                    {
                        var milliPart = secondMilliSplit[1];
                        // 根据毫秒位数处理
                        if (milliPart.Length == 1) 
                        {
                            // 1位毫秒数，乘以100
                            millisecond = GlobalUtils.ToInt(milliPart, 0) * 100;
                        }
                        else if (milliPart.Length == 2)
                        {
                            // 2位毫秒数，乘以10
                            millisecond = GlobalUtils.ToInt(milliPart, 0) * 10;
                        }
                        else
                        {
                            // 3位或更多毫秒数，取前3位
                            if (milliPart.Length > 3)
                            {
                                milliPart = milliPart.Substring(0, 3);
                            }
                            millisecond = GlobalUtils.ToInt(milliPart, 0);
                        }
                    }
                    
                    timeOffset = (minute * 60 + second) * 1000 + millisecond;
                }
                else if (split.Length > 2 && split[2].Contains('.'))
                {
                    // 处理 [mm:ss:SS.SS] 或 [mm:ss:SS.SSS] 格式
                    second = GlobalUtils.ToInt(split[1], 0);
                    var milliSplit = split[2].Split('.');
                    
                    if (milliSplit.Length > 1)
                    {
                        var milliPart = milliSplit[1];
                        // 根据毫秒位数处理
                        if (milliPart.Length == 1) 
                        {
                            // 1位毫秒数，乘以100
                            millisecond = GlobalUtils.ToInt(milliPart, 0) * 100;
                        }
                        else if (milliPart.Length == 2)
                        {
                            // 2位毫秒数，乘以10
                            millisecond = GlobalUtils.ToInt(milliPart, 0) * 10;
                        }
                        else
                        {
                            // 3位或更多毫秒数，取前3位
                            if (milliPart.Length > 3)
                            {
                                milliPart = milliPart.Substring(0, 3);
                            }
                            millisecond = GlobalUtils.ToInt(milliPart, 0);
                        }
                    }
                    
                    timeOffset = (minute * 60 + second) * 1000 + millisecond;
                }
                else if (split.Length > 2)
                {
                    // 处理 [mm:ss:SS] 或 [mm:ss:SSS] 格式
                    second = GlobalUtils.ToInt(split[1], 0);
                    var milliPart = split[2];
                    
                    // 根据毫秒位数处理
                    if (milliPart.Length == 1) 
                    {
                        // 1位毫秒数，乘以100
                        millisecond = GlobalUtils.ToInt(milliPart, 0) * 100;
                    }
                    else if (milliPart.Length == 2)
                    {
                        // 2位毫秒数，乘以10
                        millisecond = GlobalUtils.ToInt(milliPart, 0) * 10;
                    }
                    else
                    {
                        // 3位或更多毫秒数，取前3位
                        if (milliPart.Length > 3)
                        {
                            milliPart = milliPart.Substring(0, 3);
                        }
                        millisecond = GlobalUtils.ToInt(milliPart, 0);
                    }
                    
                    timeOffset = (minute * 60 + second) * 1000 + millisecond;
                }
                else
                {
                    // 只有秒部分，没有毫秒 [mm:ss] 格式
                    second = GlobalUtils.ToInt(split[1], 0);
                    timeOffset = (minute * 60 + second) * 1000;
                }
            }
            else
            {
                // 只有分钟部分 [mm] 格式
                timeOffset = minute * 60 * 1000;
            }
        }
        
        // 设置最终的TimeOffset值
        TimeOffset = timeOffset;
    }

    public int CompareTo(object input)
    {
        if (!(input is LyricTimestamp obj))
        {
            throw new MusicLyricException(ErrorMsgConst.SYSTEM_ERROR);
        }

        if (TimeOffset == obj.TimeOffset)
        {
            return 0;
        }

        if (TimeOffset == -1)
        {
            return -1;
        }

        if (obj.TimeOffset == -1)
        {
            return 1;
        }

        if (TimeOffset > obj.TimeOffset)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    public string PrintTimestamp(string timestampFormat, DotTypeEnum dotType)
    {
        var output = timestampFormat;

        int msDigit;
        if (output.Contains("SSS"))
        {
            msDigit = 3;
        }
        else if (output.Contains("SS"))
        {
            msDigit = 2;
        }
        else if (output.Contains("S"))
        {
            msDigit = 1;
        }
        else
        {
            msDigit = 3;
        }

        long offset;
        if (msDigit == 3)
        {
            offset = TimeOffset;
        }
        else
        {
            if (dotType == DotTypeEnum.DOWN)
            {
                offset = TimeOffset / (int)Math.Pow(10, 3 - msDigit) * (int)Math.Pow(10, 3 - msDigit);
            }
            else
            {
                var divisor = (int)Math.Pow(10, 3 - msDigit);
                offset = (TimeOffset + divisor/2) / divisor * divisor;
            }
        }

        var ms = offset % 1000;
        offset /= 1000;
        var minute = offset / 60;
        var second = offset - minute * 60;

        long actualMinute;
        if (output.Contains("HH"))
        {
            var hour = minute / 60;
            actualMinute = minute % 60;
            output = output.Replace("HH", hour.ToString("00"));
        }
        else
        {
            actualMinute = minute;
        }

        if (output.Contains("mm"))
        {
            output = output.Replace("mm", actualMinute.ToString("00"));
        }

        if (output.Contains("ss"))
        {
            output = output.Replace("ss", second.ToString("00"));
        }

        if (output.Contains("SSS"))
        {
            output = output.Replace("SSS", ms.ToString("000"));
        }

        if (output.Contains("SS"))
        {
            output = output.Replace("SS", (ms / 10).ToString("00"));
        }

        if (output.Contains("S"))
        {
            output = output.Replace("S", (ms / 100).ToString("0"));
        }

        return output;
    }
}

/// <summary>
/// 单行歌词信息
/// </summary>
public class LyricLineVo : IComparable
{
    /// <summary>
    /// 公共时间戳正则表达式模式，匹配 [mm:ss] 或 [mm:ss.SSS] 或 [mm:ss:SSS] 格式
    /// </summary>
    public const string TimestampPattern = @"\[\d+:\d+(?:[.:]\d+)?]";
    
    public LyricTimestamp Timestamp { get; set; }

    /// <summary>
    /// 歌词正文
    /// </summary>
    public string Content { get; set; }

    public LyricLineVo(string content, LyricTimestamp timestamp)
    {
        Timestamp = timestamp;
        Content = content;
    }

    public LyricLineVo(string lyricLine)
    {
        if (VerbatimLyricUtils.GetVerbatimLegalPrefixRegex().IsMatch(lyricLine) ||
            LyricUtils.GetCommonLegalPrefixRegex().IsMatch(lyricLine))
        {
            var index = lyricLine.IndexOf("]", StringComparison.Ordinal);
            Timestamp = new LyricTimestamp(lyricLine[..(index + 1)]);
            Content = lyricLine[(index + 1)..];
        }
        else
        {
            Timestamp = new LyricTimestamp("");
            Content = lyricLine;
        }
    }

    public static List<LyricLineVo> Split(LyricLineVo main)
    {
        var mainContent = main.Content;
        var mc = Regex.Matches(mainContent, TimestampPattern);

        // not exist sub
        if (mc.Count == 0)
        {
            return new List<LyricLineVo> { main };
        }

        var result = new List<LyricLineVo>();

        var index = 0;

        string timestamp = "", nextTimestamp = "";
        int timestampIndex = 0, nextTimestampIndex = 0;
        while (index < mc.Count)
        {
            if (index == 0)
            {
                timestamp = mc[index].Value;
                timestampIndex = mainContent.IndexOf(timestamp, StringComparison.Ordinal);

                // add first
                result.Add(new LyricLineVo(mainContent[..timestampIndex], main.Timestamp));
            }

            // find next timestamp
            if (index + 1 < mc.Count)
            {
                nextTimestamp = mc[index + 1].Value;
                nextTimestampIndex = mainContent.IndexOf(nextTimestamp, timestampIndex + timestamp.Length,
                    StringComparison.Ordinal);

                // add current
                var startIndex = timestampIndex + timestamp.Length;
                var content = mainContent.Substring(startIndex, nextTimestampIndex - startIndex);
                result.Add(new LyricLineVo(content, new LyricTimestamp(timestamp)));
            }
            else
            {
                // already in end
                var content = mainContent[(timestampIndex + timestamp.Length)..];
                result.Add(new LyricLineVo(content, new LyricTimestamp(timestamp)));
            }

            // let next to current
            timestamp = nextTimestamp;
            timestampIndex = nextTimestampIndex;

            index++;
        }

        return result;
    }

    public int CompareTo(object input)
    {
        if (!(input is LyricLineVo obj))
        {
            throw new MusicLyricException(ErrorMsgConst.SYSTEM_ERROR);
        }

        return Timestamp.CompareTo(obj.Timestamp);
    }

    /// <summary>
    /// 是否是无效的内容
    /// </summary>
    public bool IsIllegalContent()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return true;
        }

        if ("//".Equals(Content.Trim()))
        {
            return true;
        }

        return false;
    }

    public string Print(string timestampFormat, DotTypeEnum dotType)
    {
        return Timestamp.PrintTimestamp(timestampFormat, dotType) + Content.Trim();
    }
}

public class InputSongId
{
    public string QueryId { get; }

    public string SongId { get; }

    public SearchSourceEnum SearchSource { get; }

    public SearchTypeEnum SearchType { get; }

    public InputSongId(string queryId, SearchSourceEnum searchSource, SearchTypeEnum searchType)
    {
        QueryId = queryId;
        SearchSource = searchSource;
        SearchType = searchType;
    }

    public InputSongId(string songId, InputSongId other) : this(other.QueryId, other.SearchSource, other.SearchType)
    {
        SongId = songId;
    }
}

public class ResultVo<T>
{
    public T Data { get; set; }

    public string ErrorMsg { get; set; }

    private bool IsSuccessful { get; init; }

    private ResultVo()
    {
    }

    public static ResultVo<T> Failure(string errorMsg)
    {
        return new ResultVo<T>
        {
            Data = default,
            ErrorMsg = errorMsg,
            IsSuccessful = false
        };
    }

    public ResultVo(T data)
    {
        Data = data;
        ErrorMsg = ErrorMsgConst.SUCCESS;
        IsSuccessful = true;
    }

    public bool IsSuccess()
    {
        return IsSuccessful;
    }

    public ResultVo<T> Assert()
    {
        if (!IsSuccess())
        {
            throw new MusicLyricException(ErrorMsg);
        }

        return this;
    }
}

public static class EnumHelper
{
    public static string ToDescription(this Enum val)
    {
        return MusicLyricApp.Core.Localization.LocalizationService.Get($"Enum.{val.GetType().Name}.{val}");
    }
}
