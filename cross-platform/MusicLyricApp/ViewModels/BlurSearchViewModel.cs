using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels.Messages;

namespace MusicLyricApp.ViewModels;

/// <summary>Fuzzy search results shown inside the search page; the view picks columns from <see cref="SearchType"/>.</summary>
public partial class BlurSearchViewModel(List<SearchResultVo> searchResList) : ViewModelBase
{
    private bool _countNotificationsAttached;

    [ObservableProperty] private ObservableCollection<BlurSearchResultBase> _searchResults = [];

    /// <summary>Kind of the loaded results (song, album or playlist); decides the table columns.</summary>
    [ObservableProperty] private SearchTypeEnum _searchType;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadSelectedCommand))]
    private IList _selectedItems = new List<object>();

    public string SearchResultCountText => LocalizationService.Format("UI.Blur.ResultCount", SearchResults.Count);
    public string SelectedItemsCountText => LocalizationService.Format("UI.Blur.SelectedCount", SelectedItems.Count);

    partial void OnSelectedItemsChanged(IList value) => OnPropertyChanged(nameof(SelectedItemsCountText));

    public void LoadTypeAResults()
    {
        if (!_countNotificationsAttached)
        {
            _countNotificationsAttached = true;
            SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SearchResultCountText));
            LocalizationService.Observe(this, static vm =>
            {
                foreach (var item in vm.SearchResults) item.RefreshLocalizedSource();
                vm.OnPropertyChanged(nameof(SearchResultCountText));
                vm.OnPropertyChanged(nameof(SelectedItemsCountText));
            });
        }

        SearchType = searchResList[0].SearchType;
        switch (SearchType)
        {
            case SearchTypeEnum.SONG_ID:
                foreach (var searchResultVo in searchResList)
                {
                    foreach (var songVo in searchResultVo.SongVos)
                    {
                        SearchResults.Add(new BlurSongSearchResult(searchResultVo.SearchSource, songVo));
                    }
                }
                break;
            case SearchTypeEnum.ALBUM_ID:
                foreach (var searchResultVo in searchResList)
                {
                    foreach (var albumVo in searchResultVo.AlbumVos)
                    {
                        SearchResults.Add(new BlurAlbumSearchResult(searchResultVo.SearchSource, albumVo));
                    }
                }
                break;
            case SearchTypeEnum.PLAYLIST_ID:
                foreach (var searchResultVo in searchResList)
                {
                    foreach (var playlistVo in searchResultVo.PlaylistVos)
                    {
                        SearchResults.Add(new BlurPlaylistSearchResult(searchResultVo.SearchSource, playlistVo));
                    }
                }
                break;
            default:
                throw new MusicLyricException(ErrorMsgConst.FUNCTION_NOT_SUPPORT);
        }
    }

    private bool CanDownloadSelected() => SelectedItems.Count > 0;

    [RelayCommand(CanExecute = nameof(CanDownloadSelected))]
    private void DownloadSelected()
    {
        var ids = string.Join(',', SelectedItems
            .OfType<BlurSearchResultBase>()
            .Where(e => !string.IsNullOrEmpty(e.Id))
            .Select(e => e.Id));

        // Ask the main view model to fetch the lyrics, then leave the results view.
        WeakReferenceMessenger.Default.Send(new BlurSearchResultsMessage(ids));
        WeakReferenceMessenger.Default.Send(new CloseWindowMessage("BlurSearchWindow"));
    }
}
