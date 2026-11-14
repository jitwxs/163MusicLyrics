using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
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
        viewModel.TipRaised += ShowTip;
        viewModel.BatchSearchViewModel.TipRaised += ShowTip;
        UpdateBatchBadge();
        _ = viewModel.BatchSearchViewModel.LoadFromLocalCacheAsync();

        NavSidebar.ItemInvoked += OnSidebarItemInvoked;
        TitleBarDrag.Attach(this);
        SelectLocalization.Attach(this);
        BindGlobalShortcuts(viewModel);
    }

    /// <summary>Shortcuts that work on every page; search-page ones are bound by <see cref="SearchView"/>.</summary>
    private void BindGlobalShortcuts(MainWindowViewModel vm)
    {
        void Bind(string id, System.Windows.Input.ICommand command, object? parameter = null)
        {
            foreach (var gesture in AppShortcuts.Get(id).AllGestures)
            {
                var binding = new KeyBinding { Gesture = gesture, Command = command };
                if (parameter != null) binding.CommandParameter = parameter;
                KeyBindings.Add(binding);
            }
        }

        Bind(AppShortcuts.FocusSearch, new CommunityToolkit.Mvvm.Input.RelayCommand(FocusSearchBox));
        Bind(AppShortcuts.GoSearch, vm.ExecuteGoToPageCommand, MainWindowViewModel.PageSearch);
        Bind(AppShortcuts.GoBatch, vm.ExecuteGoToPageCommand, MainWindowViewModel.PageBatch);
        Bind(AppShortcuts.GoConvert, vm.ExecuteGoToPageCommand, MainWindowViewModel.PageConvert);
        Bind(AppShortcuts.GoLocal, vm.ExecuteGoToPageCommand, MainWindowViewModel.PageLocal);
        Bind(AppShortcuts.OpenSettings, vm.ExecuteGoToPageCommand, MainWindowViewModel.PageSettings);
        Bind(AppShortcuts.ShowShortcuts, new CommunityToolkit.Mvvm.Input.RelayCommand(ShowShortcuts));
    }

    /// <summary>Switches to the search page and puts the caret in the search box, ready to type.</summary>
    public void FocusSearchBox()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.CurrentPageKey = MainWindowViewModel.PageSearch;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (SearchPage.FindControl<TextBox>("SearchTextBox") is { } box)
            {
                box.Focus();
                box.SelectAll();
            }
        }, DispatcherPriority.Input);
    }

    /// <summary>Opens the 快捷键 settings section.</summary>
    public void ShowShortcuts()
    {
        if (DataContext is MainWindowViewModel vm) vm.CurrentPageKey = MainWindowViewModel.PageShortcuts;
    }

    /// <summary>Help rows are actions: SidebarItem has no DataContext, so they are mapped to commands here.</summary>
    private void OnSidebarItemInvoked(object? sender, SidebarItemInvokedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !e.Item.IsAction)
        {
            return;
        }

        System.Windows.Input.ICommand? command = e.Item.Key switch
        {
            "help/home" => vm.ExecuteHomeCommand,
            "help/wiki" => vm.ExecuteWikiCommand,
            "help/issue" => vm.ExecuteIssueCommand,
            _ => null
        };

        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }

    /// <summary>Status messages appear as toasts; errors stay longer and read as errors, not only by colour.</summary>
    private void ShowTip(string message, bool isError)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // "Needs login" errors offer the way to fix them: the cookie fields in settings.
        var action = MainWindowViewModel.IsNeedLoginMessage(message) && DataContext is MainWindowViewModel vm
            ? new ToastAction(LocalizationService.Get("Login.OpenSettings"), () => vm.ExecuteOpenCookieSettingsCommand.Execute(null))
            : null;
        Dispatcher.UIThread.Post(() => Toast.Show(this, message, isError ? ToastKind.Error : ToastKind.Info, action: action));
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

        // Unsaved lyric edits always ask, even with the exit confirmation turned off.
        var unsavedEdits = vm.SearchResultViewModel.HasUnsavedEdits;
        if (_exitConfirmed || (!vm.SettingBean.Config.ConfirmBeforeExit && !unsavedEdits))
        {
            // Stops a running local scan, match or save; finished results are already on disk.
            vm.LocalMusicScanViewModel?.Dispose();
            vm.SaveConfig();
            return;
        }

        e.Cancel = true;

        var confirmed = await DialogHelper.ConfirmOnAsync(this,
            LocalizationService.Get("Dialog.Exit.Title"),
            LocalizationService.Get(unsavedEdits ? "Editor.ExitWithEdits" : "Dialog.Exit.Confirm"),
            LocalizationService.Get("Dialog.Exit.Action"), isDanger: unsavedEdits);

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
