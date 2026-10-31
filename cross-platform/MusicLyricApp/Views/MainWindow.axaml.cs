using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;
using System.IO;
using System.Linq;
using System.Net;

namespace MusicLyricApp.Views;

public partial class MainWindow : Window, IWindowProvider
{
    public MainWindow() : this(null, null)
    {
    }

    public MainWindow(StorageService? storageService, SearchService? searchService)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this, storageService, searchService);
        
        SearchTextBox.PointerEntered += SearchTextBox_PointerEntered;
        
        // 监听搜索结果变化，更新封面图片
        var viewModel = (MainWindowViewModel)DataContext;
        viewModel.SearchResultViewModel.PropertyChanged += async (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SearchResultViewModel.SongName) && 
                viewModel.SearchResultViewModel.SaveVoMap.Count > 0)
            {
                await UpdateAlbumCover(viewModel);
            }
        };
    }
    
    private async System.Threading.Tasks.Task UpdateAlbumCover(MainWindowViewModel viewModel)
    {
        if (viewModel.SearchResultViewModel.SaveVoMap.Count > 0)
        {
            var saveVo = viewModel.SearchResultViewModel.SaveVoMap.Values.First();
            if (!string.IsNullOrWhiteSpace(saveVo.SongVo.Pics))
            {
                try
                {
                    // 从网络加载封面图片
                    var client = new WebClient();
                    NetworkClientFactory.ConfigureWebClient(client);
                    var imageData = await client.DownloadDataTaskAsync(saveVo.SongVo.Pics);
                    var stream = new MemoryStream(imageData);
                    var bitmap = new Bitmap(stream);
                    AlbumCoverImage.Source = bitmap;
                }
                catch (System.Exception)
                {
                    // 加载失败，使用默认图片
                    AlbumCoverImage.Source = null;
                }
            }
            else
            {
                AlbumCoverImage.Source = null;
            }
        }
    }
    
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!vm.SettingBean.Config.ConfirmBeforeExit)
        {
            vm.SaveConfig();
            return;
        }

        e.Cancel = true;

        var box = MessageBoxManager.GetMessageBoxStandard("退出提示", "你确定要退出吗？", ButtonEnum.YesNo);
        var result = await box.ShowAsync();

        if (result == ButtonResult.Yes)
        {
            vm.SaveConfig();

            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            });
        }
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return await StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task SetTextAsync(string? text)
    {
        await Clipboard?.SetTextAsync(text)!;
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        return await StorageProvider.TryGetFolderFromPathAsync(path);
    }
    
    private async void SearchTextBox_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (!vm.SettingBean.Config.AutoReadClipboard) return;
        
        var message = await Clipboard?.GetTextAsync()!;
        if (message != null)
        {
            vm.SearchParamViewModel.SearchText = message;
        }
    }

    private void PlaybackSlider_OnTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.ExecuteTogglePlayPauseCommand.CanExecute(null))
        {
            vm.ExecuteTogglePlayPauseCommand.Execute(null);
        }
    }
}
