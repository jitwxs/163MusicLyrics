using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

public class SettingWindow : Window, IWindowProvider
{
    public SettingWindow(SettingBean settingBean, IWindowProvider? windowProvider = null)
    {
        Width = 600;
        Height = 700;
        LocalizationService.BindWindowTitle(this, "Window.Settings.Title");

        Content = new SettingView();
        DataContext = new SettingViewModel(settingBean, windowProvider ?? this);
        Icon = Constants.GetIcon("settings");
    }

    public virtual async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        try
        {
            // Native dialogs must belong to the settings window, not the main window behind it.
            return await StorageProvider.OpenFolderPickerAsync(options);
        }
        finally
        {
            if (IsVisible)
            {
                Activate();
            }
        }
    }

    public async Task SetTextAsync(string? text)
    {
        if (Clipboard != null)
        {
            await Clipboard.SetTextAsync(text);
        }
    }

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(string path) =>
        StorageProvider.TryGetFolderFromPathAsync(path);
}
