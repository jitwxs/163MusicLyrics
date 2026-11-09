using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>Selectable table of fuzzy search results, shown inside the search page.</summary>
public partial class BlurSearchView : UserControl
{
    private BlurSearchViewModel? _viewModel;

    /// <summary>Raised when the user asks to return to the lyrics preview.</summary>
    public event EventHandler? BackRequested;

    public BlurSearchView()
    {
        AvaloniaXamlLoader.Load(this);
        Table.SelectionChanged += OnSelectionChanged;
        this.FindControl<TKCode.Arc.Button>("BackButton")!.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
    }

    public Table Table => this.FindControl<Table>("ResultsTable")!;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.LanguageChanged -= RebuildColumns;
        }

        _viewModel = DataContext as BlurSearchViewModel;

        if (_viewModel != null)
        {
            _viewModel.LanguageChanged += RebuildColumns;
        }

        RebuildColumns();
    }

    private void RebuildColumns()
    {
        Table.Columns.Clear();
        if (_viewModel == null)
        {
            return;
        }

        foreach (var column in CreateColumns(_viewModel.SearchType))
        {
            Table.Columns.Add(column);
        }
    }

    /// <summary>Columns per result kind, matching what the old results window showed.</summary>
    public static IReadOnlyList<DataTableColumn> CreateColumns(SearchTypeEnum searchType)
    {
        static DataTableColumn Column<T>(string key, Func<T, object?> value, double star, bool numeric = false)
            where T : BlurSearchResultBase => new(LocalizationService.Get(key), item => item is T t ? value(t) : null)
        {
            Width = new GridLength(star, GridUnitType.Star),
            IsNumeric = numeric
        };

        return searchType switch
        {
            SearchTypeEnum.SONG_ID =>
            [
                Column<BlurSongSearchResult>("UI.BlurGrid.Song", x => x.Title, 3),
                Column<BlurSongSearchResult>("UI.BlurGrid.Artist", x => x.Author, 2),
                Column<BlurSongSearchResult>("UI.BlurGrid.Album", x => x.Album, 2),
                Column<BlurSongSearchResult>("UI.BlurGrid.Duration", x => x.Duration, 1),
                Column<BlurSongSearchResult>("UI.BlurGrid.Platform", x => x.SearchSource, 1)
            ],
            SearchTypeEnum.ALBUM_ID =>
            [
                Column<BlurAlbumSearchResult>("UI.BlurGrid.Album", x => x.Title, 3),
                Column<BlurAlbumSearchResult>("UI.BlurGrid.Artist", x => x.Author, 2),
                Column<BlurAlbumSearchResult>("UI.BlurGrid.SongCount", x => ParseCount(x.SongCount), 1, true),
                Column<BlurAlbumSearchResult>("UI.BlurGrid.ReleaseDate", x => x.PublishTime, 1),
                Column<BlurAlbumSearchResult>("UI.BlurGrid.Platform", x => x.SearchSource, 1)
            ],
            SearchTypeEnum.PLAYLIST_ID =>
            [
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.Playlist", x => x.Title, 2),
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.Creator", x => x.Author, 1),
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.Description", x => x.Description, 3),
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.SongCount", x => ParseCount(x.SongCount), 1, true),
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.PlayCount", x => ParseCount(x.PlayCount), 1, true),
                Column<BlurPlaylistSearchResult>("UI.BlurGrid.Platform", x => x.SearchSource, 1)
            ],
            _ => []
        };
    }

    /// <summary>Counts arrive as strings; numbers sort numerically and align right.</summary>
    private static object? ParseCount(string? value) => long.TryParse(value, out var number) ? number : value;

    private void OnSelectionChanged(object? sender, DataTableSelectionChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.SelectedItems = e.SelectedItems.ToList();
        }
    }
}
