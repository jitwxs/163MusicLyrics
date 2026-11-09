using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels.Messages;

namespace MusicLyricApp.ViewModels;

public partial class BatchSearchViewModel : ViewModelBase
{
    private readonly SearchService _searchService;
    private readonly StorageService _storageService;
    private readonly SettingBean _settingBean;
    private readonly IWindowProvider _windowProvider;

    private readonly Dictionary<string, SaveVo> _saveMap = new();
    private readonly Dictionary<string, string> _songLinkMap = new();

    /// <summary>While true, list changes do not rebuild <see cref="VisibleItems"/>; used for bulk loads.</summary>
    private bool _suspendVisibleRefresh;

    /// <summary>Every song in the download manager, newest first.</summary>
    public ObservableCollection<BatchSearchItemViewModel> Items { get; } = new();

    /// <summary>The rows the table shows: <see cref="Items"/> narrowed by <see cref="FilterText"/>.</summary>
    public ObservableCollection<BatchSearchItemViewModel> VisibleItems { get; } = new();

    /// <summary>Text typed into the page's search field; matches song name, artist, album, ID or source.</summary>
    [ObservableProperty] private string _filterText = "";

    /// <summary>
    /// Asks the user to confirm deleting the given number of songs. Set by the view; when unset (tests,
    /// headless use) deletion goes ahead without asking.
    /// </summary>
    public System.Func<int, Task<bool>>? ConfirmDeleteAsync { get; set; }

    [ObservableProperty] private string _batchInputText = "";
    [ObservableProperty] private string _tipTimestamp = "";
    [ObservableProperty] private string _tipNormalMessage = "";
    [ObservableProperty] private string _tipErrorMessage = "";

    [ObservableProperty] private int _totalSongsCount;
    [ObservableProperty] private int _songsWithLyricsCount;
    [ObservableProperty] private int _saveSuccessCount;
    [ObservableProperty] private int _saveFailedCount;

    /// <summary>Raised for every status message; the window shows it as a toast. Args: message, isError.</summary>
    public event System.Action<string, bool>? TipRaised;

    public bool HasItems => Items.Count > 0;

    public bool IsFiltering => !string.IsNullOrWhiteSpace(FilterText);

    /// <summary>"Showing 3 of 120 songs" while a filter is active.</summary>
    public string FilterCountText => LocalizationService.Format("Batch.FilterCount", VisibleItems.Count, Items.Count);

    /// <summary>Message for an empty table: nothing in the list at all, or nothing matching the filter.</summary>
    public string TableEmptyMessage => LocalizationService.Get(IsFiltering ? "Batch.FilterEmpty" : "Batch.EmptyTitle");

