using System;
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
    [ObservableProperty] private string _preferredArtist = "MyGO!!!!!";
    [ObservableProperty] private string _searchProgressText = "";
    [ObservableProperty] private bool _isBatchNameSearching;
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

        var existing = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.SongId))
            .GroupBy(i => i.SongId)
            .ToDictionary(group => group.Key, group => group.First());
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
                item.TranslationStatus = string.IsNullOrWhiteSpace(saveVo.LyricVo.TranslateLyric) ? "无" : "有";
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
        SetTip($"已同步 {added} 条搜索结果。", false);
    }

    [RelayCommand]
    private async Task ExecuteSearchNamesAsync()
    {
        if (IsBatchNameSearching)
        {
            return;
        }

        var queries = BatchSongMatcher.ParseQueries(BatchInputText);
        if (queries.Count == 0)
        {
            SetTip("请每行输入一个歌名。", true);
            return;
        }

        if (queries.Count > Constants.BatchQuerySize)
        {
            SetTip($"单次最多处理 {Constants.BatchQuerySize} 首歌曲。", true);
            return;
        }

        RemovePreviousKeywordResults();
        IsBatchNameSearching = true;
        var matchedCount = 0;
        var unresolvedCount = 0;

        try
        {
            for (var index = 0; index < queries.Count; index++)
            {
                var query = queries[index];
                SearchProgressText = $"{index + 1}/{queries.Count}  正在搜索：{query.Title}";

                try
                {
                    if (await ResolveKeywordAsync(query))
                    {
                        matchedCount++;
                    }
                    else
                    {
                        unresolvedCount++;
                    }
                }
                catch (Exception ex)
                {
                    AddUnresolvedKeyword(query, ex.Message);
                    unresolvedCount++;
                }

                if (index + 1 < queries.Count)
                {
                    await Task.Delay(Constants.SleepMsBetweenBatchQuery);
                }
            }

            RefreshStats();
            SetTip($"歌名批量搜索完成：匹配 {matchedCount}，待处理 {unresolvedCount}。", unresolvedCount > 0);
        }
        finally
        {
            IsBatchNameSearching = false;
            SearchProgressText = "";
        }
    }

    [RelayCommand]
    private async Task ExecuteSelectFolderAsync()
    {
        try
        {
            var folders = await _windowProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folder",
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
            SetTip("未选择可删除项。", false);
            return;
        }

        foreach (var item in toRemove)
        {
            _saveMap.Remove(item.SongId);
            _songLinkMap.Remove(item.SongId);
            Items.Remove(item);
        }

        RefreshStats();
        SetTip($"已删除 {toRemove.Count} 条记录。", false);
    }

    [RelayCommand]
    private async Task ExecuteSaveSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SetTip("未选择可保存项。", false);
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
            SetTip("选中项没有可保存歌词。", true);
            return;
        }

        try
        {
            var temp = new SearchResultViewModel { SaveVoMap = saveMap };
            var message = await _storageService.SaveResult(temp, _settingBean, _windowProvider);
            foreach (var item in selected.Where(i => saveMap.ContainsKey(i.SongId)))
            {
                item.Status = "Saved";
                item.Progress = 100;
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
        if (target == null || string.IsNullOrWhiteSpace(target.SongId))
        {
            SetTip("未选择已匹配的歌曲。", false);
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

    private async Task<bool> ResolveKeywordAsync(BatchSongQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.DirectSongId) && query.DirectSource.HasValue)
        {
            return await ResolveDirectSongAsync(query);
        }

        var searchAttempt = await Task.Run(() =>
        {
            var results = new List<SearchResultVo>();
            var errors = new List<string>();
            foreach (var source in Enum.GetValues<SearchSourceEnum>())
            {
                try
                {
                    var response = _searchService.GetMusicApi(source).Search(query.Title, SearchTypeEnum.SONG_ID);
                    if (response.IsSuccess() && !response.Data.IsEmpty())
                    {
                        results.Add(response.Data);
                    }
                    else if (!response.IsSuccess())
                    {
                        errors.Add($"{source.ToDescription()}：{response.ErrorMsg}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{source.ToDescription()}：{ex.Message}");
                }
            }

            return (results, errors);
        });

        var preferredArtist = query.Artist ?? PreferredArtist;
        var candidates = BatchSongMatcher.RankCandidates(
            BatchSongMatcher.FindExactCandidates(query, searchAttempt.results),
            preferredArtist);

        if (candidates.Count == 0)
        {
            var message = searchAttempt.errors.Count > 0
                ? string.Join("；", searchAttempt.errors)
                : "网易云与 QQ 音乐均无完全同名结果";
            AddUnresolvedKeyword(query, message);
            return false;
        }

        var providerCandidates = candidates
            .GroupBy(candidate => candidate.Source)
            .Select(group => group.First())
            .ToList();

        var fetched = new List<(BatchSongCandidate candidate, SaveVo saveVo, int score)>();
        var errors = new List<string>();

        foreach (var candidate in providerCandidates)
        {
            var rawInput = new InputSongId(candidate.SongId, candidate.Source, SearchTypeEnum.SONG_ID);
            var inputSongId = new InputSongId(candidate.SongId, rawInput);
            var result = await Task.Run(() => _searchService.SearchSongs([inputSongId], _settingBean));

            if (!result.TryGetValue(candidate.SongId, out var songResult) || !songResult.IsSuccess())
            {
                errors.Add(songResult?.ErrorMsg ?? $"{candidate.Source.ToDescription()} 查询失败");
                continue;
            }

            var saveVo = songResult.Data;
            if (saveVo.LyricVo.IsEmpty())
            {
                errors.Add($"{candidate.Source.ToDescription()} 无歌词");
                continue;
            }

            var score = ScoreFetchedCandidate(candidate, saveVo, preferredArtist);
            fetched.Add((candidate, saveVo, score));
        }

        if (fetched.Count == 0)
        {
            AddUnresolvedKeyword(query, errors.Count == 0 ? "完全同名结果没有可用歌词" : string.Join("；", errors));
            return false;
        }

        var best = fetched
            .OrderByDescending(item => item.score)
            .ThenBy(item => item.candidate.Source == SearchSourceEnum.NET_EASE_MUSIC ? 0 : 1)
            .First();

        AddKeywordResult(query, best.candidate, best.saveVo);
        return true;
    }

    private async Task<bool> ResolveDirectSongAsync(BatchSongQuery query)
    {
        var source = query.DirectSource!.Value;
        var rawInput = new InputSongId(query.DirectSongId!, source, SearchTypeEnum.SONG_ID);
        var inputSongId = new InputSongId(query.DirectSongId!, rawInput);
        var result = await Task.Run(() => _searchService.SearchSongs([inputSongId], _settingBean));

        if (!result.TryGetValue(query.DirectSongId!, out var songResult) || !songResult.IsSuccess())
        {
            AddUnresolvedKeyword(query, songResult?.ErrorMsg ?? $"{source.ToDescription()} 查询失败");
            return false;
        }

        var saveVo = songResult.Data;
        if (saveVo.LyricVo.IsEmpty())
        {
            AddUnresolvedKeyword(query, $"{source.ToDescription()} 无歌词");
            return false;
        }

        if (!BatchSongMatcher.DirectSongMatchesQuery(query, saveVo.SongVo.Name, saveVo.SongVo.Singer))
        {
            var actualArtist = string.Join(_settingBean.Config.SingerSeparator, saveVo.SongVo.Singer);
            AddUnresolvedKeyword(
                query,
                $"直达链接对应“{saveVo.SongVo.Name}” / {actualArtist}，与输入曲目不一致");
            return false;
        }

        var candidate = new BatchSongCandidate(
            source,
            query.DirectSongId!,
            saveVo.SongVo.Name,
            saveVo.SongVo.Singer,
            saveVo.SongVo.Album,
            0);
        AddKeywordResult(query, candidate, saveVo);
        return true;
    }

    private void AddKeywordResult(BatchSongQuery query, BatchSongCandidate candidate, SaveVo saveVo)
    {
        var item = Items.FirstOrDefault(existing =>
            existing.SongId == candidate.SongId && existing.SourceEnum == candidate.Source);

        if (item == null)
        {
            item = new BatchSearchItemViewModel();
            Items.Add(item);
        }

        _saveMap[candidate.SongId] = saveVo;
        item.IsKeywordResult = true;
        item.IsSelected = true;
        item.RequestedName = query.Title;
        item.SongId = candidate.SongId;
        item.SongName = saveVo.SongVo.Name;
        item.SongSource = GetSourceName(candidate.Source);
        item.SourceEnum = candidate.Source;
        item.Singer = string.Join(_settingBean.Config.SingerSeparator, saveVo.SongVo.Singer);
        item.Album = saveVo.SongVo.Album;
        item.TranslationStatus = string.IsNullOrWhiteSpace(saveVo.LyricVo.TranslateLyric) ? "无" : "有";
        item.Status = "Ready";
        item.Error = string.IsNullOrWhiteSpace(saveVo.LyricVo.TranslateLyric) ? "平台未提供译文" : "";
        item.Progress = 100;
    }

    private void AddUnresolvedKeyword(BatchSongQuery query, string error)
    {
        Items.Add(new BatchSearchItemViewModel
        {
            IsKeywordResult = true,
            IsSelected = false,
            RequestedName = query.Title,
            Singer = query.Artist ?? PreferredArtist,
            TranslationStatus = "-",
            Status = "Unresolved",
            Error = error,
            Progress = 0
        });
    }

    private void RemovePreviousKeywordResults()
    {
        foreach (var item in Items.Where(item => item.IsKeywordResult).ToList())
        {
            if (!string.IsNullOrWhiteSpace(item.SongId))
            {
                _saveMap.Remove(item.SongId);
                _songLinkMap.Remove(item.SongId);
            }

            Items.Remove(item);
        }
    }

    private static int ScoreFetchedCandidate(
        BatchSongCandidate candidate,
        SaveVo saveVo,
        string? preferredArtist)
    {
        var score = string.IsNullOrWhiteSpace(saveVo.LyricVo.Lyric) ? 0 : 100;
        if (!string.IsNullOrWhiteSpace(saveVo.LyricVo.TranslateLyric))
        {
            score += 50;
        }

        if (!string.IsNullOrWhiteSpace(saveVo.LyricVo.TransliterationLyric))
        {
            score += 5;
        }

        if (BatchSongMatcher.ArtistMatches(candidate.Artists, preferredArtist))
        {
            score += 20;
        }

        if (candidate.Source == SearchSourceEnum.NET_EASE_MUSIC)
        {
            score += 1;
        }

        return score;
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
            SearchSourceEnum.NET_EASE_MUSIC => "网易云",
            SearchSourceEnum.QQ_MUSIC => "QQ音乐",
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
