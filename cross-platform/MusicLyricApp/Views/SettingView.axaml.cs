using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

/// <summary>
/// Settings page showing one section, picked from the main sidebar. Changes apply to the in-memory settings
/// as they are made and are persisted when the user leaves the settings area or quits.
/// </summary>
public partial class SettingView : UserControl
{
    private SettingViewModel? _viewModel;

    public SettingView()
    {
        InitializeComponent();

        // Screen readers announce lyric types by name while they are moved.
        LyricTypesList.ItemTitleSelector = item => (item as LyricsTypeEnumModel)?.Name ?? item?.ToString() ?? "";
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as SettingViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>A newly chosen section starts at its top, not at the previous section's scroll offset.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingViewModel.CurrentSection))
        {
            SectionScroller.Offset = new Vector(0, 0);
        }
    }
}
