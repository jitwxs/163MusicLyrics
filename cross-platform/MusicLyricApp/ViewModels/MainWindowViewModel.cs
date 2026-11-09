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
using NAudio.Wave;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels.Messages;
using MusicLyricApp.Views;

namespace MusicLyricApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public virtual bool IsPlaybackSupported => OperatingSystem.IsWindows();
    public SearchParamViewModel SearchParamViewModel { get; } = new();

    public SearchResultViewModel SearchResultViewModel { get; } = new();
    
    public SignalLampViewModel LampVm { get; } = new();

    /// <summary>"MusicLyricApp v{major}.{minor}", from the assembly version set in the project file.</summary>
    [ObservableProperty] private string _appTitle = BuildAppTitle();

    private static string BuildAppTitle()
    {
        var version = typeof(MainWindowViewModel).Assembly.GetName().Version ?? new Version(0, 0);
        return $"MusicLyricApp v{version.Major}.{version.Minor}";
    }

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
    
    /// <summary>Fuzzy search results shown in place of the lyrics preview; null when none are open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingBlurResults))]
    private BlurSearchViewModel? _blurResults;

    public bool IsShowingBlurResults => BlurResults != null;

    /// <summary>Raised for every status message; the window shows it as a toast. Args: message, isError.</summary>
    public event Action<string, bool>? TipRaised;

    // ---- Page navigation (single window with a sidebar) ----
    public const string PageSearch = "search";
    public const string PageBatch = "batch";
    public const string PageConvert = "convert";
    /// <summary>Settings sections are sidebar children keyed "settings/&lt;section&gt;" (see <see cref="SettingViewModel"/>).</summary>
    public const string SettingsKeyPrefix = "settings/";
    public const string PageSettings = SettingsKeyPrefix + SettingViewModel.SectionLyrics;

    public static bool IsSettingsKey(string? key) => key?.StartsWith(SettingsKeyPrefix, StringComparison.Ordinal) == true;

    /// <summary>Key of the page shown in the main window; bound two-way to the sidebar.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchPage), nameof(IsBatchPage), nameof(IsConvertPage), nameof(IsSettingsPage))]
    private string _currentPageKey = PageSearch;

    public bool IsSearchPage => CurrentPageKey == PageSearch;
    public bool IsBatchPage => CurrentPageKey == PageBatch;
    public bool IsConvertPage => CurrentPageKey == PageConvert;
    public bool IsSettingsPage => IsSettingsKey(CurrentPageKey);

    public BatchSearchViewModel BatchSearchViewModel => _downloadManagerViewModel;

    /// <summary>Created on first visit and kept, so the text being converted survives page switches.</summary>
    [ObservableProperty] private FormatConvertViewModel? _formatConvertViewModel;

    /// <summary>
    /// Recreated on entering the settings area (as the old settings window was) and flushed when leaving it;
    /// moving between settings sections keeps the same instance.
    /// </summary>
    [ObservableProperty] private SettingViewModel? _settingViewModel;

    partial void OnCurrentPageKeyChanged(string? oldValue, string newValue)
    {
        var wasSettings = IsSettingsKey(oldValue);
        var isSettings = IsSettingsKey(newValue);
        if (wasSettings && !isSettings)
        {
            FlushSettingsPage();
        }

        if (isSettings)
        {
            var section = newValue[SettingsKeyPrefix.Length..];
            if (!SettingViewModel.Sections.Contains(section))
            {
                CurrentPageKey = PageSettings;
                return;
            }

            if (!wasSettings || SettingViewModel == null)
            {
                SettingViewModel = new SettingViewModel(SettingBean, _windowProvider);
            }

            SettingViewModel.CurrentSection = section;
            return;
        }

        switch (newValue)
        {
            case PageConvert:
                FormatConvertViewModel ??= new FormatConvertViewModel(SettingBean);
                break;
            case PageSearch or PageBatch:
                break;
            default:
                // Unknown keys (e.g. a cleared sidebar selection) fall back to the search page.
                CurrentPageKey = PageSearch;
                break;
        }
    }

    /// <summary>Applies what the settings page holds in memory, like closing the old settings window did.</summary>
    private void FlushSettingsPage()
    {
        if (SettingViewModel == null)
        {
            return;
        }

        SettingViewModel.OnClosing();
        UpdateTheme();

        // Timestamp formats, lyric types and their order all shape the preview.
        _ = RefreshCurrentConsoleOutputAsync();
    }

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
    public MainWindowViewModel(IWindowProvider windowProvider, StorageService? storageService = null,
        SearchService? searchService = null)
    {
        _windowProvider = windowProvider;
        _storageService = storageService ?? new StorageService();

        _settingBean = _storageService.ReadAppConfig();
        LocalizationService.Instance.SetLanguage(_settingBean.Config.Language);
        NetworkClientFactory.Configure(_settingBean.Config);

        _searchService = searchService ?? new SearchService(_settingBean);
        
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
            if (m.Value == "BlurSearchWindow")
            {
                BlurResults = null;
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
        ++_previewRevision;
        IsBlurSearch = false;
        var sourceItem = SearchParamViewModel.SearchSources.FirstOrDefault(x => x.Value == request.SearchSource);
        if (sourceItem != null)
        {
            SearchParamViewModel.SelectedSearchSourceItem = sourceItem;
        }

        SearchParamViewModel.SearchText = request.SongId;
        CurrentPageKey = PageSearch;

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

            SetTip(LocalizationService.Get("Status.Main.DetailsFromCache"), false);
            return;
        }

        await ExecuteSearchAsync();
    }

    public void SaveConfig()
    {
        if (IsSettingsPage)
        {
            SettingViewModel?.OnClosing();
        }

        _storageService.SaveConfig(SettingBean);
    }

    /// <summary>Options that change how the lyrics are rendered; the preview follows them at once.</summary>
    private static readonly HashSet<string> PreviewAffectingParams =
    [
        nameof(SearchParamViewModel.SelectedOutputFormatItem),
        nameof(SearchParamViewModel.SelectedLrcTypeItem),
        nameof(SearchParamViewModel.LrcMergeSeparator)
    ];

    private void SearchParamViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == null || !PreviewAffectingParams.Contains(e.PropertyName))
        {
            return;
        }

        _ = RefreshCurrentConsoleOutputAsync();
    }

    private int _previewRevision;

    private async Task RefreshCurrentConsoleOutputAsync()
    {
        var revision = ++_previewRevision;
        if (SearchResultViewModel.SaveVoMap.Count != 1)
        {
            return;
        }

        var saveVo = SearchResultViewModel.SaveVoMap.Values.First();
        bool IsCurrent() => revision == _previewRevision &&
                            SearchResultViewModel.SaveVoMap.Count == 1 &&
                            ReferenceEquals(SearchResultViewModel.SaveVoMap.Values.First(), saveVo);
        try
        {
            var output = saveVo.LyricVo.IsEmpty()
                ? string.Empty
                : GlobalUtils.MergeStr(await LyricUtils.GetOutputContent(saveVo.LyricVo, SettingBean));
            // Translation can finish after another option or song has been selected.
            if (IsCurrent())
            {
                SearchResultViewModel.ConsoleOutput = output;
            }
        }
        catch (Exception ex)
        {
            // Event-triggered refreshes have no awaiting caller to report failures.
            if (IsCurrent())
            {
                SetTip(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync()
    {
        if (IsBlurSearch)
        {
            await ExecuteBlurSearchAsync();
            return;
        }

        ++_previewRevision;

        // An exact search shows its own preview, so any open fuzzy results give way to it.
        BlurResults = null;

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
                if (SearchResultViewModel.SaveVoMap.Values.Any(saveVo => saveVo.LyricVo.IsEmpty()))
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
                    SetTip(LocalizationService.Get("Status.Main.CacheHit"), false);
                }
            }
            else
            {
                ExecuteOpenDownloadManager();
                SearchResultViewModel.SaveVoMap.Clear();
                SearchResultViewModel.ResetConsoleOutput("");
                SetTip(LocalizationService.Get("Status.Main.DownloadOpened"), false);
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

            var results = new BlurSearchViewModel(resultVos);
            results.LoadTypeAResults();
            BlurResults = results;
            CurrentPageKey = PageSearch;
            SetTip(LocalizationService.Format("Status.Main.BlurSuccess", resultVos.Count), false);
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
    private async Task ExecuteCopySongLinkAsync()
    {
        var link = SearchResultViewModel.SongLink;
        if (string.IsNullOrWhiteSpace(link))
        {
            SetTip(LocalizationService.Get("Status.Main.NoSongUrl"), true);
            return;
        }

        try
        {
            await _windowProvider.SetTextAsync(link);
            SetTip(LocalizationService.Get("Status.Main.SongUrlCopied"), false);
        }
        catch (Exception ex)
        {
            SetTip(LocalizationService.Format("Status.Main.CopySongUrlFailed", ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task ExecuteCopySongPicAsync()
    {
        if (SearchResultViewModel.SaveVoMap.Count == 0)
        {
            SetTip(ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_PIC, true);
            return;
        }

        if (SearchResultViewModel.SaveVoMap.Count != 1)
        {
            SetTip(LocalizationService.Get("Status.Main.SingleSongRequired"), true);
            return;
        }

        var link = SearchResultViewModel.SaveVoMap.Values.Single().SongVo.Pics;
        if (string.IsNullOrWhiteSpace(link))
        {
            SetTip(ErrorMsgConst.SONG_PIC_GET_FAILED, true);
            return;
        }

        try
        {
            await _windowProvider.SetTextAsync(link);
            SetTip(ErrorMsgConst.SONG_PIC_GET_SUCCESS, false);
        }
        catch (Exception ex)
        {
            SetTip(LocalizationService.Format("Status.Main.CopyCoverUrlFailed", ex.Message), true);
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

        if (IsPlaybackSupported)
        {
            EnsurePlaybackPrepared();
            UpdatePlaybackProgress();
        }
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
            SetTip(LocalizationService.Get("Status.Main.UnsupportedPlayer"), true);
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
            SetTip(LocalizationService.Get("Status.Main.PlaybackPaused"), false);
        }
        else
        {
            _waveOut.Play();
            _playbackTimer.Start();
            IsPlayingSong = true;
            SetTip(LocalizationService.Get("Status.Main.PlaybackResumed"), false);
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
            SetTip(LocalizationService.Get("Status.Main.UnsupportedPlayer"), true);
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
            SetTip(LocalizationService.Format("Status.Main.PlayerInitFailed", ex.Message), true);
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
        CurrentPageKey = PageConvert;
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

    /// <summary>Opens a page by sidebar key; settings keep the section already open, if any.</summary>
    [RelayCommand]
    private void ExecuteGoToPage(string? key)
    {
        if (key == null)
        {
            return;
        }

        if (IsSettingsKey(key) && IsSettingsPage)
        {
            return;
        }

        CurrentPageKey = key;
    }

    [RelayCommand]
    private void ExecuteSetting()
    {
        CurrentPageKey = PageSettings;
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
                
                if (await DialogHelper.ShowReleaseNotesAsync(content))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = UpdateCheckService.ReleasePageUrl,
                        UseShellExecute = true
                    });
                }
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
        ThemeService.Apply(SettingBean.Config.ThemeMode);
    }

    [RelayCommand]
    private async Task ExecuteSelectOutputFolderAsync()
    {
        try
        {
            var folder = await _storageService.SelectFolderAndRememberAsync(SettingBean, _windowProvider);
            if (folder == null)
            {
                return;
            }
            LastSaveFolderPath = SettingBean.Config.LastSaveFolderPath;
            SetTip(LocalizationService.Format("Status.Main.SavePathUpdated", LastSaveFolderPath), false);
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
    private void ExecuteCloseBlurResults()
    {
        BlurResults = null;
    }

    private void SetTip(string message, bool isError)
    {
        TipTimestamp = DateTime.Now.ToString("HH:mm:ss");
        TipNormalMessage = isError ? "" : message;
        TipErrorMessage = isError ? message : "";
        TipRaised?.Invoke(message, isError);
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
        CurrentPageKey = PageBatch;
        SetTip(LocalizationService.Get("Status.Main.DownloadOpened"), false);
    }
}
