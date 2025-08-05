using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Utils;

namespace MusicLyricApp.Models;

// 双语歌词类型
public enum ShowLrcTypeEnum
{
    [Description("交错")] STAGGER = 0,
    [Description("独立")] ISOLATED = 1,
    [Description("合并")] MERGE = 2,
}

// 搜索来源
public enum SearchSourceEnum
{
    [Description("网易云")] NET_EASE_MUSIC = 0,
    [Description("QQ音乐")] QQ_MUSIC = 1,
    [Description("汽水音乐")] SODA_MUSIC = 2
}

// 搜索类型
public enum SearchTypeEnum
{
    [Description("单曲")] SONG_ID = 0,
    [Description("专辑")] ALBUM_ID = 1,
    [Description("歌单")] PLAYLIST_ID = 2,
}

// 强制两位类型
public enum DotTypeEnum
{
    [Description("截位")] DOWN = 0,
    [Description("四舍五入")] HALF_UP = 1
}

// 输出文件格式
public enum OutputEncodingEnum
{
    [Description("UTF-8")] UTF_8 = 0,
    [Description("UTF-8-BOM")] UTF_8_BOM = 1,
    [Description("UTF-32")] UTF_32 = 2,
    [Description("UNICODE")] UNICODE = 3
}

public enum OutputFormatEnum
{
    [Description("LRC")] LRC = 0,

    [Description("SRT")] SRT = 1
}

// 译文缺省规则
public enum TransLyricLostRuleEnum
{
    [Description("忽略展示")] IGNORE = 0,
    [Description("展示空行")] EMPTY_LINE = 1,
    [Description("填充原文")] FILL_ORIGIN = 2,
}

// 中文处理策略
public enum ChineseProcessRuleEnum
{
    [Description("不额外处理")] IGNORE = 0,
    [Description("强制简体")] SIMPLIFIED_CHINESE = 1,
    [Description("强制繁体")] TRADITIONAL_CHINESE = 2,
}

public enum LyricsTypeEnum
{
    [Description("原文")] ORIGIN = 0,
    [Description("原始译文")] ORIGIN_TRANS = 1,
    [Description("中文译文")] CHINESE = 2,
    [Description("英文译文")] ENGLISH = 3,
    [Description("音译文")] TRANSLITERATION = 4,
    [Description("拼音译文")] PINYIN = 5,
}

public enum ThemeModeEnum
{
    [Description("跟随系统")] FOLLOW_SYSTEM = 0,
    [Description("亮色")] LIGHT = 1,
    [Description("暗黑")] DARK = 2,
}

public enum LanguageEnum
{
    /// <summary>
    /// 其他
    /// </summary>
    [Description("Other")] OTHER = 0,

    /// <summary>
    /// 英语
    /// </summary>
    [Description("English")] ENGLISH = 1,

    /// <summary>
    /// 日语
    /// </summary>
    [Description("Japanese")] JAPANESE = 2,

    /// <summary>
    /// 韩语
    /// </summary>
    [Description("Korean")] KOREAN = 3,

    /// <summary>
    /// 俄语
    /// </summary>
    [Description("Russian")] RUSSIAN = 4,

    /// <summary>
    /// 法语
    /// </summary>
    [Description("French")] FRENCH = 5,

    /// <summary>
    /// 意大利语
    /// </summary>
    [Description("Italian")] ITALIAN = 6,

    /// <summary>
    /// 汉语
    /// </summary>
    [Description("Chinese")] CHINESE = 7,
}

/**
 * 错误码
 */
