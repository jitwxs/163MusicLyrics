using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.LocalMusic;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public enum LocalScanState { Pending, Matching, Review, Ready, NoMatch, Failed, Saving, Saved, Skipped, NoLyrics }

public partial class LocalScanItemViewModel(LocalAudioFile audio) : ViewModelBase
{
    public LocalAudioFile Audio { get; private set; } = audio;
    public string Path => Audio.Path;
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Metadata => string.Join(" · ", new[]
    {
        string.Join(" — ", new[] { Audio.Title, Audio.Artist }.Where(x => !string.IsNullOrWhiteSpace(x))),
        Audio.Album,
        Audio.Duration > 0 ? TimeSpan.FromMilliseconds(Audio.Duration).ToString(@"mm\:ss") : ""
    }.Where(x => !string.IsNullOrWhiteSpace(x)));
    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private string _keyword = string.Join(" ", new[] { audio.Title, audio.Artist }.Where(x => !string.IsNullOrWhiteSpace(x)));
    [ObservableProperty] private LocalScanState _state = audio.Error == null ? LocalScanState.Pending : LocalScanState.Failed;
    [ObservableProperty] private string _detail = audio.Error ?? "";
    [ObservableProperty] private LocalSongCandidate? _candidate;
    public bool RetrySave { get; set; }
    public ObservableCollection<LocalSongCandidate> Candidates { get; } = [];
    public string Status => LocalizationService.Get("LocalScan.State." + State);
    partial void OnStateChanged(LocalScanState value) => OnPropertyChanged(nameof(Status));
    partial void OnCandidateChanged(LocalSongCandidate? value)
    {
        RetrySave = false;
        if (State is LocalScanState.Ready or LocalScanState.Saved or LocalScanState.NoLyrics)
            State = LocalScanState.Review;
    }
    partial void OnKeywordChanged(string value)
    {
        Candidate = null;
        Candidates.Clear();
        State = LocalScanState.Pending;
        Detail = "";
    }
    public void UpdateAudio(LocalAudioFile value)
    {
        Audio = value;
        OnPropertyChanged(nameof(Metadata));
        if (string.IsNullOrWhiteSpace(Keyword)) Keyword = string.Join(" ", new[] { value.Title, value.Artist }.Where(x => x.Length > 0));
    }
    public void RefreshLanguage() => OnPropertyChanged(nameof(Status));
}

