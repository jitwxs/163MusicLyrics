using Avalonia.Controls;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>
/// Settings page. Changes apply to the in-memory settings as they are made and are persisted when the
/// user leaves the page or quits, so the Arc page's save/discard bar is not used.
/// </summary>
public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();

        // Screen readers announce lyric types by name while they are moved.
        LyricTypesList.ItemTitleSelector = item => (item as LyricsTypeEnumModel)?.Name ?? item?.ToString() ?? "";

        ApplyLabels();
        LocalizationService.Instance.PropertyChanged += (_, _) => ApplyLabels();
    }

    private void ApplyLabels()
    {
        SettingsPage.Labels = new SettingsPageLabels
        {
            NavigationName = LocalizationService.Get("Settings.NavigationName")
        };
    }
}
