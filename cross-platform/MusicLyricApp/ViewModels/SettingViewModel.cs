using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using NLog;

namespace MusicLyricApp.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    // ---- Sections, listed under "settings" in the main sidebar ----
    public const string SectionLyrics = "lyrics";
    public const string SectionExtensions = "extensions";
    public const string SectionNetwork = "network";
    public const string SectionDownload = "download";
    public const string SectionApplication = "application";
    public const string SectionShortcuts = "shortcuts";

    public static readonly IReadOnlyList<string> Sections =
        [SectionLyrics, SectionExtensions, SectionNetwork, SectionDownload, SectionApplication, SectionShortcuts];

    /// <summary>The section the settings page shows; the main sidebar picks it.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLyricsSection), nameof(IsExtensionsSection), nameof(IsNetworkSection),
        nameof(IsDownloadSection), nameof(IsApplicationSection), nameof(IsShortcutsSection))]
    private string _currentSection = SectionLyrics;

    public bool IsLyricsSection => CurrentSection == SectionLyrics;
    public bool IsExtensionsSection => CurrentSection == SectionExtensions;
    public bool IsNetworkSection => CurrentSection == SectionNetwork;
    public bool IsDownloadSection => CurrentSection == SectionDownload;
    public bool IsApplicationSection => CurrentSection == SectionApplication;
    public bool IsShortcutsSection => CurrentSection == SectionShortcuts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSettingTips), nameof(IsTipsOpen))]
    private string _settingTips = LocalizationService.Get("Status.Settings.DefaultHelp");

    /// <summary>Two-way open state of the tips alert; closing it clears the tip until the next one.</summary>
    public bool IsTipsOpen
    {
        get => HasSettingTips;
        set
        {
            if (!value && HasSettingTips)
            {
                SettingTips = LocalizationService.Get("Status.Settings.DefaultHelp");
            }
        }
    }

    /// <summary>True once a command has put real help or a result in <see cref="SettingTips"/>, not the placeholder.</summary>
    public bool HasSettingTips => !string.IsNullOrWhiteSpace(SettingTips) &&
                                  SettingTips != LocalizationService.Get("Status.Settings.DefaultHelp");

    public ObservableCollection<LyricsTypeEnumModel> LyricsTypes { get; } = [];

    [ObservableProperty] private LyricsTypeEnumModel? _selectedLyricsTypeItem;
    
    [ObservableProperty] private string _configPath = Constants.GetConfigFilePath();

    /// <summary>
    /// The cookie field to scroll to and focus, set when the online lyrics page's sign-in state is clicked; the view
    /// takes it and clears it.
    /// </summary>
    [ObservableProperty] private SearchSourceEnum? _pendingCookieFocus;

    public void RequestCookieFocus(SearchSourceEnum source)
    {
        PendingCookieFocus = null;
        PendingCookieFocus = LoginStatusViewModel.HasAccount(source) ? source : SearchSourceEnum.NET_EASE_MUSIC;
    }

    /// <summary>Folder the daily log files are written to; see <see cref="Constants.GetLogFolderPath"/>.</summary>
    public string LogFolderPath { get; }

    /// <summary>Asks before old logs are deleted, given what would go; set by the view, left unset in tests.</summary>
    public Func<LogUsage, Task<bool>>? ConfirmDeleteOldLogsAsync { get; set; }

    /// <summary>What the log folder holds, read when the page opens and after a clean-up.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LogUsageText))]
    [NotifyCanExecuteChangedFor(nameof(DeleteOldLogsCommand))]
    private LogUsage _logUsage;

    /// <summary>"共 3 个日志文件，占用 1.2 MB，其中旧日志 1.1 MB", or a note that there are none.</summary>
    public string LogUsageText => LogUsage.Files == 0
        ? LocalizationService.Get("Settings.LogUsage.None")
        : LogUsage.OldFiles == 0
            ? LocalizationService.Format("Settings.LogUsage", LogUsage.Files, LoggingService.FormatSize(LogUsage.Bytes))
            : LocalizationService.Format("Settings.LogUsage.WithOld", LogUsage.Files,
                LoggingService.FormatSize(LogUsage.Bytes), LoggingService.FormatSize(LogUsage.OldBytes));

    private bool CanDeleteOldLogs => LogUsage.OldFiles > 0;

    public SettingParamViewModel SettingParamViewModel { get; } = new();

    private readonly SettingBean _settingBean;
    private readonly IWindowProvider? _windowProvider;
    private readonly Action<string> _openDirectory;
    private readonly Action<string> _openLink;
    private readonly StorageService _storageService = new();
    private readonly UpdateCheckService _updateCheckService = new();
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Sign-in state of the cookies on the extensions section; null when the page is used on its own.</summary>
    public LoginStatusViewModel? Login { get; }

    public SettingViewModel(SettingBean settingBean, IWindowProvider? windowProvider = null,
        Action<string>? openDirectory = null, LoginStatusViewModel? login = null, string? logFolderPath = null,
        Action<string>? openLink = null)
    {
        _openLink = openLink ?? ShellLauncher.Open;
        _settingBean = settingBean;
        LogFolderPath = logFolderPath ?? Constants.GetLogFolderPath();
        RefreshLogUsage();
        LocalizationService.Observe(this, static vm => vm.OnPropertyChanged(nameof(LogUsageText)));
        _windowProvider = windowProvider;
        _openDirectory = openDirectory ?? ShellLauncher.Open;
        LocalSongCacheService.EnsureConfigDefaults(_settingBean);
        SettingParamViewModel.Bind(_settingBean);

        // Subscribed after Bind, so loading the saved cookies does not count as an edit.
        Login = login;
        if (Login != null)
        {
            SettingParamViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SettingParamViewModel.NetEaseCookie))
                    Login.ScheduleCheck(SearchSourceEnum.NET_EASE_MUSIC);
                else if (e.PropertyName == nameof(SettingParamViewModel.QqMusicCookie))
                    Login.ScheduleCheck(SearchSourceEnum.QQ_MUSIC);
            };
        }

        InitLyricsTypes();
    }

    private void InitLyricsTypes()
    {
        var selected = _settingBean.Config.DeserializationOutputLyricsTypes();

        foreach (var e in selected)
        {
            LyricsTypes.Add(new LyricsTypeEnumModel(e)
            {
                IsSelected = true
            });
        }

        foreach (var e in Enum.GetValues<LyricsTypeEnum>())
        {
            if (!selected.Contains(e))
            {
                LyricsTypes.Add(new LyricsTypeEnumModel(e));
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        var index = LyricsTypes.IndexOf(SelectedLyricsTypeItem!);
        if (index <= 0) return;

        LyricsTypes.Move(index, index - 1);
        ForceRefreshLyrics();
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        var index = LyricsTypes.IndexOf(SelectedLyricsTypeItem!);
        if (index >= LyricsTypes.Count - 1 || index < 0) return;

        LyricsTypes.Move(index, index + 1);
        ForceRefreshLyrics();
    }

    private bool CanMoveUp() => SelectedLyricsTypeItem != null && LyricsTypes.IndexOf(SelectedLyricsTypeItem) > 0;

    private bool CanMoveDown() => SelectedLyricsTypeItem != null &&
                                  LyricsTypes.IndexOf(SelectedLyricsTypeItem) < LyricsTypes.Count - 1;

    private void ForceRefreshLyrics()
    {
        var copy = new ObservableCollection<LyricsTypeEnumModel>(LyricsTypes);
        LyricsTypes.Clear();
        foreach (var item in copy)
            LyricsTypes.Add(item);
    }

    public void OnClosing()
    {
        LocalSongCacheService.EnsureConfigDefaults(_settingBean);
        _settingBean.Config.OutputLyricTypes = string.Join(",", LyricsTypes
            .Where(x => x.IsSelected)
            .Select(x => x.Id));
    }

    [RelayCommand]
    private void OutputTips()
    {
        SettingTips = Constants.HelpTips.GetContent(Constants.HelpTips.TypeEnum.OUTPUT_SETTING);
    }

    [RelayCommand]
    private void OpenConfigPath()
    {
        if (!File.Exists(ConfigPath)) return;
        
        var folder = Path.GetDirectoryName(ConfigPath);
        if (folder == null) return;
        
        try
        {
            _openDirectory(folder);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenConfigPath error");
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);
        }
    }

    /// <summary>Opens the source's website in the browser, to sign in there and copy the cookie.</summary>
    [RelayCommand]
    private void OpenSourceWebsite(SearchSourceEnum source)
    {
        if (MusicSources.Get(source).WebUrl is not { } url) return;

        try
        {
            _openLink(url);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenSourceWebsite error");
            SettingTips = LocalizationService.Format("Login.OpenWebsiteFailed", url);
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            // The logger only creates the folder on its first write, so it may not exist yet.
            Directory.CreateDirectory(LogFolderPath);
            _openDirectory(LogFolderPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenLogFolder error");
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);
        }
    }

    public void RefreshLogUsage() => LogUsage = LoggingService.GetLogUsage(LogFolderPath, DateTime.Now);

    [RelayCommand(CanExecute = nameof(CanDeleteOldLogs))]
    private async Task DeleteOldLogsAsync()
    {
        RefreshLogUsage();
        if (LogUsage.OldFiles == 0)
        {
            SettingTips = LocalizationService.Get("Status.Settings.NoOldLogs");
            return;
        }

        if (ConfirmDeleteOldLogsAsync != null && !await ConfirmDeleteOldLogsAsync(LogUsage))
        {
            return;
        }

        var (files, bytes) = LoggingService.DeleteOldLogs(LogFolderPath, DateTime.Now);
        RefreshLogUsage();
        SettingTips = files > 0
            ? LocalizationService.Format("Status.Settings.OldLogsDeleted", files, LoggingService.FormatSize(bytes))
            : LocalizationService.Get("Status.Settings.NoOldLogs");
    }

    [RelayCommand]
    private async Task TestProxyAsync()
    {
        if (SettingParamViewModel.NetworkProxyMode.Selected?.Value != NetworkProxyModeEnum.HTTP_PROXY)
        {
            SettingTips = LocalizationService.Get("Status.Settings.ProxyModeRequired");
            return;
        }

        if (!string.IsNullOrWhiteSpace(SettingParamViewModel.ProxyPort.Error))
        {
            SettingTips = SettingParamViewModel.ProxyPort.Error;
            return;
        }

        if (string.IsNullOrWhiteSpace(_settingBean.Config.ProxyHost) || _settingBean.Config.ProxyPort <= 0)
        {
            SettingTips = LocalizationService.Get("Status.Settings.ProxyInvalid");
            return;
        }

        SettingTips = LocalizationService.Get("Status.Settings.ProxyTesting");
        try
        {
            NetworkClientFactory.Configure(_settingBean.Config);
            using var client = NetworkClientFactory.CreateHttpClient(8);
            using var resp = await client.GetAsync("https://c.y.qq.com/");
            SettingTips = resp.IsSuccessStatusCode
                ? LocalizationService.Get("Status.Settings.ProxySuccess")
                : LocalizationService.Format("Status.Settings.ProxyHttpFailed", (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            SettingTips = LocalizationService.Format("Status.Settings.ProxyFailed", ex.Message);
        }
    }

    [RelayCommand]
    private void ApplyProxy()
    {
        NetworkClientFactory.Configure(_settingBean.Config);
        _storageService.SaveConfig(_settingBean);
        SettingTips = LocalizationService.Get("Status.Settings.ProxyApplied");
    }

    [RelayCommand]
    private async Task SelectSaveFolderAsync()
    {
        if (_windowProvider == null)
        {
            return;
        }

        try
        {
            var localPath = (await _storageService.SelectFolderAndRememberAsync(_settingBean, _windowProvider)).LocalPath();
            if (localPath == null)
            {
                return;
            }

            SettingParamViewModel.SaveFolderPath = localPath;
            SettingTips = LocalizationService.Format("Status.Settings.OutputPathUpdated", localPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "SelectSaveFolder error");
            SettingTips = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenSaveFolder()
    {
        var path = SettingParamViewModel.SaveFolderPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            SettingTips = LocalizationService.Get("Status.Settings.OutputPathEmpty");
            return;
        }

        try
        {
            Directory.CreateDirectory(path);
            _openDirectory(path);
            SettingTips = LocalizationService.Format("Status.Settings.OutputPathOpened", path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenSaveFolder error");
            SettingTips = LocalizationService.Format("Status.Settings.OpenOutputPathFailed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SelectSearchCacheFolderAsync()
    {
        if (_windowProvider == null)
        {
            return;
        }

        try
        {
            var path = (await _windowProvider.PickFolderAsync("Dialog.Folder.Cache")).LocalPath();
            if (path == null)
            {
                return;
            }

            SettingParamViewModel.SearchCacheFolderPath = path;
            _storageService.SaveConfig(_settingBean);
            SettingTips = LocalizationService.Format("Status.Settings.CachePathUpdated", path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "SelectSearchCacheFolder error");
            SettingTips = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            if (!await _updateCheckService.OfferUpdateAsync())
            {
                SettingTips = ErrorMsgConst.THIS_IS_LATEST_VERSION;
            }
        }
        catch (Exception ex)
        {
            SettingTips = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenSearchCachePath()
    {
        var path = SettingParamViewModel.SearchCacheFolderPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(path);
            _openDirectory(path);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenSearchCachePath error");
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);
        }
    }
}