/// <summary>Session-local file tasks; no dependence on the search page's source/type or ID-only row dictionaries.</summary>
public partial class LocalMusicScanViewModel : ViewModelBase, IDisposable
{
    private readonly SettingBean _source;
    // Snapshot of the app settings, taken when each operation starts, so later edits on the settings page
    // apply to the next scan, match or save without the page mutating the persisted settings.
    private SettingBean _settings;
    private readonly LocalAudioScanner _scanner;
    private readonly IAudioMetadataReader _metadata;
    private readonly LocalSongMatcher _matcher;
    private readonly LocalLyricWriter _writer = new();
    private CancellationTokenSource? _cancellation;
    private string _scannedRoot = "";
    private bool _lastCancelled;
    private readonly Dictionary<string, string> _outputOwners = new(OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<LocalScanItemViewModel> Items { get; } = [];
    public ObservableCollection<string> Issues { get; } = [];
    public ObservableCollection<EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>> Sources { get; } = new(Enum.GetValues<SearchSourceEnum>().Select(source =>
        new EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum> { Value = source }));
    [ObservableProperty] private EnumDisplayHelper.EnumDisplayItem<SearchSourceEnum>? _preferredSource;
    [ObservableProperty] private string _folderPath = "";
    [ObservableProperty] private string _outputFolder = "";
    [ObservableProperty] private bool _includeSubfolders = true;
    [ObservableProperty] private bool _useFallback = true;
    [ObservableProperty] private bool _filenameIsId;
    [ObservableProperty] private bool _besideAudio = true;
    [ObservableProperty] private bool _overwrite;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _completed;
    [ObservableProperty] private int _total;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _preview = "";
    [ObservableProperty] private LocalScanItemViewModel? _selectedItem;
    public bool IsIdle => !IsBusy;
    public bool HasItems => Items.Count > 0;
    public bool HasSelection => SelectedItem != null;
    public bool HasPreview => Preview.Length > 0;
    public bool HasIssues => Issues.Count > 0;
    public string IssuesText => string.Join(Environment.NewLine, Issues);
    public string Summary => LocalizationService.Format("LocalScan.Summary", Items.Count,
        Items.Count(x => x.State == LocalScanState.Ready), Items.Count(x => x.State == LocalScanState.Review),
        Items.Count(x => x.State == LocalScanState.Saved), Items.Count(x => x.State is LocalScanState.Failed or LocalScanState.NoLyrics or LocalScanState.NoMatch));
    public string ProgressText => LocalizationService.Format("LocalScan.Progress", Completed, Total);

    public LocalMusicScanViewModel(SettingBean settings, Func<SearchSourceEnum, IMusicApi> api,
        IAudioMetadataReader? metadata = null, TimeSpan? requestInterval = null)
    {
        // Preserve lyric formatting without mutating the main page's persisted settings.
        _source = settings;
        _settings = Snapshot();
        _metadata = metadata ?? new AudioMetadataReader();
        _scanner = new LocalAudioScanner(_metadata);
        _matcher = new LocalSongMatcher(api, requestInterval);
        PreferredSource = Sources.First(x => x.Value == settings.Param.SearchSource);
        OutputFolder = settings.Config.LastSaveFolderPath;
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasItems));
        Issues.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(HasIssues)); OnPropertyChanged(nameof(IssuesText)); };
        LocalizationService.Observe(this, static vm => vm.OnLanguageChanged());
    }

    private void OnLanguageChanged()
    {
        foreach (var item in Items) item.RefreshLanguage();
        RefreshSummary();
        OnPropertyChanged(nameof(ProgressText));
    }
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));
    partial void OnCompletedChanged(int value) => OnPropertyChanged(nameof(ProgressText));
    partial void OnTotalChanged(int value) => OnPropertyChanged(nameof(ProgressText));
    partial void OnPreviewChanged(string value) => OnPropertyChanged(nameof(HasPreview));
    partial void OnSelectedItemChanged(LocalScanItemViewModel? value)
    {
        Preview = "";
        OnPropertyChanged(nameof(HasSelection));
    }
    /// <summary>Raised for every status message; the main window shows it as a toast. Args: message, isError.</summary>
    public event Action<string, bool>? TipRaised;

    private SettingBean Snapshot() => _source.ToJson().ToEntity<SettingBean>();

    private void Tip(string message, bool isError = false)
    {
        Message = message;
        if (message.Length > 0) TipRaised?.Invoke(message, isError);
    }

    /// <summary>Shows an error raised by the view, such as a folder picker failure.</summary>
    public void ReportError(string message) => Tip(message, true);

    private void RefreshSummary() => OnPropertyChanged(nameof(Summary));
    private LocalSaveOptions SaveOptions => new(_scannedRoot, OutputFolder, BesideAudio, Overwrite);

    [RelayCommand]
    private void Cancel()
    {
        _cancellation?.Cancel();
        if (_cancellation != null) Tip(LocalizationService.Get("LocalScan.Cancelling"));
    }

    private async Task RunAsync(Func<CancellationToken, Task> operation, bool resetProgress = true)
    {
        if (IsBusy) return;
        IsBusy = true;
        // A single-file preview is not a batch; keep the last batch's progress visible.
        if (resetProgress) Completed = 0;
        Message = "";
        _lastCancelled = false;
        _settings = Snapshot();
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        try { await operation(cancellation.Token); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { _lastCancelled = true; Tip(LocalizationService.Get("LocalScan.Cancelled")); }
        catch (Exception ex) { Tip(ex.Message, true); }
        finally
        {
            _cancellation = null;
            IsBusy = false;
            RefreshSummary();
        }
    }

    [RelayCommand]
    private Task ScanAsync() => RunAsync(async ct =>
    {
        if (!Directory.Exists(FolderPath)) throw new IOException(LocalizationService.Get("LocalScan.InvalidFolder"));
        _scannedRoot = System.IO.Path.GetFullPath(FolderPath);
        Items.Clear(); Issues.Clear(); Preview = ""; SelectedItem = null; _outputOwners.Clear(); Total = 0;
        var files = new Progress<LocalAudioFile>(audio =>
        {
            var item = new LocalScanItemViewModel(audio);
            item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(item.State)) RefreshSummary(); };
            Items.Add(item);
            Completed = Items.Count;
            Total = Items.Count;
            RefreshSummary();
        });
        var issues = new Progress<string>(issue => Issues.Add(issue));
        IsScanning = true;
        try { await Task.Run(() => _scanner.Scan(_scannedRoot, IncludeSubfolders, files, issues, ct), ct); }
        finally { IsScanning = false; }
        Tip(LocalizationService.Get("LocalScan.ScanComplete"));
        SelectedItem = Items.FirstOrDefault();
    });

    [RelayCommand]
    private Task MatchSelectedAsync() => MatchAsync(Items.Where(x => x.IsSelected).ToArray());

    [RelayCommand]
    private Task SearchAgainAsync() => MatchAsync(SelectedItem == null ? [] : [SelectedItem]);

    [RelayCommand]
    private async Task RetryAsync()
    {
        if (IsBusy) return;
        var failedSaves = Items.Where(x => x.IsSelected && x.State == LocalScanState.Failed && x.RetrySave && x.Candidate != null).ToArray();
        var matching = Items.Where(x => x.IsSelected && !x.RetrySave &&
            x.State is LocalScanState.Failed or LocalScanState.NoMatch or LocalScanState.NoLyrics or LocalScanState.Pending).ToArray();
        if (matching.Length > 0)
        {
            await MatchAsync(matching);
            if (_lastCancelled) return;
        }
        if (failedSaves.Length > 0) await SaveItemsAsync(failedSaves);
    }

    private Task MatchAsync(IReadOnlyList<LocalScanItemViewModel> items) => RunAsync(async ct =>
    {
        Total = items.Count;
        // Existing-lyrics checks need a resolvable destination before any network request starts.
        if (!BesideAudio && string.IsNullOrWhiteSpace(OutputFolder))
            throw new IOException(LocalizationService.Get("LocalScan.InvalidFolder"));
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            item.State = LocalScanState.Matching;
            item.Detail = "";
            item.RetrySave = false;
            item.Candidate = null; item.Candidates.Clear();
            try
            {
                if (!Overwrite && LocalLyricWriter.HasExistingLyrics(item.Path, SaveOptions, _settings))
                {
                    item.State = LocalScanState.Skipped;
                    item.Detail = LocalizationService.Get("LocalScan.Existing");
                    continue;
                }
                if (item.Audio.Error != null) item.UpdateAudio(await Task.Run(() => _metadata.Read(item.Path), ct));
                var result = await _matcher.MatchAsync(item.Audio, item.Keyword, PreferredSource?.Value ?? SearchSourceEnum.NET_EASE_MUSIC,
                    UseFallback, FilenameIsId, ct);
                foreach (var candidate in result.Candidates) item.Candidates.Add(candidate);
                var originalKeyword = string.Join(" ", new[] { item.Audio.Title, item.Audio.Artist }.Where(x => !string.IsNullOrWhiteSpace(x)));
                item.Candidate = FilenameIsId || item.Keyword == originalKeyword ? result.Automatic : null;
                item.State = item.Candidate != null ? LocalScanState.Ready : result.Candidates.Count > 0
                    ? LocalScanState.Review : result.Warning.Length > 0 ? LocalScanState.Failed : LocalScanState.NoMatch;
                item.Detail = result.Warning;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            { item.State = LocalScanState.Pending; throw; }
            catch (Exception ex) { item.State = LocalScanState.Failed; item.Detail = ex.Message; }
            finally { Completed++; RefreshSummary(); }
        }
    });

    [RelayCommand]
    private void ConfirmCandidate()
    {
        if (IsBusy || SelectedItem?.Candidate == null) return;
        SelectedItem.State = LocalScanState.Ready;
        SelectedItem.Detail = "";
        RefreshSummary();
    }

    [RelayCommand]
    private void SelectAll()
    {
        if (IsBusy) return;
        var select = Items.Any(x => !x.IsSelected);
        foreach (var item in Items) item.IsSelected = select;
    }

    [RelayCommand]
    private Task PreviewAsync() => RunAsync(async ct =>
    {
        var candidate = SelectedItem?.Candidate;
        Preview = "";
        if (candidate == null) { Tip(LocalizationService.Get("LocalScan.ChooseCandidate")); return; }
        var lyrics = await _matcher.GetLyricsAsync(candidate, _settings.Config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE, ct);
        var output = await Task.Run(() => LyricUtils.GetOutputContent(lyrics, _settings), ct);
        ct.ThrowIfCancellationRequested();
        Preview = string.Join(Environment.NewLine + Environment.NewLine, output);
    }, resetProgress: false);

    [RelayCommand]
    private Task SaveSelectedAsync() => SaveItemsAsync(Items.Where(x => x.IsSelected &&
        (x.State == LocalScanState.Ready || x.State == LocalScanState.Failed && x.RetrySave) && x.Candidate != null).ToArray());

    private Task SaveItemsAsync(IReadOnlyList<LocalScanItemViewModel> items) => RunAsync(async ct =>
    {
        Total = items.Count;
        if (items.Count == 0) { Tip(LocalizationService.Get("LocalScan.NoReady")); return; }
        var options = SaveOptions;
        if (!options.BesideAudio && string.IsNullOrWhiteSpace(options.OutputRoot))
            throw new IOException(LocalizationService.Get("LocalScan.InvalidFolder"));
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            item.State = LocalScanState.Saving;
            item.RetrySave = true;
            try
            {
                if (!Overwrite && LocalLyricWriter.HasExistingLyrics(item.Path, options, _settings))
                {
                    item.RetrySave = false; item.State = LocalScanState.Skipped; item.Detail = LocalizationService.Get("LocalScan.Existing"); continue;
                }
                var lyrics = await _matcher.GetLyricsAsync(item.Candidate!, _settings.Config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE, ct);
                if (_settings.Config.IgnorePureMusicInSave && lyrics.IsPureMusic())
                {
                    item.RetrySave = false;
                    item.State = LocalScanState.Skipped;
                    item.Detail = ErrorMsgConst.PURE_MUSIC_IGNORE_SAVE;
                    continue;
                }
                var written = await Task.Run(() => _writer.WriteAsync(item.Path, lyrics, _settings, options, ct,
                    paths => paths.All(path => !_outputOwners.TryGetValue(path, out var owner) || owner == item.Path)), ct);
                item.RetrySave = false;
                item.State = written.Skipped ? LocalScanState.Skipped : LocalScanState.Saved;
                item.Detail = written.Skipped ? LocalizationService.Get("LocalScan.Existing") : string.Join(Environment.NewLine, written.Paths);
                if (!written.Skipped)
                    foreach (var path in written.Paths) _outputOwners[path] = item.Path;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            { item.State = LocalScanState.Ready; throw; }
            catch (Exception ex)
            {
                item.State = ex is LocalLyricsUnavailableException ? LocalScanState.NoLyrics : LocalScanState.Failed;
                if (item.State == LocalScanState.NoLyrics) item.RetrySave = false;
                item.Detail = ex.Message;
            }
            finally { Completed++; RefreshSummary(); }
        }
    });

    public void Dispose()
    {
        _cancellation?.Cancel();
    }
}
