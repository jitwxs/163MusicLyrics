using System;

namespace MusicLyricApp.Bean
{
    public class SettingBean
    {
        public readonly ConfigBean Config = new ConfigBean();

        public readonly PersistParamBean Param = new PersistParamBean();
    }

    public class ConfigBean
    {
        public bool RememberParam = false;

        public bool AutoReadClipboard = false;

        public bool AutoCheckUpdate = true;
    }

    public class PersistParamBean
    {
        /// <summary>
        /// 搜索来源
        /// </summary>
        public SearchSourceEnum SearchSource = SearchSourceEnum.NET_EASE_MUSIC;

        /// <summary>
        /// 搜索类型
        /// </summary>
        public SearchTypeEnum SearchType = SearchTypeEnum.SONG_ID;

        /// <summary>
        /// 歌词展示格式
        /// </summary>
        public ShowLrcTypeEnum ShowLrcType = ShowLrcTypeEnum.ONLY_ORIGIN;
        
        /// <summary>
        /// 指定歌词合并的分隔符
        /// </summary>
        public string LrcMergeSeparator = String.Empty;

        /// <summary>
        /// 小数位处理策略
        /// </summary>
        public DotTypeEnum DotType = DotTypeEnum.DISABLE;

        /// <summary>
        /// 输出文件名类型
        /// </summary>
        public OutputFilenameTypeEnum OutputFileNameType = OutputFilenameTypeEnum.NAME_SINGER;

        /// <summary>
        /// 输出文件格式
        /// </summary>
        public OutputFormatEnum OutputFileFormat = OutputFormatEnum.LRC;

        /// <summary>
        /// 输出文件编码
        /// </summary>
        public OutputEncodingEnum Encoding = OutputEncodingEnum.UTF_8;

        public void Update(SearchInfo searchInfo)
        {
            SearchSource = searchInfo.SearchSource;
            SearchType = searchInfo.SearchType;

            ShowLrcType = searchInfo.ShowLrcType;
            LrcMergeSeparator = searchInfo.LrcMergeSeparator;
            DotType = searchInfo.DotType;

            OutputFileNameType = searchInfo.OutputFileNameType;
            OutputFileFormat = searchInfo.OutputFileFormat;
            Encoding = searchInfo.Encoding;
        }
    }
}