    public BatchSearchViewModel(
        SettingBean settingBean,
        SearchService searchService,
        StorageService storageService,
        IWindowProvider windowProvider)
    {
        _storageService = storageService;
        _settingBean = settingBean;
        _searchService = searchService;
        _windowProvider = windowProvider;
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            RefreshVisibleItems();
        };
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FilterCountText));
            OnPropertyChanged(nameof(TableEmptyMessage));
        };
    }

    partial void OnFilterTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsFiltering));
        OnPropertyChanged(nameof(TableEmptyMessage));
        RefreshVisibleItems();
    }

    private bool MatchesFilter(BatchSearchItemViewModel item)
    {
        var query = FilterText.Trim();
        if (query.Length == 0)
        {
            return true;
        }

        static bool Has(string? field, string q) => field?.Contains(q, System.StringComparison.OrdinalIgnoreCase) == true;
        return Has(item.SongName, query) || Has(item.Singer, query) || Has(item.Album, query) ||
               Has(item.SongId, query) || Has(item.SongSource, query);
    }

    /// <summary>
    /// Rebuilds the shown rows from the full list. Rows the filter hides are unticked, so bulk actions only ever
    /// touch rows the user can see.
    /// </summary>
    private void RefreshVisibleItems()
    {
        if (_suspendVisibleRefresh)
        {
            return;
        }

        var visible = new List<BatchSearchItemViewModel>(Items.Count);
        foreach (var item in Items)
        {
            if (MatchesFilter(item))
            {
                visible.Add(item);
            }
            else if (item.IsSelected)
            {
                item.IsSelected = false;
            }
        }

        if (!visible.SequenceEqual(VisibleItems))
        {
            VisibleItems.Clear();
            foreach (var item in visible) VisibleItems.Add(item);
        }

        OnPropertyChanged(nameof(FilterCountText));
    }

    /// <summary>Puts a row at <paramref name="index"/>, adding it or moving it there.</summary>
    private void PlaceAt(BatchSearchItemViewModel item, int index)
    {
        var current = Items.IndexOf(item);
        if (current < 0)
        {
            Items.Insert(index, item);
        }
        else if (current != index)
        {
            Items.Move(current, index);
        }
    }

    public void AddSearchResults(Dictionary<string, ResultVo<SaveVo>> resDict, List<InputSongId> inputSongIds, string? inputText = null)
    {
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            BatchInputText = inputText;
            // A new search should show its results, not hide them behind an old filter.
            FilterText = "";
        }

        var existing = Items.ToDictionary(i => i.SongId, i => i);
        var added = 0;
        var position = 0;

        foreach (var input in inputSongIds)
        {
            if (!resDict.TryGetValue(input.SongId, out var result))
            {
                continue;
            }

            if (!existing.TryGetValue(input.SongId, out var item))
            {
                item = new BatchSearchItemViewModel { SongId = input.SongId };
                existing[input.SongId] = item;
                added++;
            }

            PlaceAt(item, position++);

            if (result.IsSuccess())
            {
                var saveVo = result.Data;
                _saveMap[input.SongId] = saveVo;
                LocalSongCacheService.SaveCache(_settingBean, input.SearchSource, input.SongId, saveVo);

                item.SongName = saveVo.SongVo.Name;
                item.SongSource = GetSourceName(input.SearchSource);
                item.SourceEnum = input.SearchSource;
                item.Singer = string.Join(_settingBean.Config.SingerSeparator, saveVo.SongVo.Singer);
                item.Album = saveVo.SongVo.Album;
                item.State = BatchItemStateEnum.READY;
                item.Status = LocalizationService.Get("Status.Batch.Ready");
                item.Error = saveVo.LyricVo.IsEmpty() ? ErrorMsgConst.LRC_NOT_EXIST : "";
                item.Progress = 100;
            }
            else
            {
                _saveMap.Remove(input.SongId);
                _songLinkMap.Remove(input.SongId);

                item.SongName = "";
                item.SongSource = GetSourceName(input.SearchSource);
                item.SourceEnum = input.SearchSource;
                item.Singer = "";
                item.Album = "";
                item.State = BatchItemStateEnum.FAILED;
                item.Status = LocalizationService.Get("Status.Batch.Failed");
                item.Error = result.ErrorMsg;
                item.Progress = 0;
            }
        }

        RefreshVisibleItems(); // names are known only now, so match the filter again
        RefreshStats();
        SetTip(LocalizationService.Format("Status.Batch.Sync", added), false);
    }

    /// <summary>
    /// Restores recently cached songs into the list when the app starts, newest first, so earlier searches can be
    /// saved again without searching. How many is <see cref="ConfigBean.SearchCacheStartupLoadLimit"/>. Files are
    /// read off the UI thread; songs searched meanwhile stay above them and are not duplicated.
    /// </summary>
    public async Task LoadFromLocalCacheAsync()
    {
        var limit = _settingBean.Config.SearchCacheStartupLoadLimit;
        if (limit <= 0)
        {
            return;
        }

        var songs = await Task.Run(() => LocalSongCacheService.LoadRecent(_settingBean, limit));
        if (songs.Count == 0)
        {
            return;
        }

        var existing = Items.Select(i => i.SongId).ToHashSet();
        _suspendVisibleRefresh = true;
        try
        {
            foreach (var song in songs)
            {
                if (!existing.Add(song.SongId))
                {
                    continue;
                }

                var saveVo = song.SaveVo;
                _saveMap[song.SongId] = saveVo;
                if (!string.IsNullOrWhiteSpace(song.SongLink))
                {
                    _songLinkMap[song.SongId] = song.SongLink;
                }

                Items.Add(new BatchSearchItemViewModel
                {
                    SongId = song.SongId,
                    SongName = saveVo.SongVo.Name,
                    SongSource = GetSourceName(song.Source),
                    SourceEnum = song.Source,
                    Singer = string.Join(_settingBean.Config.SingerSeparator, saveVo.SongVo.Singer),
                    Album = saveVo.SongVo.Album,
                    State = BatchItemStateEnum.READY,
                    Status = LocalizationService.Get("Status.Batch.Ready"),
                    Error = saveVo.LyricVo.IsEmpty() ? ErrorMsgConst.LRC_NOT_EXIST : "",
                    Progress = 100
                });
            }
        }
        finally
        {
            _suspendVisibleRefresh = false;
        }

        RefreshVisibleItems();
        RefreshStats();
    }

    [RelayCommand]
    private void ExecuteSelectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ExecuteClearSelected()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
    }

    /// <summary>Deletes the ticked songs from the list and from the local cache, after the user confirms.</summary>
    [RelayCommand]
    private async Task ExecuteDeleteSelectedAsync()
    {
        var toRemove = Items.Where(item => item.IsSelected).ToList();
        if (toRemove.Count == 0)
        {
            SetTip(LocalizationService.Get("Status.Batch.NoDeleteSelection"), false);
            return;
        }

        if (ConfirmDeleteAsync != null && !await ConfirmDeleteAsync(toRemove.Count))
        {
            return;
        }

        var cacheTargets = toRemove.Select(item => (item.SourceEnum, item.SongId)).ToList();
        foreach (var item in toRemove)
        {
            _saveMap.Remove(item.SongId);
            _songLinkMap.Remove(item.SongId);
            Items.Remove(item);
        }

        RefreshStats();
        await Task.Run(() =>
        {
            foreach (var (source, songId) in cacheTargets)
            {
                LocalSongCacheService.Delete(_settingBean, source, songId);
            }
        });

        RefreshStats();
        SetTip(LocalizationService.Format("Status.Batch.DeleteComplete", toRemove.Count), false);
    }

    [RelayCommand]
    private Task ExecuteSaveSelectedAsync() => SaveItemsAsync(Items.Where(item => item.IsSelected).ToList());

    /// <summary>Saves the given rows' lyrics and updates their status; the selection is left untouched.</summary>
    private async Task SaveItemsAsync(List<BatchSearchItemViewModel> selected)
    {
        if (selected.Count == 0)
        {
            SetTip(LocalizationService.Get("Status.Batch.NoSaveSelection"), false);
            return;
        }

        var saveMap = new Dictionary<string, SaveVo>();
        foreach (var item in selected)
        {
            if (_saveMap.TryGetValue(item.SongId, out var saveVo))
            {
                saveMap[item.SongId] = saveVo;
            }
        }

        if (saveMap.Count == 0)
        {
            SetTip(LocalizationService.Get("Status.Batch.EmptyLyrics"), true);
            return;
        }

        try
        {
            var temp = new SearchResultViewModel { SaveVoMap = saveMap };
            var message = await _storageService.SaveResult(temp, _settingBean, _windowProvider);
            foreach (var item in selected.Where(i => saveMap.ContainsKey(i.SongId)))
            {
                var skipReason = StorageService.IsSkipStorage(saveMap[item.SongId], _settingBean);
                var saved = skipReason == null;
                item.State = saved ? BatchItemStateEnum.SAVED : BatchItemStateEnum.SKIPPED;
                item.Status = LocalizationService.Get(saved ? "Status.Batch.Saved" : "Status.Batch.Skipped");
                item.Error = skipReason ?? "";
                item.Progress = saved ? 100 : 0;
            }

            var stats = ParseSaveMessage(message);
            SaveSuccessCount += stats.success;
            SaveFailedCount += stats.failed;

            RefreshStats();
            SetTip(message, stats.failed > 0);
        }
        catch (System.Exception ex)
        {
            SetTip(ex.Message, true);
        }
    }

    [RelayCommand]
    private Task ExecuteContextSaveAsync(BatchSearchItemViewModel? item) =>
        // A row's own save button saves just that row, whatever else is ticked.
        item == null ? ExecuteSaveSelectedAsync() : SaveItemsAsync([item]);

    [RelayCommand]
    private void ExecuteViewDetail(BatchSearchItemViewModel? item)
    {
        var target = item ?? Items.FirstOrDefault(x => x.IsSelected);
        if (target == null)
        {
            SetTip(LocalizationService.Get("Status.Batch.NoDetailSelection"), false);
            return;
        }

        // The main view model switches back to the search page to show the song.
        WeakReferenceMessenger.Default.Send(new OpenSongDetailMessage(new SongDetailRequest(target.SongId, target.SourceEnum)));
    }

    private void RefreshStats()
    {
        TotalSongsCount = Items.Count;
        SongsWithLyricsCount = _saveMap.Count(pair => !pair.Value.LyricVo.IsEmpty());
    }

    private void SetTip(string message, bool isError)
    {
        TipTimestamp = System.DateTime.Now.ToString("HH:mm:ss");
        TipNormalMessage = isError ? "" : message;
        TipErrorMessage = isError ? message : "";
        TipRaised?.Invoke(message, isError);
    }

    private static (int success, int failed) ParseSaveMessage(string message)
    {
        var matches = Regex.Matches(message, @"\d+");
        if (matches.Count >= 2)
        {
            return (int.Parse(matches[0].Value), int.Parse(matches[1].Value));
        }

        return (0, 0);
    }

    private static string GetSourceName(SearchSourceEnum source)
    {
        return source switch
        {
            SearchSourceEnum.NET_EASE_MUSIC => LocalizationService.Get("Status.Search.SourceNetEase"),
            SearchSourceEnum.QQ_MUSIC => LocalizationService.Get("Status.Search.SourceQQ"),
            _ => source.ToString()
        };
    }

    public bool TryGetCachedSaveVo(string songId, out SaveVo saveVo)
    {
        return _saveMap.TryGetValue(songId, out saveVo!);
    }

    public bool TryGetCachedSongLink(string songId, out string link)
    {
        if (_songLinkMap.TryGetValue(songId, out link!))
        {
            return true;
        }

        var item = Items.FirstOrDefault(x => x.SongId == songId);
        if (item == null)
        {
            link = string.Empty;
            return false;
        }

        if (LocalSongCacheService.TryLoadSongLink(_settingBean, item.SourceEnum, songId, out link))
        {
            _songLinkMap[songId] = link;
            return true;
        }

        return false;
    }

    public void CacheSongLink(string songId, string link)
    {
        if (string.IsNullOrWhiteSpace(songId) || string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        _songLinkMap[songId] = link;
        var item = Items.FirstOrDefault(x => x.SongId == songId);
        if (item != null)
        {
            LocalSongCacheService.UpdateSongLink(_settingBean, item.SourceEnum, songId, link);
        }
    }

    public void SelectSongsByIds(IEnumerable<string> songIds)
    {
        var set = songIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
        FilterText = ""; // the songs being picked must be visible
        foreach (var item in Items)
        {
            item.IsSelected = set.Contains(item.SongId);
        }
    }
}
