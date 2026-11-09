using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricApp.Views;

/// <summary>The local search page (本地搜索): scan a folder, match each file to a song, confirm, then save lyrics beside it.</summary>
public partial class LocalMusicScanView : UserControl
{
    public LocalMusicScanView() => InitializeComponent();

    private async void ChooseInputFolder(object? sender, RoutedEventArgs e) => await ChooseFolder(false);
    private async void ChooseOutputFolder(object? sender, RoutedEventArgs e) => await ChooseFolder(true);

    private async Task ChooseFolder(bool output)
    {
        if (DataContext is not LocalMusicScanViewModel vm || vm.IsBusy || TopLevel.GetTopLevel(this) is not { } top) return;
        try
        {
            var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            { Title = LocalizationService.Get(output ? "LocalScan.OutputFolder" : "LocalScan.Folder"), AllowMultiple = false });
            var path = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path)) return;
            if (output) vm.OutputFolder = path; else vm.FolderPath = path;
        }
        catch (Exception ex) { vm.ReportError(ex.Message); }
    }

    private void OpenSongPage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LocalMusicScanViewModel vm || vm.SelectedItem?.Candidate is not { } candidate) return;
        var id = Uri.EscapeDataString(candidate.Id);
        var url = candidate.Source == SearchSourceEnum.NET_EASE_MUSIC
            ? $"https://music.163.com/#/song?id={id}" : $"https://y.qq.com/n/ryqq/songDetail/{id}";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { vm.ReportError(ex.Message); }
    }
}
