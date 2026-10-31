using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicLyricApp.Models;

public enum NetworkProxyModeEnum
{
    SYSTEM_PROXY = 0,
    DIRECT_CONNECT = 1,
    HTTP_PROXY = 2
}


public enum FileConflictStrategyEnum
{
    OVERWRITE = 0,

    APPEND_NUMBER = 1
}

public class SettingBean
{
    public readonly ConfigBean Config = new();

    public readonly PersistParamBean Param = new();
}

public class ConfigBean
{
    /// <summary>UI language, stored independently from functional settings.</summary>
    public string Language = "zh-CN";

    /// <summary>
    /// LRC 歌词时间戳格式
    /// </summary>
    public string LrcTimestampFormat = "[mm:ss.SSS]";

    /// <summary>
    /// SRT 歌词时间戳格式
    /// </summary>
    public string SrtTimestampFormat = "HH:mm:ss,SSS";

    /// <summary>
    /// 逐字歌词模式
    /// </summary>
    public VerbatimLyricModeEnum VerbatimLyricMode = VerbatimLyricModeEnum.DISABLE;

    /// <summary>
    /// 忽略空的歌词行
    /// </summary>
    public bool IgnoreEmptyLyric = true;

    /// <summary>
    /// 小数位处理策略
    /// </summary>
    public DotTypeEnum DotType = DotTypeEnum.DOWN;
    
    /// <summary>
    /// 中文处理策略
    /// </summary>
    public ChineseProcessRuleEnum ChineseProcessRule = ChineseProcessRuleEnum.IGNORE;

    public ThemeModeEnum ThemeMode = ThemeModeEnum.FOLLOW_SYSTEM;
    
    /// <summary>
    /// 多个歌手的分隔符
    /// </summary>
    public string SingerSeparator = ",";

    /// <summary>
    /// 文件名歌手数量上限（-1 表示不截断）
    /// </summary>
    public int SingerCountLimit = -1;

    /// <summary>
    /// 聚合模糊搜索
    /// </summary>
    public bool AggregatedBlurSearch = false;

    /// <summary>
    /// 自读读取剪切板
    /// </summary>
    public bool AutoReadClipboard = false;

    /// <summary>
    /// 自动检查更新
    /// </summary>
    public bool AutoCheckUpdate = true;

    /// <summary>
    /// 关闭主窗口时是否二次确认
    /// </summary>
    public bool ConfirmBeforeExit = true;

    /// <summary>
    /// 保存时跳过纯音乐
    /// </summary>
    public bool IgnorePureMusicInSave = true;

    /// <summary>
    /// 对于 "独立" 歌词格式，保存在不同的文件中
    /// </summary>
    public bool SeparateFileForIsolated = false;

    /// <summary>
    /// 输出文件名格式
    /// </summary>
    public string OutputFileNameFormat = "${name} - ${singer}";

    /// <summary>
    /// 同名文件处理策略
    /// </summary>
    public FileConflictStrategyEnum FileConflictStrategy = FileConflictStrategyEnum.OVERWRITE;

    /// <summary>
    /// 同名文件追加后缀模板，使用 {n} 代表序号
    /// </summary>
    public string FileConflictSuffixPattern = " ({n})";

    /// <summary>
    /// 最近一次保存目录
    /// </summary>
    public string LastSaveFolderPath = "";

    /// <summary>
    /// 保存歌词时是否同时下载封面与直链音频
    /// </summary>
    public bool DownloadCoverAndSongLinkOnSave = false;

    /// <summary>
    /// 本地搜索缓存目录
    /// </summary>
    public string SearchCacheFolderPath = "";

    /// <summary>
    /// 本地搜索缓存最大大小（MB）
    /// </summary>
    public int SearchCacheMaxSizeMb = 128;

    /// <summary>
    /// 代理模式
    /// </summary>
    public NetworkProxyModeEnum NetworkProxyMode = NetworkProxyModeEnum.SYSTEM_PROXY;

    /// <summary>
    /// HTTP 代理地址
    /// </summary>
    public string ProxyHost = "";

    /// <summary>
    /// HTTP 代理端口
    /// </summary>
    public int ProxyPort = 80;

    /// <summary>
    /// HTTP 代理用户名
    /// </summary>
    public string ProxyUsername = "";

    /// <summary>
    /// HTTP 代理密码
    /// </summary>
    public string ProxyPassword = "";

    /// <summary>
    /// 输出的歌词类型列表
    /// </summary>
    public string OutputLyricTypes = string.Join(",", [(int)LyricsTypeEnum.ORIGIN, (int)LyricsTypeEnum.ORIGIN_TRANS]);

    /// <summary>
    /// QQ 音乐 Cookie
    /// </summary>
    public string QQMusicCookie = "";

    /// <summary>
    /// 网易云音乐 Cookie
    /// </summary>
    public string NetEaseCookie = "";

    public readonly TransConfigBean TransConfig = new();

    public List<LyricsTypeEnum> DeserializationOutputLyricsTypes()
    {
        return string.IsNullOrWhiteSpace(OutputLyricTypes)
            ? []
            : OutputLyricTypes.Split(',').Select(e => (LyricsTypeEnum)Convert.ToInt32(e)).ToList();
    }
}

public class TransConfigBean
{
    /// <summary>
    /// 译文缺省规则
    /// </summary>
    public TransLyricLostRuleEnum LostRule = TransLyricLostRuleEnum.IGNORE;

    /// <summary>
    /// 译文歌词匹配精度
    /// </summary>
    public int MatchPrecisionDeviation = 0;

    /// <summary>
    /// 百度翻译 APP ID
    /// </summary>
    public string BaiduTranslateAppId = "";

    /// <summary>
    /// 百度翻译密钥
    /// </summary>
    public string BaiduTranslateSecret = "";

    /// <summary>
    /// 彩云小译 Token
    /// </summary>
    public string CaiYunToken = "";
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
    public ShowLrcTypeEnum ShowLrcType = ShowLrcTypeEnum.STAGGER;

    /// <summary>
    /// 指定歌词合并的分隔符
    /// </summary>
    public string LrcMergeSeparator = string.Empty;

    /// <summary>
    /// 输出文件格式
    /// </summary>
    public OutputFormatEnum OutputFileFormat = OutputFormatEnum.LRC;

    /// <summary>
    /// 输出文件编码
    /// </summary>
    public OutputEncodingEnum Encoding = OutputEncodingEnum.UTF_8;
}
