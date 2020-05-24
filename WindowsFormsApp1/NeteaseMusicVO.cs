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
        ONLY_ORIGIN, // 仅显示原文
        ONLY_TRANSLATE, // 仅显示译文
        ORIGIN_PRIOR, // 优先原文
        TRANSLATE_PRIOR, // 优先译文
        MERGE_ORIGIN, // 合并显示，优先原文
        MERGE_TRANSLATE, // 合并显示，优先译文
    }

    // 输出文件名类型
    public enum OUTPUT_FILENAME_TYPE_ENUM
    {
        NAME_SINGER, // 歌曲名 - 歌手
        SINGER_NAME, // 歌手 - 歌曲名
        NAME // 歌曲名
    }

    // 搜索类型
    public enum SEARCH_TYPE_ENUM
    {
        SONG_ID = 1, // 歌曲ID
        ALBUM_ID = 2 // 专辑ID
    }

    public class SaveVO
    {
        public SaveVO(SongVO songVO, LyricVO lyricVO, SearchInfo searchInfo)
        {
            this.songVO = songVO;
            this.lyricVO = lyricVO;
            this.searchInfo = searchInfo;
        }

        public SongVO songVO { get; set; }

        public LyricVO lyricVO { get; set; }

        public SearchInfo searchInfo { get; set; }
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
