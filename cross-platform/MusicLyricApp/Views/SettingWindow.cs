using Avalonia.Controls;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

public class SettingWindow : Window
{
    public SettingWindow(SettingBean settingBean, IWindowProvider? windowProvider = null)
    {
        Width = 600;
        Height = 700;
        LocalizationService.BindWindowTitle(this, "Window.Settings.Title");

        Content = new SettingView();
        DataContext = new SettingViewModel(settingBean, windowProvider);
        Icon = Constants.GetIcon("settings");
    }
}
