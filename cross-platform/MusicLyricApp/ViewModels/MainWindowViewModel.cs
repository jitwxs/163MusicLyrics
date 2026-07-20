using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NAudio.Wave;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels.Messages;
using MusicLyricApp.Views;

namespace MusicLyricApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public bool IsPlaybackSupported => OperatingSystem.IsWindows();
    public SearchParamViewModel SearchParamViewModel { get; } = new();

    public SearchResultViewModel SearchResultViewModel { get; } = new();
    
    public SignalLampViewModel LampVm { get; } = new();

    [ObservableProperty] private string _appTitle = "MusicLyricApp v7.3 Batch";

    [ObservableProperty] private string _lastSaveFolderPath = "";
    [ObservableProperty] private string _tipTimestamp = "";
    [ObservableProperty] private string _tipNormalMessage = "";
    [ObservableProperty] private string _tipErrorMessage = "";
    [ObservableProperty] private bool _isBlurSearch;
    [ObservableProperty] private bool _hasSongLink;
    [ObservableProperty] private bool _isPlayingSong;
    [ObservableProperty] private double _playbackPositionSeconds;
    [ObservableProperty] private double _playbackDurationSeconds;

    public string PlaybackProgressText => $"{FormatPlaybackTime(PlaybackPositionSeconds)} / {FormatPlaybackTime(PlaybackDurationSeconds)}";
    public string TipFullMessage => $"{(!string.IsNullOrWhiteSpace(TipErrorMessage) ? TipErrorMessage : TipNormalMessage)}";

    private readonly SearchService _searchService;

    private readonly DispatcherTimer _playbackTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private IWavePlayer? _waveOut;
    private MediaFoundationReader? _audioReader;
    private string _currentPlayingLink = string.Empty;
    private bool _updatingPlaybackPosition;

    private readonly StorageService _storageService = new();
    private readonly UpdateCheckService _updateCheckService = new();
        
    private readonly IWindowProvider _windowProvider;
    
    private readonly BatchSearchViewModel _downloadManagerViewModel;

    [ObservableProperty] private SettingBean _settingBean;
    
    private SettingWindow? _settingWindow;
    
    private BlurSearchWindow? _blurSearchWindow;
    
    private BatchSearchWindow? _batchSearchWindow;

    private FormatConvertWindow? _formatConvertWindow;

    // Parameterless constructor for design-time use
    public MainWindowViewModel()
    {
        _settingBean = new StorageService().ReadAppConfig();
        NetworkClientFactory.Configure(_settingBean.Config);
        _searchService = new SearchService(_settingBean);
        _windowProvider = null;
        _downloadManagerViewModel = new BatchSearchViewModel(_settingBean, _searchService, new StorageService(), null!);
        
        SearchParamViewModel.Bind(_settingBean.Param);
        LastSaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        SearchParamViewModel.PropertyChanged += SearchParamViewModelOnPropertyChanged;

        SearchResultViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchResultViewModel.SongLink))
            {
                RefreshSongLinkState();
            }
        };

        _playbackTimer.Tick += (_, _) => UpdatePlaybackProgress();
    }

    // Main constructor for runtime use
    public MainWindowViewModel(IWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;

        _settingBean = _storageService.ReadAppConfig();
        NetworkClientFactory.Configure(_settingBean.Config);

        _searchService = new SearchService(_settingBean);
        
        _storageService.SetSearchService(_searchService);
        _downloadManagerViewModel = new BatchSearchViewModel(_settingBean, _searchService, _storageService, _windowProvider);

        SearchParamViewModel.Bind(_settingBean.Param);
        LastSaveFolderPath = _settingBean.Config.LastSaveFolderPath;
        SearchParamViewModel.PropertyChanged += SearchParamViewModelOnPropertyChanged;

        SearchResultViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchResultViewModel.SongLink))
            {
                RefreshSongLinkState();
            }
        };

        _playbackTimer.Tick += (_, _) => UpdatePlaybackProgress();
        
        WeakReferenceMessenger.Default.Register<BlurSearchResultsMessage>(this, (r, m) =>
        {
            _ = ProcessBlurSearchResults(m.Value);
        });
        
        WeakReferenceMessenger.Default.Register<CloseWindowMessage>(this, (r, m) =>
        {
            if (m.Value == "BlurSearchWindow" && _blurSearchWindow != null)
            {
                _blurSearchWindow.Close();
                _blurSearchWindow = null;
            }
        });

        WeakReferenceMessenger.Default.Register<OpenSongDetailMessage>(this, (r, m) =>
        {
            _ = OpenSongDetailAsync(m.Value);
        });
        
        UpdateTheme();
        
        if (_settingBean.Config.AutoCheckUpdate)
        {
            ThreadPool.QueueUserWorkItem(p => _ = DoVersionCheck(false));
        }
    }

    private async Task ProcessBlurSearchResults(string ids)
    {
        var selectedSongIds = new List<string>();
        try
        {
            var parseParam = new SearchParamViewModel();
            parseParam.Bind(SettingBean.Param);
            parseParam.SearchText = ids;
            _searchService.InitSongIds(parseParam, SettingBean);
            selectedSongIds = parseParam.SongIds.Select(x => x.SongId).Distinct().ToList();
        }
        catch
        {
            // Ignore parse failure here and keep original behavior.
        }

        IsBlurSearch = false;
        SearchParamViewModel.SearchText = ids;
        await ExecuteSearchAsync();

        if (selectedSongIds.Count > 1)
        {
            _downloadManagerViewModel.SelectSongsByIds(selectedSongIds);
            ExecuteOpenDownloadManager();
        }
    }

    private async Task OpenSongDetailAsync(SongDetailRequest request)
    {
        IsBlurSearch = false;
        var sourceItem = SearchParamViewModel.SearchSources.FirstOrDefault(x => x.Value == request.SearchSource);
        if (sourceItem != null)
        {
            SearchParamViewModel.SelectedSearchSourceItem = sourceItem;
        }

        SearchParamViewModel.SearchText = request.SongId;

        ResetPlaybackAndSongLink();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }

                mainWindow.Activate();
            }
        }

        if (_downloadManagerViewModel.TryGetCachedSaveVo(request.SongId, out var saveVo))
        {
            var tempParam = new SearchParamViewModel();
            tempParam.Bind(SettingBean.Param);
            tempParam.SongIds.Clear();
            var rawInput = new InputSongId(request.SongId, request.SearchSource, SearchTypeEnum.SONG_ID);
            tempParam.SongIds.Add(new InputSongId(request.SongId, rawInput));

            var cachedResult = new Dictionary<string, ResultVo<SaveVo>>
            {
                [request.SongId] = new(saveVo)
            };

            await _searchService.RenderSearchResult(tempParam, SearchResultViewModel, SettingBean, cachedResult);

            if (_downloadManagerViewModel.TryGetCachedSongLink(request.SongId, out var cachedLink))
            {
                SearchResultViewModel.SongLink = cachedLink;
            }
            else
            {
                var musicApi = _searchService.GetMusicApi(request.SearchSource);
                var linkResult = await Task.Run(() => musicApi.GetSongLink(request.SongId));
                if (linkResult.IsSuccess())
                {
                    SearchResultViewModel.SongLink = linkResult.Data;
                    _downloadManagerViewModel.CacheSongLink(request.SongId, linkResult.Data);
                }
            }

            SetTip("已从下载管理缓存加载歌曲详情。", false);
            return;
        }

        await ExecuteSearchAsync();
    }

    public void SaveConfig()
    {
        _storageService.SaveConfig(SettingBean);
    }

    private void SearchParamViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SearchParamViewModel.SelectedOutputFormatItem))
        {
            return;
        }

        _ = RefreshCurrentConsoleOutputAsync();
    }

    private async Task RefreshCurrentConsoleOutputAsync()
    {
        if (SearchResultViewModel.SaveVoMap.Count != 1)
        {
            return;
        }

        var saveVo = SearchResultViewModel.SaveVoMap.Values.First();
        if (saveVo.LyricVo.IsEmpty())
        {
            SearchResultViewModel.ResetConsoleOutput(string.Empty);
            return;
        }

        var content = await LyricUtils.GetOutputContent(saveVo.LyricVo, SettingBean);
        SearchResultViewModel.ResetConsoleOutput(GlobalUtils.MergeStr(content));
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync()
    {
        if (IsBlurSearch)
        {
            await ExecuteBlurSearchAsync();
            return;
        }

        try
        {
            _searchService.InitSongIds(SearchParamViewModel, SettingBean);

            var songIds = SearchParamViewModel.SongIds;
            if (songIds.Count == 0)
            {
                throw new MusicLyricException(ErrorMsgConst.SEARCH_RESULT_EMPTY);
            }

            var result = _searchService.SearchSongs(SearchParamViewModel.SongIds, SettingBean);
            var localCacheHitCount = _searchService.LastLocalCacheHitCount;
            LampVm.UpdateLampInfo(result, SettingBean);
            _downloadManagerViewModel.AddSearchResults(result, SearchParamViewModel.SongIds, SearchParamViewModel.SearchText);

            if (SearchParamViewModel.SongIds.Count == 1)
            {
                ResetPlaybackAndSongLink();
                await _searchService.RenderSearchResult(SearchParamViewModel, SearchResultViewModel, SettingBean, result);
                if (SearchResultViewModel.ConsoleOutput == ErrorMsgConst.LRC_NOT_EXIST)
                {
                    SearchResultViewModel.ConsoleOutput = "";
                    SetTip(ErrorMsgConst.LRC_NOT_EXIST, true);
                }

                // 自动获取直链
                await Task.Run(() =>
                {
                    var songId0 = SearchParamViewModel.SongIds[0];
                    
                    var musicApi = _searchService.GetMusicApi(songId0.SearchSource);
                    var linkResult = musicApi.GetSongLink(songId0.SongId);
                    if (linkResult.IsSuccess())
                    {
                        SearchResultViewModel.SongLink = linkResult.Data;
                        _downloadManagerViewModel.CacheSongLink(songId0.SongId, linkResult.Data);
                    }
                });

                if (localCacheHitCount > 0)
                {
                    SetTip("命中本地缓存", false);
                }
            }
            else
            {
                ExecuteOpenDownloadManager();
                SearchResultViewModel.SaveVoMap.Clear();
                SearchResultViewModel.ResetConsoleOutput("");
                SetTip("已打开下载管理窗口。", false);
            }
        }
        catch (Exception ex)
        {
            SetTip(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task ExecuteExactSearchWithModeAsync()
    {
        IsBlurSearch = false;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ExecuteBlurSearchWithModeAsync()
    {
        IsBlurSearch = true;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task ExecuteBlurSearchAsync()
    {
        try
        {
            var resultVos = _searchService.BlurSearch(SearchParamViewModel, SettingBean);

            if (_blurSearchWindow != null)
            {
                if (!_blurSearchWindow.IsVisible)
                {
                    // 窗口已经被关闭或隐藏
                    _blurSearchWindow = null;
                }
                else
                {
                    _blurSearchWindow.UpdateResults(resultVos);

                    // 窗口还在：如果最小化，恢复正常；否则激活
                    if (_blurSearchWindow.WindowState == WindowState.Minimized)
                    {
                        _blurSearchWindow.WindowState = WindowState.Normal;
                    }

                    _blurSearchWindow.Activate();
                    SetTip($"模糊搜索成功，共 {resultVos.Count} 组结果。", false);
                    return;
                }
            }

            // 创建新窗口
            _blurSearchWindow = new BlurSearchWindow(resultVos);
            _blurSearchWindow.Closed += (_, _) => _blurSearchWindow = null;
            _blurSearchWindow.Show();
            SetTip($"模糊搜索成功，共 {resultVos.Count} 组结果。", false);
        }
        catch (Exception ex)
        {
            SetTip(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task ExecuteSaveAsync()
    {
        try
        {
            var message = await _storageService.SaveResult(SearchResultViewModel, SettingBean, _windowProvider);
            LastSaveFolderPath = SettingBean.Config.LastSaveFolderPath;
            SetTip(message, false);
        }
        catch (Exception ex)
        {
            if (ex.Message != ErrorMsgConst.STORAGE_FOLDER_ERROR)
            {
                SetTip(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task ExecuteSongLinkAsync()
    {
        try
        {
            var message = await _storageService.DownloadSongLink(SearchResultViewModel, SettingBean, _windowProvider);
            LastSaveFolderPath = SettingBean.Config.LastSaveFolderPath;
            SetTip(message, false);
        }
        catch (Exception ex)
        {
            SetTip(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task ExecuteSongPicAsync()
    {
        try
        {
            var message = await _storageService.DownloadSongPic(SearchResultViewModel, SettingBean, _windowProvider);
            LastSaveFolderPath = SettingBean.Config.LastSaveFolderPath;
            SetTip(message, false);
        }
        catch (Exception ex)
        {
            SetTip(ex.Message, true);
        }
    }

    private void RefreshSongLinkState()
    {
        HasSongLink = !string.IsNullOrWhiteSpace(SearchResultViewModel.SongLink);
        if (!HasSongLink)
        {
            StopPlaybackInternal(resetPosition: true, disposePlayer: true);
            return;
        }

        EnsurePlaybackPrepared();
        UpdatePlaybackProgress();
    }

    private void UpdatePlaybackProgress()
    {
        if (_audioReader == null || _updatingPlaybackPosition)
        {
            return;
        }

        _updatingPlaybackPosition = true;
        PlaybackDurationSeconds = Math.Max(0, _audioReader.TotalTime.TotalSeconds);
        PlaybackPositionSeconds = Math.Clamp(_audioReader.CurrentTime.TotalSeconds, 0, PlaybackDurationSeconds);
        _updatingPlaybackPosition = false;
    }

    partial void OnPlaybackPositionSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(PlaybackProgressText));

        if (_updatingPlaybackPosition)
        {
            return;
        }

        if (_audioReader == null && !EnsurePlaybackPrepared())
        {
            return;
        }

        var safeValue = Math.Clamp(value, 0, Math.Max(0, _audioReader.TotalTime.TotalSeconds));
        _audioReader.CurrentTime = TimeSpan.FromSeconds(safeValue);
    }

    partial void OnPlaybackDurationSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(PlaybackProgressText));
    }

    private static string FormatPlaybackTime(double seconds)
    {
        if (seconds <= 0)
        {
            return "00:00";
        }

        var ts = TimeSpan.FromSeconds(seconds);
        var totalMinutes = (int)ts.TotalMinutes;
        return $"{totalMinutes:00}:{ts.Seconds:00}";
    }

    [RelayCommand]
    private void ExecutePlaySong()
    {
        if (!IsPlaybackSupported)
        {
            SetTip("当前平台不支持内置播放器。", true);
            return;
        }

        if (!HasSongLink)
        {
            SetTip("\u5f53\u524d\u6b4c\u66f2\u6ca1\u6709\u53ef\u64ad\u653e\u7684\u76f4\u94fe\u3002", true);
            return;
        }

        try
        {
            if (!EnsurePlaybackPrepared())
            {
                return;
            }

            _waveOut?.Play();
            IsPlayingSong = true;
            _playbackTimer.Start();
            SetTip("\u5f00\u59cb\u64ad\u653e\u6b4c\u66f2\u3002", false);
        }
        catch (Exception ex)
        {
            StopPlaybackInternal(resetPosition: true, disposePlayer: true);
            SetTip($"\u64ad\u653e\u5931\u8d25\uff1a{ex.Message}", true);
        }
    }

    [RelayCommand]
    private void ExecuteStopSong()
    {
        if (_audioReader == null && _waveOut == null)
        {
            return;
        }

        StopPlaybackInternal(resetPosition: true, disposePlayer: true);
        SetTip("\u5df2\u505c\u6b62\u64ad\u653e\u3002", false);
    }

    private void StopPlaybackInternal(bool resetPosition, bool disposePlayer)
    {
        _playbackTimer.Stop();

        if (_waveOut != null)
        {
            _waveOut.Stop();
            if (disposePlayer)
            {
                _waveOut.Dispose();
                _waveOut = null;
            }
        }

        if (disposePlayer && _audioReader != null)
        {
            _audioReader.Dispose();
            _audioReader = null;
            _currentPlayingLink = string.Empty;
        }

        IsPlayingSong = false;

        if (resetPosition)
        {
            _updatingPlaybackPosition = true;
            PlaybackPositionSeconds = 0;
            PlaybackDurationSeconds = 0;
            _updatingPlaybackPosition = false;
        }
    }

    [RelayCommand]
    private void ExecuteTogglePlayPause()
    {
        if (!HasSongLink)
        {
            return;
        }

        if (_audioReader == null || _waveOut == null)
        {
            ExecutePlaySong();
            return;
        }

        if (IsPlayingSong)
        {
            _waveOut.Pause();
            _playbackTimer.Stop();
            IsPlayingSong = false;
            SetTip("已暂停播放。", false);
        }
        else
        {
            _waveOut.Play();
            _playbackTimer.Start();
            IsPlayingSong = true;
            SetTip("继续播放。", false);
        }
    }

    private void ResetPlaybackAndSongLink()
    {
        StopPlaybackInternal(resetPosition: true, disposePlayer: true);
        SearchResultViewModel.SongLink = "";
    }

    private bool EnsurePlaybackPrepared()
    {
        if (!IsPlaybackSupported)
        {
            SetTip("当前平台不支持内置播放器。", true);
            return false;
        }

        if (!HasSongLink)
        {
            return false;
        }

        var link = SearchResultViewModel.SongLink.Trim();
        if (_audioReader != null && _waveOut != null && _currentPlayingLink.Equals(link, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            StopPlaybackInternal(resetPosition: true, disposePlayer: true);
            _audioReader = new MediaFoundationReader(link);
            _waveOut = new WaveOutEvent();
            _waveOut.PlaybackStopped += (_, _) =>
            {
                _playbackTimer.Stop();
                IsPlayingSong = false;
                UpdatePlaybackProgress();
            };
            _waveOut.Init(_audioReader);
            _currentPlayingLink = link;
            PlaybackDurationSeconds = Math.Max(0, _audioReader.TotalTime.TotalSeconds);
            PlaybackPositionSeconds = Math.Clamp(PlaybackPositionSeconds, 0, PlaybackDurationSeconds);
            return true;
        }
        catch (Exception ex)
        {
            StopPlaybackInternal(resetPosition: true, disposePlayer: true);
            SetTip($"播放器初始化失败：{ex.Message}", true);
            return false;
        }
    }

    [RelayCommand]
    private static void ExecuteHome()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/jitwxs/163MusicLyrics",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void ExecuteFormatConvert()
    {
        if (_formatConvertWindow != null)
        {
            if (!_formatConvertWindow.IsVisible)
            {
                _formatConvertWindow = null;
            }
            else
            {
                if (_formatConvertWindow.WindowState == WindowState.Minimized)
                {
                    _formatConvertWindow.WindowState = WindowState.Normal;
                }

                _formatConvertWindow.Activate();
                return;
            }
        }

        _formatConvertWindow = new FormatConvertWindow(SettingBean);
        _formatConvertWindow.Closed += (_, _) => _formatConvertWindow = null;
        _formatConvertWindow.Show();
    }
    
    [RelayCommand]
    private static void ExecuteWiki()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/jitwxs/163MusicLyrics/wiki",
            UseShellExecute = true
        });
    }
    
    [RelayCommand]
    private static void ExecuteIssue()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/jitwxs/163MusicLyrics/issues",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task ExecuteShortCutAsync()
    {
        var sb = new StringBuilder();
        sb
            .Append("精确搜索: \t Enter").Append(Environment.NewLine)
            .Append("模糊搜索: \t Ctrl + Enter").Append(Environment.NewLine)
            .Append("保存结果: \t Ctrl + S").Append(Environment.NewLine);
        
        await DialogHelper.ShowMessage(sb.ToString());
    }

    [RelayCommand]
    private void ExecuteSetting()
    {
        if (_settingWindow != null)
        {
            if (!_settingWindow.IsVisible)
            {
                // 窗口已经被关闭或隐藏
                _settingWindow = null;
            }
            else
            {
                // 窗口还在：如果最小化，恢复正常；否则激活
                if (_settingWindow.WindowState == WindowState.Minimized)
                {
                    _settingWindow.WindowState = WindowState.Normal;
                }

                _settingWindow.Activate();
                return;
            }
        }

        // 创建新窗口
        _settingWindow = new SettingWindow(SettingBean, _windowProvider);
        _settingWindow.Closed += (_, _) => 
        {
            if (_settingWindow.DataContext is SettingViewModel vm)
            {
                vm.OnClosing();
                UpdateTheme();
            }
            _settingWindow = null;
        };
        _settingWindow.Show();
    }
    
    private bool _inCheckVersion;
    
    private bool _showMessageIfNotExistLatestVersion;
    
    private async Task DoVersionCheck(bool showMessageIfNotExistLatestVersion)
    {
        _showMessageIfNotExistLatestVersion = showMessageIfNotExistLatestVersion;
        
        if (_inCheckVersion)
        {
            return;
        }
        
        _inCheckVersion = true;
        
        try
        {
            var checkResult = _updateCheckService.CheckLatestVersion(AppTitle);
            if (checkResult.HasUpdate)
            {
                var content = UpdateCheckService.BuildReleaseDescription(checkResult.Info);
                
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("更新说明", content, ButtonEnum.YesNo);
                    var result = await box.ShowWindowAsync();
    
                    if (result == ButtonResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = UpdateCheckService.ReleasePageUrl,
                            UseShellExecute = true
                        });
                    }
                });
            }
            else if (_showMessageIfNotExistLatestVersion)
            {
                SetTip(ErrorMsgConst.THIS_IS_LATEST_VERSION, false);
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowMessage(ex);
        }
        finally
        {
            _inCheckVersion = false;
        }
    }

    private void UpdateTheme()
    {
        ((App)Application.Current!).SetTheme(SettingBean.Config.ThemeMode);
    }

    [RelayCommand]
    private async Task ExecuteSelectOutputFolderAsync()
    {
        try
        {
            await _storageService.SelectFolderAndRememberAsync(SettingBean, _windowProvider);
            LastSaveFolderPath = SettingBean.Config.LastSaveFolderPath;
            SetTip($"保存路径已更新：{LastSaveFolderPath}", false);
        }
        catch (Exception ex)
        {
            if (ex.Message != ErrorMsgConst.STORAGE_FOLDER_ERROR)
            {
                SetTip(ex.Message, true);
            }
        }
    }

    private void SetTip(string message, bool isError)
    {
        TipTimestamp = DateTime.Now.ToString("HH:mm:ss");
        TipNormalMessage = isError ? "" : message;
        TipErrorMessage = isError ? message : "";
    }

    partial void OnTipTimestampChanged(string value)
    {
        OnPropertyChanged(nameof(TipFullMessage));
    }

    partial void OnTipNormalMessageChanged(string value)
    {
        OnPropertyChanged(nameof(TipFullMessage));
    }

    partial void OnTipErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(TipFullMessage));
    }

    [RelayCommand]
    private void ExecuteOpenDownloadManager()
    {
        if (_batchSearchWindow != null)
        {
            if (!_batchSearchWindow.IsVisible)
            {
                _batchSearchWindow = null;
            }
            else
            {
                if (_batchSearchWindow.WindowState == WindowState.Minimized)
                {
                    _batchSearchWindow.WindowState = WindowState.Normal;
                }
                _batchSearchWindow.Activate();
                return;
            }
        }

        _batchSearchWindow = new BatchSearchWindow(_downloadManagerViewModel);
        _batchSearchWindow.Closed += (_, _) => _batchSearchWindow = null;
        _batchSearchWindow.Show();
        SetTip("已打开下载管理窗口。", false);
    }
}
