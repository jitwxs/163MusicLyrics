using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Bean
{
    // 双语歌词类型
    public enum ShowLrcTypeEnum
    {
        [Description("仅显示原文")] ONLY_ORIGIN = 0,
        [Description("仅显示译文")] ONLY_TRANSLATE = 1,
        [Description("优先原文（交错）")] ORIGIN_PRIOR_STAGGER = 2,
        [Description("优先译文（交错）")] TRANSLATE_PRIOR_STAGGER = 3,
        [Description("优先原文（独立）")] ORIGIN_PRIOR_ISOLATED = 4,
        [Description("优先译文（独立）")] TRANSLATE_PRIOR_ISOLATED = 5,
        [Description("优先原文（合并）")] ORIGIN_PRIOR_MERGE = 6,
        [Description("优先译文（合并）")] TRANSLATE_PRIOR_MERGE = 7,
    }

    // 输出文件名类型
    public enum OutputFilenameTypeEnum
    {
        [Description("歌曲名 - 歌手")] NAME_SINGER = 0,
        [Description("歌手 - 歌曲名")] SINGER_NAME = 1,
        [Description("歌曲名")] NAME = 2
    }

    // 搜索来源
    public enum SearchSourceEnum
    {
        [Description("网易云")] NET_EASE_MUSIC = 0,
        [Description("QQ音乐")] QQ_MUSIC = 1
    }

    // 搜索类型
    public enum SearchTypeEnum
    {
        [Description("单曲")] SONG_ID = 0,
        [Description("专辑")] ALBUM_ID = 1
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
        [Description("GB-2312")] GB_2312 = 2,
        [Description("GBK")] GBK = 3,
        [Description("UNICODE")] UNICODE = 4
    }

    public enum OutputFormatEnum
    {
        [Description("lrc文件(*.lrc)|*.lrc")] LRC = 0,

        [Description("srt文件(*.srt)|*.srt")] SRT = 1
    }

    // 罗马音转换模式
    public enum RomajiModeEnum
    {
        [Description("标准模式")] NORMAL = 0,
        [Description("空格分组")] SPACED = 1,
        [Description("送假名")] OKURIGANA = 2,
        [Description("注音假名")] FURIGANA = 3,
    }

    // 罗马音字体系
    public enum RomajiSystemEnum
    {
        [Description("日本式")] NIPPON = 0,
        [Description("护照式")] PASSPORT = 1,
        [Description("平文式")] HEPBURN = 2,
    }

    // 译文缺省规则
    public enum TranslateLyricDefaultRuleEnum
    {
        [Description("忽略展示")] IGNORE = 0,
        [Description("展示空行")] EMPTY_LINE = 1,
        [Description("填充原文")] FILL_ORIGIN = 2,
    }

    /**
     * 错误码
     */
    public static class ErrorMsg
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

        public const string GET_LATEST_VERSION_FAILED = "获取最新版本失败";
        public const string THIS_IS_LATEST_VERSION = "当前版本已经是最新版本";
        public const string SYSTEM_ERROR = "系统错误";
        public const string NETWORK_ERROR = "网络错误，请检查网络链接";
        public const string API_RATE_LIMIT = "请求过于频繁，请稍后再试";
    }

    /// <summary>
    /// 封装单首歌曲的持久化信息
    /// </summary>
    public class SaveVo
    {
        public SaveVo(string songId, SongVo songVo, LyricVo lyricVo)
        {
            SongId = songId;
            SongVo = songVo;
            LyricVo = lyricVo;
        }

        public string SongId { get; }

        public SongVo SongVo { get; }

        public LyricVo LyricVo { get; }
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public class SearchResultVo
    {
        public SearchTypeEnum SearchType { get; set; }

        public readonly List<SongSearchResultVo> SongVos = new List<SongSearchResultVo>();

        public readonly List<AlbumSearchResultVo> AlbumVos = new List<AlbumSearchResultVo>();

        public bool IsEmpty()
        {
            return SongVos.Count == 0 && AlbumVos.Count == 0;
        }
        
        public class SongSearchResultVo
        {
            public string DisplayId { get; set; }
            
            public string SongName { get; set; }

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
            /// 发行公司
            /// </summary>
            public string Company { get; set; }
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
        public string Singer { get; set; }
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
        /// 歌曲链接 Url
        /// </summary>
        public string Links { get; set; }
        
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
        /// 歌词内容
        /// </summary>
        public string Lyric;

        /// <summary>
        /// 译文歌词内容
        /// </summary>
        public string TranslateLyric;

        /// <summary>
        /// 歌曲时长 ms
        /// </summary>
        public long Duration { get; set; }

        public void SetLyric(string content)
        {
            Lyric = HttpUtility.HtmlDecode(content);
        }

        public void SetTranslateLyric(string content)
        {
            TranslateLyric = HttpUtility.HtmlDecode(content);
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Lyric) && string.IsNullOrEmpty(TranslateLyric);
        }
    }

    public class LyricTimestamp : IComparable
    {
        public long TimeOffset { get; }

        public LyricTimestamp(long millisecond)
        {
            TimeOffset = millisecond;
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

                var minute = GlobalUtils.toInt(split[0], 0);

                int second = 0, millisecond = 0;
                if (split.Length > 1)
                {
                    split = split[1].Split('.');

                    second = GlobalUtils.toInt(split[0], 0);

                    if (split.Length > 1)
                    {
                        // 三位毫秒，右填充 0
                        millisecond = GlobalUtils.toInt(split[1].PadRight(3, '0'), 0);
                    }
                }

                TimeOffset = (minute * 60 + second) * 1000 + millisecond;
            }
        }

        public int CompareTo(object input)
        {
            if (!(input is LyricTimestamp obj))
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
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
            var index = lyricLine.IndexOf("]");
            if (index == -1)
            {
                Timestamp = new LyricTimestamp("");
                Content = lyricLine;
            }
            else
            {
                Timestamp = new LyricTimestamp(lyricLine.Substring(0, index + 1));
                Content = lyricLine.Substring(index + 1);
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
                    nextTimestampIndex = mainContent.IndexOf(nextTimestamp, timestampIndex + timestamp.Length, StringComparison.Ordinal);
                    
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
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
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

    /// <summary>
    /// 搜索信息
    /// </summary>
    public class SearchInfo
    {
        /// <summary>
        /// 输入 ID 列表
        /// </summary>
        public string[] InputIds { get; set; }

        /// <summary>
        /// 实际处理的歌曲 ID 列表
        /// </summary>
        public readonly HashSet<string> SongIds = new HashSet<string>();

        public SettingBean SettingBeanBackup { get; set; }

        public SettingBean SettingBean { get; set; }
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
            ErrorMsg = Bean.ErrorMsg.SUCCESS;
        }

        public bool IsSuccess()
        {
            return ErrorMsg == Bean.ErrorMsg.SUCCESS;
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
}