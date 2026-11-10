using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;

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
            var path = (await top.StorageProvider.PickFolderAsync(output ? "LocalScan.OutputFolder" : "LocalScan.Folder")).LocalPath();
            if (path == null) return;
            if (output) vm.OutputFolder = path; else vm.FolderPath = path;
        }
        catch (Exception ex) { vm.ReportError(ex.Message); }
    }

    private void OpenSongPage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LocalMusicScanViewModel vm || vm.SelectedItem?.Candidate is not { } candidate) return;
        var url = MusicSources.Get(candidate.Source).SongPageUrl(candidate.Id);
        try { ShellLauncher.Open(url); }
        catch (Exception ex) { vm.ReportError(ex.Message); }
    }
}
