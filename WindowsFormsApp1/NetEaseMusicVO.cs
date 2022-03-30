using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace 网易云歌词提取
{
    // 双语歌词类型
    public enum SHOW_LRC_TYPE_ENUM
    {
        ONLY_ORIGIN = 0, // 仅显示原文
        ONLY_TRANSLATE = 1, // 仅显示译文
        ORIGIN_PRIOR = 2, // 优先原文
        TRANSLATE_PRIOR = 3, // 优先译文
        MERGE_ORIGIN = 4, // 合并显示，优先原文
        MERGE_TRANSLATE = 5, // 合并显示，优先译文
    }

    // 输出文件名类型
    public enum OUTPUT_FILENAME_TYPE_ENUM
    {
        NAME_SINGER = 0, // 歌曲名 - 歌手
        SINGER_NAME = 1, // 歌手 - 歌曲名
        NAME = 2 // 歌曲名
    }

    // 搜索类型
    public enum SEARCH_TYPE_ENUM
    {
        SONG_ID = 0, // 歌曲ID
        ALBUM_ID = 1 // 专辑ID
    }

    // 强制两位类型
    public enum DOT_TYPE_ENUM
    {
        DISABLE = 0, // 不启用
        DOWN = 1, // 截位
        HALF_UP = 2 // 四舍五入
    }

    // 输出文件格式
    public enum OUTPUT_ENCODING_ENUM
    {
        UTF_8 = 0,
        UTF_8_BOM = 1,
        GB_2312 = 2,
        GBK = 3,
        UNICODE = 4
    }

    public enum OUTPUT_FORMAT_ENUM
    {
        [Description("lrc文件(*.lrc)|*.lrc")]
        LRC = 0,
        
        [Description("srt文件(*.srt)|*.srt")]
        SRT = 1
    }

    /**
     * 错误码
     */
    public static class ErrorMsg
    {
        public static string SUCCESS = "成功";
        public static string SEARCH_RESULT_STAGE = "查询成功，结果已暂存";
        public static string MUST_SEARCH_BEFORE_SAVE = "您必须先搜索，才能保存内容";
        public static string MUST_SEARCH_BEFORE_COPY_SONG_URL = "您必须先搜索，才能获取直链";
        public static string INPUT_ID_ILLEGAG = "您输入的 ID 号不合法";
        public static string INPUT_ALBUM_ILLEGAG = "您输入的 专辑编号 不合法";
        public static string SONG_NOT_EXIST = "歌曲信息暂未被收录或查询失败";
        public static string LRC_NOT_EXIST = "歌词信息暂未被收录或查询失败";
        public static string FUNCTION_NOT_SUPPORT = "该功能暂不可用，请等待后续更新";
        public static string SONG_URL_COPY_SUCESS = "歌曲直链，已复制到剪切板";
        public static string SONG_URL_GET_FAILED = "歌曲直链，获取失败";
        public static string FILE_NAME_IS_EMPTY = "输出文件名不能为空";
        public static string SAVE_SUCCESS = "保存成功";

        public static string GET_LATEST_VERSION_FAILED = "获取最新版本失败";
        public static string THIS_IS_LATEST_VERSION = "当前版本已经是最新版本";
        public static string EXIST_LATEST_VERSION = "检测到最新版本 {0}，下载地址已复制到剪切板";
    }

    public class SaveVo
    {
        public SaveVo(long songId, SongVo songVo, LyricVo lyricVo)
        {
            SongId = songId;
            SongVo = songVo;
            LyricVo = lyricVo;
        }

        public long SongId { get; set; }

        public SongVo SongVo { get; set; }

        public LyricVo LyricVo { get; set; }
    }

    public class SongVo
    {
        public string Name { get; set; }

        public string Singer { get; set; }

        public string Album { get; set; }

        public string Links { get; set; }
        
        public long DateTime { get; set; }
    }

    public class LyricVo
    {
        public string Lyric { get; set; }

        public string TLyric { get; set; }
        
        public long DateTime { get; set; }

        public string Output { get; set; }
    }

    public class SearchInfo
    {
        public SEARCH_TYPE_ENUM SearchType { get; set; }

        public OUTPUT_FILENAME_TYPE_ENUM OutputFileNameType { get; set; }

        public SHOW_LRC_TYPE_ENUM ShowLrcType { get; set; }

        public string[] InputIds { get; set; }

        public readonly HashSet<long> SONG_IDS = new HashSet<long>();

        public OUTPUT_ENCODING_ENUM Encoding { get; set; }

        public string LrcMergeSeparator { get; set; }

        public DOT_TYPE_ENUM DotType { get; set; }
        
        public OUTPUT_FORMAT_ENUM OutputFileFormat { get; set; }
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
 
            return (attributes.Single() as DescriptionAttribute).Description;
        }
    }
}
