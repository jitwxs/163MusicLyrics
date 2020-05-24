using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class ErrorMsg
    {
        public static string MUST_SEARCH_BEFORE_SAVE = "您必须先搜索，才能保存内容";
        public static string MUST_SEARCH_BEFORE_COPY_SONG_URL = "您必须先搜索，才能获取直链";
        public static string INPUT_ID_ILLEGAG = "您输入的 ID 号不合法";
        public static string SONG_NOT_EXIST = "歌曲信息暂未被收录";
        public static string LRC_NOT_EXIST = "歌词信息暂未被收录";
        public static string FUNCTION_NOT_SUPPORT = "该功能暂不可用，请等待后续更新";
        public static string SONG_URL_COPY_SUCESS = "歌曲直链，复制成功";
        public static string FILE_NAME_IS_EMPTY = "输出文件名不能为空";
    }

    public class SaveVO
    {
        public SaveVO(SongVO songVO, LyricVO lyricVO)
        {
            this.songVO = songVO;
            this.lyricVO = lyricVO;
        }

        public SongVO songVO { get; set; }

        public LyricVO lyricVO { get; set; }
    }

    public class SongVO
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string Name { get; set; }

        public string Singer { get; set; }

        public string Album { get; set; }

        public string Links { get; set; }
    }

    public class LyricVO
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string Lyric { get; set; }

        public string TLyric { get; set; }

        public string Output { get; set; }
    }

    public class SearchInfo
    {
        public SEARCH_TYPE_ENUM SerchType { get; set; }

        public OUTPUT_FILENAME_TYPE_ENUM OutputFileNameType { get; set; }

        public SHOW_LRC_TYPE_ENUM ShowLrcType { get; set; }

        public string SearchId { get; set; }

        public string Encoding { get; set; }

        public string LrcMergeSeparator { get; set; }

        public bool BatchSearch { get; set; }

        public bool Constraint2Dot { get; set; }
    }
}
