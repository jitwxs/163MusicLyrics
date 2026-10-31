using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
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

    public ObservableCollection<BatchSearchItemViewModel> Items { get; } = new();

    [ObservableProperty] private string _batchInputText = "";
    [ObservableProperty] private string _tipTimestamp = "";
    [ObservableProperty] private string _tipNormalMessage = "";
    [ObservableProperty] private string _tipErrorMessage = "";

    [ObservableProperty] private int _totalSongsCount;
    [ObservableProperty] private int _songsWithLyricsCount;
    [ObservableProperty] private int _saveSuccessCount;
    [ObservableProperty] private int _saveFailedCount;

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

    }

    public void AddSearchResults(Dictionary<string, ResultVo<SaveVo>> resDict, List<InputSongId> inputSongIds, string? inputText = null)
    {
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            BatchInputText = inputText;
        }

        var existing = Items.ToDictionary(i => i.SongId, i => i);
        var added = 0;

        foreach (var input in inputSongIds)
        {
            if (!resDict.TryGetValue(input.SongId, out var result))
            {
                continue;
            }

            if (!existing.TryGetValue(input.SongId, out var item))
            {
                item = new BatchSearchItemViewModel { SongId = input.SongId };
                Items.Add(item);
                existing[input.SongId] = item;
                added++;
            }

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
                item.Status = "Ready";
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
                item.Status = "Failed";
                item.Error = result.ErrorMsg;
                item.Progress = 0;
            }
        }

        RefreshStats();
        SetTip(LocalizationService.Format("Status.Batch.Sync", added), false);
    }

    [RelayCommand]
    private async Task ExecuteSelectFolderAsync()
    {
        try
        {
            var folders = await _windowProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocalizationService.Get("Dialog.Folder.Select"),
                AllowMultiple = false
            });

            if (folders.Count == 0)
            {
                return;
            }

            var folder = folders[0];
            var localPath = folder.Path?.LocalPath ?? folder.Path?.ToString();
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            BatchInputText = localPath;
            await RunBatchSearchAsync(localPath);
        }
        catch (System.Exception ex)
        {
            SetTip(ex.Message, true);
        }
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

    [RelayCommand]
    private void ExecuteDeleteSelected()
    {
        var toRemove = Items.Where(item => item.IsSelected).ToList();
        if (toRemove.Count == 0)
        {
            SetTip(LocalizationService.Get("Status.Batch.NoDeleteSelection"), false);
            return;
        }

        foreach (var item in toRemove)
        {
            _saveMap.Remove(item.SongId);
            _songLinkMap.Remove(item.SongId);
            Items.Remove(item);
        }

        RefreshStats();
        SetTip(LocalizationService.Format("Status.Batch.DeleteComplete", toRemove.Count), false);
    }

    [RelayCommand]
    private async Task ExecuteSaveSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToList();
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
    private async Task ExecuteContextSaveAsync(BatchSearchItemViewModel? item)
    {
        if (item != null && !item.IsSelected)
        {
            item.IsSelected = true;
        }

        await ExecuteSaveSelectedAsync();
    }

    [RelayCommand]
    private void ExecuteViewDetail(BatchSearchItemViewModel? item)
    {
        var target = item ?? Items.FirstOrDefault(x => x.IsSelected);
        if (target == null)
        {
            SetTip(LocalizationService.Get("Status.Batch.NoDetailSelection"), false);
            return;
        }

        if (!target.IsSelected)
        {
            target.IsSelected = true;
        }

        WeakReferenceMessenger.Default.Send(new OpenSongDetailMessage(new SongDetailRequest(target.SongId, target.SourceEnum)));
        WeakReferenceMessenger.Default.Send(new CloseWindowMessage("BatchSearchWindow"));
    }

    private async Task RunBatchSearchAsync(string inputText)
    {
        try
        {
            var searchParam = new SearchParamViewModel();
            searchParam.Bind(_settingBean.Param);
            searchParam.SearchText = inputText;

            _searchService.InitSongIds(searchParam, _settingBean);
            var result = await Task.Run(() => _searchService.SearchSongs(searchParam.SongIds, _settingBean));
            AddSearchResults(result, searchParam.SongIds, inputText);
        }
        catch (System.Exception ex)
        {
            SetTip(ex.Message, true);
        }
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
        foreach (var item in Items)
        {
            item.IsSelected = set.Contains(item.SongId);
        }
    }
}
