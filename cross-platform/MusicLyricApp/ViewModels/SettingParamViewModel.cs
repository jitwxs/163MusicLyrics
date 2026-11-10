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
    public sealed class LanguageOption(string code, string resourceKey) : LocalizedObject
    {
        public string Code { get; } = code;
        public string DisplayName => LocalizationService.Get(resourceKey);
        public override string ToString() => DisplayName;
    }

    public ObservableCollection<LanguageOption> Languages { get; } =
    [
        new("zh-CN", "Language.SimplifiedChinese"),
        new("zh-TW", "Language.TraditionalChinese"),
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
    public EnumChoice<FileConflictStrategyEnum> FileConflictStrategy { get; }

    [ObservableProperty]
    private bool _isConflictSuffixEnabled;

    // 0. 网络代理模式
    public EnumChoice<NetworkProxyModeEnum> NetworkProxyMode { get; }

    public bool IsHttpProxySelected => NetworkProxyMode.Value == NetworkProxyModeEnum.HTTP_PROXY;

    // 0.1 HTTP 代理地址
    [ObservableProperty] private string _proxyHost = string.Empty;

    partial void OnProxyHostChanged(string value)
    {
        _settingBean.Config.ProxyHost = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 0.2 HTTP 代理端口
    public NumberSetting ProxyPort { get; }

    // 0.3 HTTP 代理用户名
    [ObservableProperty] private string _proxyUsername = string.Empty;

    partial void OnProxyUsernameChanged(string value)
    {
        _settingBean.Config.ProxyUsername = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 0.4 HTTP 代理密码
    [ObservableProperty] private string _proxyPassword = string.Empty;

    partial void OnProxyPasswordChanged(string value)
    {
        _settingBean.Config.ProxyPassword = value;
        if (IsHttpProxySelected)
        {
            NetworkClientFactory.Configure(_settingBean.Config);
        }
    }

    // 1. 毫秒截位规则
    public EnumChoice<DotTypeEnum> DotType { get; }

    // 2. 译文缺省规则
    public EnumChoice<TransLyricLostRuleEnum> TransLyricLostRule { get; }

    // 3. 中文处理策略
    public EnumChoice<ChineseProcessRuleEnum> ChineseProcessRule { get; }

    // 4. 主题
    public EnumChoice<ThemeModeEnum> ThemeMode { get; }

    // 5, 6. LRC and SRT timestamp formats
    public TimestampFormatEditorViewModel LrcTimestamp { get; }

    public TimestampFormatEditorViewModel SrtTimestamp { get; }

    /// <summary>The rounding rule only matters when a format keeps one or two millisecond digits (S, SS).</summary>
    public bool IsDotTypeApplicable =>
        TimestampFormatValidator.MillisecondDigits(_settingBean.Config.LrcTimestampFormat) < 3 ||
        TimestampFormatValidator.MillisecondDigits(_settingBean.Config.SrtTimestampFormat) < 3;

    public string DotTypeHint => LocalizationService.Get(IsDotTypeApplicable
        ? "Settings.Timestamp.Rounding.Applies"
        : "Settings.Timestamp.Rounding.Inactive");

    private void OnTimestampFormatCommitted()
    {
        OnPropertyChanged(nameof(IsDotTypeApplicable));
        OnPropertyChanged(nameof(DotTypeHint));
    }
    
    // 7. SRT 时间戳
    [ObservableProperty] private bool _ignoreEmptyLyric;
    
    partial void OnIgnoreEmptyLyricChanged(bool value)
    {
        _settingBean.Config.IgnoreEmptyLyric = value;
    }
    
    // 8. 逐字歌词
    public EnumChoice<VerbatimLyricModeEnum> VerbatimLyricMode { get; }

    // 9. 译文匹配精度
    [ObservableProperty] private int _matchPrecisionDeviation;
    
    partial void OnMatchPrecisionDeviationChanged(int value)
    {
        _settingBean.Config.TransConfig.MatchPrecisionDeviation = value;
    }

    // 10. 百度 APP ID
    [ObservableProperty] private string _baiduAppId = string.Empty;
    
    partial void OnBaiduAppIdChanged(string value)
    {
        _settingBean.Config.TransConfig.BaiduTranslateAppId = value;
    }
    
    // 11. 百度密钥
    [ObservableProperty] private string _baiduSecret = string.Empty;
    
    partial void OnBaiduSecretChanged(string value)
    {
        _settingBean.Config.TransConfig.BaiduTranslateSecret = value;
    }
    
    // 12. 彩云小译 Token
    [ObservableProperty] private string _caiYunToken = string.Empty;
    
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
    [ObservableProperty] private string _outputFileNameFormat = string.Empty;
    
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
    [ObservableProperty] private string _qqMusicCookie = string.Empty;
    
    partial void OnQqMusicCookieChanged(string value)
    {
        // Saved without the quotes, "Cookie:" prefix and line breaks a copy from the browser often brings.
        _settingBean.Config.QQMusicCookie = Core.Service.Login.CookieInspector.Normalize(value);
    }
    
    // 20. 网易云 Cookie
    [ObservableProperty] private string _netEaseCookie = string.Empty;
    
    partial void OnNetEaseCookieChanged(string value)
    {
        _settingBean.Config.NetEaseCookie = Core.Service.Login.CookieInspector.Normalize(value);
    }
    
    // 21. 歌手分隔符
    [ObservableProperty] private string _singerSeparator = string.Empty;
    
    partial void OnSingerSeparatorChanged(string value)
    {
        _settingBean.Config.SingerSeparator = value;
    }

    // 21.3 同名文件后缀模板
    [ObservableProperty] private string _fileConflictSuffixPattern = string.Empty;

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

    [ObservableProperty] private bool _enableFileLogging;

    partial void OnEnableFileLoggingChanged(bool value)
    {
        _settingBean.Config.EnableFileLogging = value;
        LoggingService.ConfigureFileLogging(value);
    }

    // 21.1 文件名歌手数上限（-1 不截断）
    [ObservableProperty] private int _singerCountLimit;

    partial void OnSingerCountLimitChanged(int value)
    {
        _settingBean.Config.SingerCountLimit = value < -1 ? -1 : value;
    }

    // 22. 歌词合并符
    [ObservableProperty] private string _lrcMergeSeparator = string.Empty;

    partial void OnLrcMergeSeparatorChanged(string value)
    {
        _settingBean.Param.LrcMergeSeparator = value;
    }

    // 23. 保存路径
    [ObservableProperty] private string _saveFolderPath = string.Empty;

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
    [ObservableProperty] private string _searchCacheFolderPath = string.Empty;

    partial void OnSearchCacheFolderPathChanged(string value)
    {
        _settingBean.Config.SearchCacheFolderPath = value;
    }

    // 26. 搜索缓存最大占用（MB）
    public NumberSetting SearchCacheMaxSizeMb { get; }

    // 28. 启动时加载到下载管理的缓存歌曲数量
    public NumberSetting SearchCacheStartupLoadLimit { get; }

    private SettingBean _settingBean = new();

    public SettingParamViewModel()
    {
        var config = () => _settingBean.Config;
        FileConflictStrategy = new(value =>
        {
            config().FileConflictStrategy = value;
            IsConflictSuffixEnabled = value == FileConflictStrategyEnum.APPEND_NUMBER;
        });
        NetworkProxyMode = new(value =>
        {
            config().NetworkProxyMode = value;
            OnPropertyChanged(nameof(IsHttpProxySelected));
            NetworkClientFactory.Configure(config());
        });
        ProxyPort = new(1, 65535, () => LocalizationService.Get("Message.InvalidPort"), port =>
        {
            config().ProxyPort = port;
            if (IsHttpProxySelected) NetworkClientFactory.Configure(config());
        });
        DotType = new(value =>
        {
            config().DotType = value;
            LrcTimestamp?.RefreshPreview();
            SrtTimestamp?.RefreshPreview();
        });
        TransLyricLostRule = new(value => config().TransConfig.LostRule = value);
        ChineseProcessRule = new(value => config().ChineseProcessRule = value);
        ThemeMode = new(value => config().ThemeMode = value);
        VerbatimLyricMode = new(value => config().VerbatimLyricMode = value);
        SearchCacheMaxSizeMb = new(1, 2048, () => LocalizationService.Get("Status.Settings.CacheRangeInvalid"),
            size => config().SearchCacheMaxSizeMb = size,
            notANumber: () => LocalizationService.Get("Status.Settings.CacheNumberInvalid"));
        SearchCacheStartupLoadLimit = new(0, Constants.MaxStartupCacheLoadLimit,
            () => LocalizationService.Format("Status.Settings.StartupLoadRangeInvalid", Constants.MaxStartupCacheLoadLimit),
            limit => config().SearchCacheStartupLoadLimit = limit);
        LrcTimestamp = new TimestampFormatEditorViewModel(true, () => _settingBean.Config.DotType, format =>
        {
            _settingBean.Config.LrcTimestampFormat = format;
            OnTimestampFormatCommitted();
        });
        SrtTimestamp = new TimestampFormatEditorViewModel(false, () => _settingBean.Config.DotType, format =>
        {
            _settingBean.Config.SrtTimestampFormat = format;
            OnTimestampFormatCommitted();
        });
        LocalizationService.Observe(this, static vm => vm.OnPropertyChanged(nameof(DotTypeHint)));

    }

    public void Bind(SettingBean settingBean)
    {
        _settingBean = settingBean;
        SelectedLanguage = Languages.First(language => language.Code == LocalizationService.NormalizeLanguage(settingBean.Config.Language));
        NetworkClientFactory.Configure(_settingBean.Config);

        FileConflictStrategy.Value = _settingBean.Config.FileConflictStrategy;
        IsConflictSuffixEnabled = _settingBean.Config.FileConflictStrategy == FileConflictStrategyEnum.APPEND_NUMBER;
        FileConflictSuffixPattern = string.IsNullOrWhiteSpace(_settingBean.Config.FileConflictSuffixPattern)
            ? " ({n})"
            : _settingBean.Config.FileConflictSuffixPattern;
        NetworkProxyMode.Value = _settingBean.Config.NetworkProxyMode;
        ProxyHost = _settingBean.Config.ProxyHost;
        ProxyPort.Load(_settingBean.Config.ProxyPort <= 0 ? 80 : _settingBean.Config.ProxyPort);
        ProxyUsername = _settingBean.Config.ProxyUsername;
        ProxyPassword = _settingBean.Config.ProxyPassword;
        DotType.Value = _settingBean.Config.DotType;
        TransLyricLostRule.Value = _settingBean.Config.TransConfig.LostRule;
        ChineseProcessRule.Value = _settingBean.Config.ChineseProcessRule;
        ThemeMode.Value = _settingBean.Config.ThemeMode;
        LrcTimestamp.Load(_settingBean.Config.LrcTimestampFormat);
        SrtTimestamp.Load(_settingBean.Config.SrtTimestampFormat);
        OnTimestampFormatCommitted();
        IgnoreEmptyLyric = _settingBean.Config.IgnoreEmptyLyric;
        VerbatimLyricMode.Value = _settingBean.Config.VerbatimLyricMode;
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
        EnableFileLogging = _settingBean.Config.EnableFileLogging;
        QqMusicCookie = _settingBean.Config.QQMusicCookie;
        NetEaseCookie = _settingBean.Config.NetEaseCookie;
        SingerSeparator = _settingBean.Config.SingerSeparator;
        SingerCountLimit = _settingBean.Config.SingerCountLimit;
        LrcMergeSeparator = _settingBean.Param.LrcMergeSeparator;
        SaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        DownloadCoverAndSongLinkOnSave = _settingBean.Config.DownloadCoverAndSongLinkOnSave;
        SearchCacheFolderPath = _settingBean.Config.SearchCacheFolderPath;
        SearchCacheMaxSizeMb.Load(_settingBean.Config.SearchCacheMaxSizeMb <= 0 ? 128 : _settingBean.Config.SearchCacheMaxSizeMb);
        SearchCacheStartupLoadLimit.Load(_settingBean.Config.SearchCacheStartupLoadLimit);
    }
}

