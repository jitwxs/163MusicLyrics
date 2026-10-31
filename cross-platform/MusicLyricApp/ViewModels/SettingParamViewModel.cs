using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class SettingParamViewModel : ViewModelBase
{
    public sealed class LanguageOption(string code, string resourceKey) : INotifyPropertyChanged
    {
        public string Code { get; } = code;
        public string DisplayName => LocalizationService.Get(resourceKey);
        public event PropertyChangedEventHandler? PropertyChanged;
        public override string ToString() => DisplayName;
        public void RefreshDisplayName() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
    }

    public ObservableCollection<LanguageOption> Languages { get; } =
    [
        new("zh-CN", "Language.SimplifiedChinese"),
        new("en-US", "Language.English")
    ];

    [ObservableProperty] private LanguageOption? _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value == null || _settingBean == null) return;
        _settingBean.Config.Language = value.Code;
        LocalizationService.Instance.SetLanguage(value.Code);
    }

    // -1. 同名文件处理策略
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<FileConflictStrategyEnum>> FileConflictStrategies { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<FileConflictStrategyEnum>();

    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<FileConflictStrategyEnum> _selectedFileConflictStrategyItem;

    [ObservableProperty]
    private bool _isConflictSuffixEnabled;

    partial void OnSelectedFileConflictStrategyItemChanged(EnumDisplayHelper.EnumDisplayItem<FileConflictStrategyEnum>? value)
    {
        if (value == null)
        {
            return;
        }

        _settingBean.Config.FileConflictStrategy = value.Value;
        IsConflictSuffixEnabled = value.Value == FileConflictStrategyEnum.APPEND_NUMBER;
    }

    // 0. 网络代理模式
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<NetworkProxyModeEnum>> NetworkProxyModes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<NetworkProxyModeEnum>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHttpProxySelected))]
    private EnumDisplayHelper.EnumDisplayItem<NetworkProxyModeEnum> _selectedNetworkProxyModeItem;

    public bool IsHttpProxySelected => SelectedNetworkProxyModeItem?.Value == NetworkProxyModeEnum.HTTP_PROXY;

    partial void OnSelectedNetworkProxyModeItemChanged(EnumDisplayHelper.EnumDisplayItem<NetworkProxyModeEnum>? value)
    {
        if (value == null)
        {
            return;
        }

        _settingBean.Config.NetworkProxyMode = value.Value;
        NetworkClientFactory.Configure(_settingBean.Config);
    }

    // 0.1 HTTP 代理地址
    [ObservableProperty] private string _proxyHost;

    partial void OnProxyHostChanged(string value)
    {
        _settingBean.Config.ProxyHost = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 0.2 HTTP 代理端口
    [ObservableProperty] private string _proxyPortText;

    [ObservableProperty] private string _proxyPortError;

    partial void OnProxyPortTextChanged(string value)
    {
        if (!int.TryParse(value, out var port) || port < 1 || port > 65535)
        {
            ProxyPortError = LocalizationService.Get("Message.InvalidPort");
            return;
        }

        ProxyPortError = string.Empty;
        _settingBean.Config.ProxyPort = port;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 0.3 HTTP 代理用户名
    [ObservableProperty] private string _proxyUsername;

    partial void OnProxyUsernameChanged(string value)
    {
        _settingBean.Config.ProxyUsername = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 0.4 HTTP 代理密码
    [ObservableProperty] private string _proxyPassword;

    partial void OnProxyPasswordChanged(string value)
    {
        _settingBean.Config.ProxyPassword = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 1. 毫秒截位规则
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<DotTypeEnum>> DotTypes { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<DotTypeEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<DotTypeEnum> _selectedDotTypeItem;

    public DotTypeEnum SelectedDotType => SelectedDotTypeItem?.Value ?? default;
    
    partial void OnSelectedDotTypeItemChanged(EnumDisplayHelper.EnumDisplayItem<DotTypeEnum>? value)
    {
        if (value != null) _settingBean.Config.DotType = value.Value;
    }
    
    // 2. 译文缺省规则
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<TransLyricLostRuleEnum>> TransLyricLostRules { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<TransLyricLostRuleEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<TransLyricLostRuleEnum> _selectedTransLyricLostRuleItem;

    public TransLyricLostRuleEnum SelectedTransLyricLostRule => SelectedTransLyricLostRuleItem?.Value ?? default;
    
    partial void OnSelectedTransLyricLostRuleItemChanged(EnumDisplayHelper.EnumDisplayItem<TransLyricLostRuleEnum>? value)
    {
        if (value != null) _settingBean.Config.TransConfig.LostRule = value.Value;
    }
    
    // 3. 中文处理策略
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<ChineseProcessRuleEnum>> ChineseProcessRules { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<ChineseProcessRuleEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<ChineseProcessRuleEnum> _selectedChineseProcessRuleItem;

    public ChineseProcessRuleEnum SelectedChineseProcessRule => SelectedChineseProcessRuleItem?.Value ?? default;
    
    partial void OnSelectedChineseProcessRuleItemChanged(
        EnumDisplayHelper.EnumDisplayItem<ChineseProcessRuleEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<ChineseProcessRuleEnum>? newValue)
    {
        if (newValue != null) _settingBean.Config.ChineseProcessRule = newValue.Value;
    }
    
    // 4. 主题
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<ThemeModeEnum>> ThemeModeRules { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<ThemeModeEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<ThemeModeEnum> _selectedThemeModeRuleItem;

    public ThemeModeEnum SelectedThemeModeRule => SelectedThemeModeRuleItem?.Value ?? default;
    
    partial void OnSelectedThemeModeRuleItemChanged(
        EnumDisplayHelper.EnumDisplayItem<ThemeModeEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<ThemeModeEnum>? newValue)
    {
        if (newValue != null) _settingBean.Config.ThemeMode = newValue.Value;
    }
    
    // 5. LRC 时间戳
    [ObservableProperty] private string _lrcTimestampFormat;
    
    partial void OnLrcTimestampFormatChanged(string value)
    {
        _settingBean.Config.LrcTimestampFormat = value;
    }
    
    // 6. SRT 时间戳
    [ObservableProperty] private string _srtTimestampFormat;
    
    partial void OnSrtTimestampFormatChanged(string value)
    {
        _settingBean.Config.SrtTimestampFormat = value;
    }
    
    // 7. SRT 时间戳
    [ObservableProperty] private bool _ignoreEmptyLyric;
    
    partial void OnIgnoreEmptyLyricChanged(bool value)
    {
        _settingBean.Config.IgnoreEmptyLyric = value;
    }
    
    // 8. SRT 时间戳
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<VerbatimLyricModeEnum>> VerbatimLyricMode { get; } =
        EnumDisplayHelper.GetEnumDisplayCollection<VerbatimLyricModeEnum>();
    
    [ObservableProperty]
    private EnumDisplayHelper.EnumDisplayItem<VerbatimLyricModeEnum> _selectedVerbatimLyricModeItem;

    public VerbatimLyricModeEnum SelectedTVerbatimLyricMode => SelectedVerbatimLyricModeItem?.Value ?? default;
    
    partial void OnSelectedVerbatimLyricModeItemChanged(
        EnumDisplayHelper.EnumDisplayItem<VerbatimLyricModeEnum>? oldValue,
        EnumDisplayHelper.EnumDisplayItem<VerbatimLyricModeEnum>? newValue)
    {
        if (newValue != null) _settingBean.Config.VerbatimLyricMode = newValue.Value;
    }
    
    // 9. 译文匹配精度
    [ObservableProperty] private int _matchPrecisionDeviation;
    
    partial void OnMatchPrecisionDeviationChanged(int value)
    {
        _settingBean.Config.TransConfig.MatchPrecisionDeviation = value;
    }

    // 10. 百度 APP ID
    [ObservableProperty] private string _baiduAppId;
    
    partial void OnBaiduAppIdChanged(string value)
    {
        _settingBean.Config.TransConfig.BaiduTranslateAppId = value;
    }
    
    // 11. 百度密钥
    [ObservableProperty] private string _baiduSecret;
    
    partial void OnBaiduSecretChanged(string value)
    {
        _settingBean.Config.TransConfig.BaiduTranslateSecret = value;
    }
    
    // 12. 彩云小译 Token
    [ObservableProperty] private string _caiYunToken;
    
    partial void OnCaiYunTokenChanged(string value)
    {
        _settingBean.Config.TransConfig.CaiYunToken = value;
    }
    
    // 13. 跳过纯音乐
    [ObservableProperty] private bool _ignorePureMusicInSave;
    
    partial void OnIgnorePureMusicInSaveChanged(bool value)
    {
        _settingBean.Config.IgnorePureMusicInSave = value;
    }
    
    // 14. 独立歌词格式分文件保存
    [ObservableProperty] private bool _separateFileForIsolated;
    
    partial void OnSeparateFileForIsolatedChanged(bool value)
    {
        _settingBean.Config.SeparateFileForIsolated = value;
    }
    
    // 15. 保存文件名
    [ObservableProperty] private string _outputFileNameFormat;
    
    partial void OnOutputFileNameFormatChanged(string value)
    {
        _settingBean.Config.OutputFileNameFormat = value;
    }
    
    // 16. 聚合模糊搜索
    [ObservableProperty] private bool _aggregatedBlurSearch;
    
    partial void OnAggregatedBlurSearchChanged(bool value)
    {
        _settingBean.Config.AggregatedBlurSearch = value;
    }
    
    // 17. 自动读取剪切板
    [ObservableProperty] private bool _autoReadClipboard;
    
    partial void OnAutoReadClipboardChanged(bool value)
    {
        _settingBean.Config.AutoReadClipboard = value;
    }
    
    // 18. 自动检查更新
    [ObservableProperty] private bool _autoCheckUpdate;
    
    partial void OnAutoCheckUpdateChanged(bool value)
    {
        _settingBean.Config.AutoCheckUpdate = value;
    }
    
    // 19. QQ音乐 Cookie
    [ObservableProperty] private string _qqMusicCookie;
    
    partial void OnQqMusicCookieChanged(string value)
    {
        _settingBean.Config.QQMusicCookie = value;
    }
    
    // 20. 网易云 Cookie
    [ObservableProperty] private string _netEaseCookie;
    
    partial void OnNetEaseCookieChanged(string value)
    {
        _settingBean.Config.NetEaseCookie = value;
    }
    
    // 21. 歌手分隔符
    [ObservableProperty] private string _singerSeparator;
    
    partial void OnSingerSeparatorChanged(string newValue)
    {
        _settingBean.Config.SingerSeparator = newValue;
    }

    // 21.3 同名文件后缀模板
    [ObservableProperty] private string _fileConflictSuffixPattern;

    partial void OnFileConflictSuffixPatternChanged(string value)
    {
        _settingBean.Config.FileConflictSuffixPattern = value;
    }

    // 21.2 关闭时确认
    [ObservableProperty] private bool _confirmBeforeExit;

    partial void OnConfirmBeforeExitChanged(bool value)
    {
        _settingBean.Config.ConfirmBeforeExit = value;
    }

    // 21.1 文件名歌手数上限（-1 不截断）
    [ObservableProperty] private int _singerCountLimit;

    partial void OnSingerCountLimitChanged(int value)
    {
        _settingBean.Config.SingerCountLimit = value < -1 ? -1 : value;
    }

    // 22. 歌词合并符
    [ObservableProperty] private string _lrcMergeSeparator;

    partial void OnLrcMergeSeparatorChanged(string value)
    {
        _settingBean.Param.LrcMergeSeparator = value;
    }

    // 23. 保存路径
    [ObservableProperty] private string _saveFolderPath;

    partial void OnSaveFolderPathChanged(string value)
    {
        _settingBean.Config.LastSaveFolderPath = value;
    }

    // 24. 保存歌词时下载封面与直链
    [ObservableProperty] private bool _downloadCoverAndSongLinkOnSave;

    partial void OnDownloadCoverAndSongLinkOnSaveChanged(bool value)
    {
        _settingBean.Config.DownloadCoverAndSongLinkOnSave = value;
    }

    // 25. 搜索缓存目录
    [ObservableProperty] private string _searchCacheFolderPath;

    partial void OnSearchCacheFolderPathChanged(string value)
    {
        _settingBean.Config.SearchCacheFolderPath = value;
    }

    // 26. 搜索缓存最大占用（MB）输入文本
    [ObservableProperty] private string _searchCacheMaxSizeMbText;

    // 27. 搜索缓存大小输入错误提示
    [ObservableProperty] private string _searchCacheSizeError;

    partial void OnSearchCacheMaxSizeMbTextChanged(string value)
    {
        if (!int.TryParse(value, out var size))
        {
            SearchCacheSizeError = LocalizationService.Get("Status.Settings.CacheNumberInvalid");
            return;
        }

        if (size < 1 || size > 2048)
        {
            SearchCacheSizeError = LocalizationService.Get("Status.Settings.CacheRangeInvalid");
            return;
        }

        SearchCacheSizeError = string.Empty;
        _settingBean.Config.SearchCacheMaxSizeMb = size;
    }
    
    private SettingBean _settingBean;

    public SettingParamViewModel()
    {
        foreach (var language in Languages)
        {
            LocalizationService.Instance.PropertyChanged += (_, _) => language.RefreshDisplayName();
        }
    }

    public void Bind(SettingBean settingBean)
    {
        _settingBean = settingBean;
        SelectedLanguage = Languages.First(language => language.Code == settingBean.Config.Language);
        NetworkClientFactory.Configure(_settingBean.Config);

        SelectedFileConflictStrategyItem = FileConflictStrategies.First(item => Equals(item.Value, _settingBean.Config.FileConflictStrategy));
        IsConflictSuffixEnabled = _settingBean.Config.FileConflictStrategy == FileConflictStrategyEnum.APPEND_NUMBER;
        FileConflictSuffixPattern = string.IsNullOrWhiteSpace(_settingBean.Config.FileConflictSuffixPattern)
            ? " ({n})"
            : _settingBean.Config.FileConflictSuffixPattern;
        SelectedNetworkProxyModeItem = NetworkProxyModes.First(item => Equals(item.Value, _settingBean.Config.NetworkProxyMode));
        ProxyHost = _settingBean.Config.ProxyHost;
        ProxyPortText = (_settingBean.Config.ProxyPort <= 0 ? 80 : _settingBean.Config.ProxyPort).ToString();
        ProxyPortError = string.Empty;
        ProxyUsername = _settingBean.Config.ProxyUsername;
        ProxyPassword = _settingBean.Config.ProxyPassword;
        SelectedDotTypeItem = DotTypes.First(item => Equals(item.Value, _settingBean.Config.DotType));
        SelectedTransLyricLostRuleItem = TransLyricLostRules.First(item => Equals(item.Value, _settingBean.Config.TransConfig.LostRule));
        SelectedChineseProcessRuleItem = ChineseProcessRules.First(item => Equals(item.Value, _settingBean.Config.ChineseProcessRule));
        SelectedThemeModeRuleItem = ThemeModeRules.First(item => Equals(item.Value, _settingBean.Config.ThemeMode));
        LrcTimestampFormat = _settingBean.Config.LrcTimestampFormat;
        SrtTimestampFormat = _settingBean.Config.SrtTimestampFormat;
        IgnoreEmptyLyric = _settingBean.Config.IgnoreEmptyLyric;
        SelectedVerbatimLyricModeItem = VerbatimLyricMode.First(item => Equals(item.Value, _settingBean.Config.VerbatimLyricMode));
        MatchPrecisionDeviation = _settingBean.Config.TransConfig.MatchPrecisionDeviation;
        BaiduAppId = _settingBean.Config.TransConfig.BaiduTranslateAppId;
        BaiduSecret =  _settingBean.Config.TransConfig.BaiduTranslateSecret;
        CaiYunToken = _settingBean.Config.TransConfig.CaiYunToken;
        IgnorePureMusicInSave = _settingBean.Config.IgnorePureMusicInSave;
        SeparateFileForIsolated = _settingBean.Config.SeparateFileForIsolated;
        OutputFileNameFormat = _settingBean.Config.OutputFileNameFormat;
        AggregatedBlurSearch = _settingBean.Config.AggregatedBlurSearch;
        AutoReadClipboard = _settingBean.Config.AutoReadClipboard;
        AutoCheckUpdate = _settingBean.Config.AutoCheckUpdate;
        ConfirmBeforeExit = _settingBean.Config.ConfirmBeforeExit;
        QqMusicCookie = _settingBean.Config.QQMusicCookie;
        NetEaseCookie = _settingBean.Config.NetEaseCookie;
        SingerSeparator = _settingBean.Config.SingerSeparator;
        SingerCountLimit = _settingBean.Config.SingerCountLimit;
        LrcMergeSeparator = _settingBean.Param.LrcMergeSeparator;
        SaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        DownloadCoverAndSongLinkOnSave = _settingBean.Config.DownloadCoverAndSongLinkOnSave;
        SearchCacheFolderPath = _settingBean.Config.SearchCacheFolderPath;
        SearchCacheMaxSizeMbText = (_settingBean.Config.SearchCacheMaxSizeMb <= 0 ? 128 : _settingBean.Config.SearchCacheMaxSizeMb).ToString();
        SearchCacheSizeError = string.Empty;
    }
}

