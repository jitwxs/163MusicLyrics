using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>The single app window: Arc title bar, navigation sidebar and the page host.</summary>
public partial class MainWindow : ArcWindow, IWindowProvider
{
    private bool _exitConfirmed;

    public MainWindow() : this(null, null)
    {
    }

    public MainWindow(StorageService? storageService, SearchService? searchService)
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel(this, storageService, searchService);
        DataContext = viewModel;

        viewModel.BatchSearchViewModel.Items.CollectionChanged += BatchItemsOnCollectionChanged;
        UpdateBatchBadge();
    }

    private void BatchItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateBatchBadge();

    /// <summary>SidebarItem is not in the visual tree, so its badge is set here rather than bound.</summary>
    private void UpdateBatchBadge()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var item = NavSidebar.Sections.SelectMany(s => s.Items)
            .FirstOrDefault(i => i.Key == MainWindowViewModel.PageBatch);
        if (item != null)
        {
            var count = vm.BatchSearchViewModel.Items.Count;
            item.Badge = count > 0 ? count.ToString() : null;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (_exitConfirmed || !vm.SettingBean.Config.ConfirmBeforeExit)
        {
            vm.SaveConfig();
            return;
        }

        e.Cancel = true;

        var confirmed = await Dialog.ConfirmAsync(this,
            LocalizationService.Get("Dialog.Exit.Title"),
            LocalizationService.Get("Dialog.Exit.Confirm"),
            LocalizationService.Get("Dialog.Exit.Action"),
            LocalizationService.Get("Dialog.Action.Cancel"));

        if (!confirmed)
        {
            return;
        }

        _exitConfirmed = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
            else
            {
                Close();
            }
        });
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return await StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task SetTextAsync(string? text)
    {
        if (Clipboard != null)
        {
            await Clipboard.SetTextAsync(text);
        }
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        return await StorageProvider.TryGetFolderFromPathAsync(path);
    }
}
