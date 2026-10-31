using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NLog;

namespace MusicLyricApp.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    [ObservableProperty] private string _settingTips = LocalizationService.Get("Status.Settings.DefaultHelp");

    public ObservableCollection<LyricsTypeEnumModel> LyricsTypes { get; } = [];

    [ObservableProperty] private LyricsTypeEnumModel? _selectedLyricsTypeItem;
    
    [ObservableProperty] private string _configPath = Constants.GetConfigFilePath();

    public SettingParamViewModel SettingParamViewModel { get; } = new();

    private readonly SettingBean _settingBean;
    private readonly IWindowProvider? _windowProvider;
    private readonly StorageService _storageService = new();
    private readonly UpdateCheckService _updateCheckService = new();
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SettingViewModel(SettingBean settingBean, IWindowProvider? windowProvider = null)
    {
        _settingBean = settingBean;
        _windowProvider = windowProvider;
        LocalSongCacheService.EnsureConfigDefaults(_settingBean);
        SettingParamViewModel.Bind(_settingBean);

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
    private void TimestampTips()
    {
        SettingTips = Constants.HelpTips.GetContent(Constants.HelpTips.TypeEnum.TIME_STAMP_SETTING);
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
            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenConfigPath error");
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);
        }
    }

    [RelayCommand]
    private async Task TestProxyAsync()
    {
        if (SettingParamViewModel.SelectedNetworkProxyModeItem?.Value != NetworkProxyModeEnum.HTTP_PROXY)
        {
            SettingTips = LocalizationService.Get("Status.Settings.ProxyModeRequired");
            return;
        }

        if (!string.IsNullOrWhiteSpace(SettingParamViewModel.ProxyPortError))
        {
            SettingTips = SettingParamViewModel.ProxyPortError;
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

        var folder = await _storageService.SelectFolderAndRememberAsync(_settingBean, _windowProvider);
        var localPath = folder.Path?.LocalPath ?? folder.Path?.ToString() ?? "";
        SettingParamViewModel.SaveFolderPath = localPath;
        SettingTips = LocalizationService.Format("Status.Settings.OutputPathUpdated", localPath);
    }

    [RelayCommand]
    private async Task SelectSearchCacheFolderAsync()
    {
        if (_windowProvider == null)
        {
            return;
        }

        var folders = await _windowProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationService.Get("Dialog.Folder.Cache"),
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].Path?.LocalPath ?? folders[0].Path?.ToString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        SettingParamViewModel.SearchCacheFolderPath = path;
        _storageService.SaveConfig(_settingBean);
        SettingTips = LocalizationService.Format("Status.Settings.CachePathUpdated", path);
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            var checkResult = _updateCheckService.CheckLatestVersion(GetCurrentVersionTitle());
            if (checkResult.HasUpdate)
            {
                var content = UpdateCheckService.BuildReleaseDescription(checkResult.Info);
                var box = MessageBoxManager.GetMessageBoxStandard(LocalizationService.Get("Dialog.ReleaseNotes.Title"), content, ButtonEnum.YesNo);
                var result = await box.ShowWindowAsync();
                if (result == ButtonResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = UpdateCheckService.ReleasePageUrl,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                SettingTips = ErrorMsgConst.THIS_IS_LATEST_VERSION;
            }
        }
        catch (Exception ex)
        {
            SettingTips = ex.Message;
        }
    }

    private static string GetCurrentVersionTitle()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null &&
            !string.IsNullOrWhiteSpace(desktop.MainWindow.Title))
        {
            return desktop.MainWindow.Title;
        }

        return "MusicLyricApp v0.0";
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
            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenSearchCachePath error");
            throw new MusicLyricException(ErrorMsgConst.STORAGE_FOLDER_ERROR);
        }
    }
}
