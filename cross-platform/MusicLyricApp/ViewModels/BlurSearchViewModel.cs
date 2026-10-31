using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels.Messages;

namespace MusicLyricApp.ViewModels;

public partial class BlurSearchViewModel(List<SearchResultVo> searchResList) : ViewModelBase
{
    private bool _countNotificationsAttached;

    [ObservableProperty] private ObservableCollection<BlurSearchResultBase> _searchResults = [];

    // 通知 View 创建列
    public event Action<List<DataGridTextColumn>>? ColumnsChanged;

    [ObservableProperty] private IList _selectedItems = new List<object>();

    public string SearchResultCountText => LocalizationService.Format("UI.Blur.ResultCount", SearchResults.Count);
    public string SelectedItemsCountText => LocalizationService.Format("UI.Blur.SelectedCount", SelectedItems.Count);

    partial void OnSelectedItemsChanged(IList value) => OnPropertyChanged(nameof(SelectedItemsCountText));

    public void LoadTypeAResults()
    {
        if (!_countNotificationsAttached)
        {
            _countNotificationsAttached = true;
            SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SearchResultCountText));
            LocalizationService.Instance.PropertyChanged += (_, _) =>
            {
                foreach (var item in SearchResults) item.RefreshLocalizedSource();
                OnPropertyChanged(nameof(SearchResultCountText));
                OnPropertyChanged(nameof(SelectedItemsCountText));
                if (SearchResults.FirstOrDefault() is BlurSongSearchResult)
                    ColumnsChanged?.Invoke(BlurSongSearchResult.GetDataGridColumns());
                else if (SearchResults.FirstOrDefault() is BlurAlbumSearchResult)
                    ColumnsChanged?.Invoke(BlurAlbumSearchResult.GetDataGridColumns());
                else if (SearchResults.FirstOrDefault() is BlurPlaylistSearchResult)
                    ColumnsChanged?.Invoke(BlurPlaylistSearchResult.GetDataGridColumns());
            };
        }

        var searchType = searchResList[0].SearchType;
        switch (searchType)
        {
            case SearchTypeEnum.SONG_ID:
                foreach (var searchResultVo in searchResList)
                {
                    var searchSource = searchResultVo.SearchSource;
                    foreach (var songVo in searchResultVo.SongVos)
                    {
                        SearchResults.Add(new BlurSongSearchResult(searchSource, songVo));
                    }
                }

                ColumnsChanged?.Invoke(BlurSongSearchResult.GetDataGridColumns());
                break;
            case SearchTypeEnum.ALBUM_ID:
                foreach (var searchResultVo in searchResList)
                {
                    var searchSource = searchResultVo.SearchSource;
                    foreach (var albumVo in searchResultVo.AlbumVos)
                    {
                        SearchResults.Add(new BlurAlbumSearchResult(searchSource, albumVo));
                    }
                }

                ColumnsChanged?.Invoke(BlurAlbumSearchResult.GetDataGridColumns());
                break;
            case SearchTypeEnum.PLAYLIST_ID:
                foreach (var searchResultVo in searchResList)
                {
                    var searchSource = searchResultVo.SearchSource;
                    foreach (var playlistVo in searchResultVo.PlaylistVos)
                    {
                        SearchResults.Add(new BlurPlaylistSearchResult(searchSource, playlistVo));
                    }
                }

                ColumnsChanged?.Invoke(BlurPlaylistSearchResult.GetDataGridColumns());
                break;
            default:
                throw new MusicLyricException(ErrorMsgConst.FUNCTION_NOT_SUPPORT);
        }
    }

    [RelayCommand]
    private void DownloadSelected()
    {
        var ids = string.Join(',', SelectedItems
            .OfType<BlurSearchResultBase>()
            .Where(e => !string.IsNullOrEmpty(e.Id))
            .Select(e => e.Id));
        
        // send message to main window
        WeakReferenceMessenger.Default.Send(new BlurSearchResultsMessage(ids));
        // closing window
        WeakReferenceMessenger.Default.Send(new CloseWindowMessage("BlurSearchWindow"));
    }
}
