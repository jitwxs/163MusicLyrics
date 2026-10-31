using Avalonia.Controls;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Views;

public class FormatConvertWindow : Window
{
    public FormatConvertWindow(SettingBean settingBean)
    {
        LocalizationService.BindWindowTitle(this, "Window.FormatConvert.Title");
        Width = 980;
        Height = 640;
        MinWidth = 860;
        MinHeight = 520;

        DataContext = new FormatConvertViewModel(settingBean);
        Content = new FormatConvertView();
        Icon = Constants.GetIcon("settings");
    }
}