public static class ErrorMsgConst
{
    public const string SUCCESS = "成功";
    public const string SEARCH_RESULT_EMPTY = "查询结果为空，请修改查询条件";
    public const string MUST_SEARCH_BEFORE_SAVE = "您必须先搜索，才能保存内容";
    public const string MUST_SEARCH_BEFORE_GET_SONG_URL = "您必须先搜索，才能获取歌曲链接";
    public const string MUST_SEARCH_BEFORE_GET_SONG_PIC = "您必须先搜索，才能获取歌曲封面";
    public const string INPUT_CONENT_EMPLT = "输入内容不能为空";
    public const string INPUT_ID_ILLEGAL = "您输入的ID不合法";
    public const string PLAYLIST_NOT_EXIST = "歌单信息暂未被收录或查询失败";
    public const string ALBUM_NOT_EXIST = "专辑信息暂未被收录或查询失败";
    public const string SONG_NOT_EXIST = "歌曲信息暂未被收录或查询失败";
    public const string LRC_NOT_EXIST = "歌词信息暂未被收录或查询失败";
    public const string FUNCTION_NOT_SUPPORT = "该功能暂不可用，请等待后续更新";
    public const string SONG_URL_GET_SUCCESS = "歌曲直链，已复制到剪切板";
    public const string SONG_URL_GET_FAILED = "歌曲直链，获取失败";
    public const string SONG_PIC_GET_SUCCESS = "歌曲封面，已复制到剪切板";
    public const string SONG_PIC_GET_FAILED = "歌曲封面，获取失败";
    public const string DEPENDENCY_LOSS = "缺少必须依赖，请前往项目主页下载 {0} 插件";
    public const string SAVE_COMPLETE = "保存完毕，成功 {0} 跳过 {1}";
    public const string NEED_LOGIN = "本请求需要登陆信息才可使用，请检查 Cookie 是否填写或过期";
    public const string PURE_MUSIC_IGNORE_SAVE = "该首歌曲是纯音乐，根据设置跳过保存";

    public const string TRANSLATE_LANGUAGE_NOT_SUPPORT = "翻译 API 语言暂不支持，请更换其他语言";
    public const string NOT_EXIST_TRANSLATE_API = "未配置任何的翻译 API，请关闭自定义语言的翻译类型，或配置 API";
    public const string CAIYUN_TRANSLATE_AUTH_FAILED = "彩云小译调用失败，请检查相关鉴权配置";
    public const string BAIDU_TRANSLATE_AUTH_FAILED = "百度翻译调用失败，请检查相关鉴权配置";

    public const string GET_LATEST_VERSION_FAILED = "获取最新版本失败";
    public const string THIS_IS_LATEST_VERSION = "当前版本已经是最新版本";
    public const string SYSTEM_ERROR = "系统错误";
    public const string NETWORK_ERROR = "网络错误，请检查网络链接";
    public const string API_RATE_LIMIT = "请求过于频繁，请稍后再试";
    public const string STORAGE_FOLDER_ERROR = "存储文件夹获取错误";
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
    /// <param name="timestamp">[mm:ss.SSS] or [mm:ss]</param>
    public LyricTimestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp) || timestamp[0] != '[' || timestamp[timestamp.Length - 1] != ']')
        {
            // 不支持的格式
            TimeOffset = 0;
        }
        else
        {
            timestamp = timestamp.Substring(1, timestamp.Length - 2);

            var split = timestamp.Split(':');

            var minute = GlobalUtils.ToInt(split[0], 0);

            int second = 0, millisecond = 0;
            if (split.Length > 1)
            {
                split = split[1].Split('.');

                second = GlobalUtils.ToInt(split[0], 0);

                if (split.Length > 1)
                {
                    // 三位毫秒，右填充 0
                    millisecond = GlobalUtils.ToInt(split[1].PadRight(3, '0'), 0);
                }
            }

            TimeOffset = (minute * 60 + second) * 1000 + millisecond;
        }
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
                offset = TimeOffset / 10 * 10;
            }
            else
            {
                offset = (TimeOffset + 5) / 10 * 10;
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
        if (LyricUtils.VerbatimLegalPrefixRegex.IsMatch(lyricLine) ||
            LyricUtils.CommonLegalPrefixRegex.IsMatch(lyricLine))
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
        const string timestampPattern = @"\[\d+:\d+.\d+]";

        var mainContent = main.Content;
        var mc = Regex.Matches(mainContent, timestampPattern);

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
                result.Add(new LyricLineVo(mainContent.Substring(0, timestampIndex), main.Timestamp));
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
                var content = mainContent.Substring(timestampIndex + timestamp.Length);
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
        SongId = queryId; // 修正：默认SongId等于QueryId
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

    private ResultVo()
    {
    }

    public static ResultVo<T> Failure(string errorMsg)
    {
        return new ResultVo<T>
        {
            Data = default,
            ErrorMsg = errorMsg
        };
    }

    public ResultVo(T data)
    {
        Data = data;
        ErrorMsg = ErrorMsgConst.SUCCESS;
    }

    public bool IsSuccess()
    {
        return ErrorMsg == ErrorMsgConst.SUCCESS;
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
        var type = val.GetType();
        var memberInfo = type.GetMember(val.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        //如果没有定义描述，就把当前枚举值的对应名称返回
        if (attributes == null || attributes.Length != 1) return val.ToString();

        return (attributes.Single() as DescriptionAttribute)?.Description;
    }
}