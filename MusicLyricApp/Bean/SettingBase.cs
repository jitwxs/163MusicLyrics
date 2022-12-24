using System;

namespace MusicLyricApp.Bean
{
    public class SettingBean
    {
        public readonly ConfigBean Config = new ConfigBean();

        public PersistParamBean Param = new PersistParamBean();
    }

    public class ConfigBean
    {
        /// <summary>
        /// 参数记忆
        /// </summary>
        public bool RememberParam = false;

        /// <summary>
        /// 自读读取剪切板
        /// </summary>
        public bool AutoReadClipboard = false;

        /// <summary>
        /// 自动检查更新
        /// </summary>
        public bool AutoCheckUpdate = true;
        
        /// <summary>
        /// 启用逐字歌词模式
        /// </summary>
        public bool EnableVerbatimLyric = false;
        
        /// <summary>
        /// 译文缺省规则
        /// </summary>
        public TranslateLyricDefaultRuleEnum TranslateLyricDefaultRule = TranslateLyricDefaultRuleEnum.IGNORE;

        /// <summary>
        /// QQ 音乐 Cookie
        /// </summary>
        public string QQMusicCookie = "";

        /// <summary>
        /// 网易云音乐 Cookie
        /// </summary>
        public string NetEaseCookie = "";
        
        /// <summary>
        /// 罗马音相关配置
        /// </summary>
        public RomajiConfigBean RomajiConfig = new RomajiConfigBean();
    }

    public class RomajiConfigBean
    {
        /// <summary>
        /// 译文显示罗马音
        /// </summary>
        public bool Enable = false;

        /// <summary>
        /// 罗马音转换模式
        /// </summary>
        public RomajiModeEnum ModeEnum = RomajiModeEnum.SPACED;

        /// <summary>
        /// 罗马音字体系
        /// </summary>
        public RomajiSystemEnum SystemEnum = RomajiSystemEnum.HEPBURN;
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
        /// LRC 歌词时间戳格式
        /// </summary>
        public string LrcTimestampFormat = "[mm:ss.SSS]";

        /// <summary>
        /// SRT 歌词时间戳格式
        /// </summary>
        public string SrtTimestampFormat = "HH:mm:ss,SSS";

        /// <summary>
        /// 译文歌词匹配精度
        /// </summary>
        public int TranslateMatchPrecisionDeviation = 0;

        /// <summary>
        /// 忽略空的歌词行
        /// </summary>
        public bool IgnoreEmptyLyric = true;

        /// <summary>
        /// 小数位处理策略
        /// </summary>
        public DotTypeEnum DotType = DotTypeEnum.DOWN;

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
    }
}