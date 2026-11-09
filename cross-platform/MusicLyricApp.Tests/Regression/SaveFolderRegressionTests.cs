using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform.Storage;
using Moq;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class SaveFolderRegressionTests
{
    private sealed class PickerSettingWindow(TestWorkspace ws) : SettingWindow(ws.Settings)
    {
        public int PickerCalls { get; private set; }
        public FolderPickerOpenOptions? PickerOptions { get; private set; }
        public TaskCompletionSource<IReadOnlyList<IStorageFolder>> PickerResult { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            PickerCalls++;
            PickerOptions = options;
            return PickerResult.Task;
        }
    }

    [AvaloniaTheory]
    [InlineData(false, "empty")]
    [InlineData(true, "empty")]
    [InlineData(false, "cancelled")]
    [InlineData(true, "cancelled")]
    [InlineData(false, "error")]
    [InlineData(true, "error")]
    public async Task BothPickersBelongToSettingsAndCancellationKeepsWindowsOpen(bool cache, string outcome)
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var before = File.ReadAllBytes(ws.ConfigPath);
        var owner = new Window();
        var settings = new PickerSettingWindow(ws);
        var closed = false;
        settings.Closed += (_, _) => closed = true;
        try
        {
            owner.Show();
            settings.Show(owner);
            var vm = Assert.IsType<SettingViewModel>(settings.DataContext);
            var savePath = vm.SettingParamViewModel.SaveFolderPath;
            var cachePath = vm.SettingParamViewModel.SearchCacheFolderPath;
            var tips = vm.SettingTips;
            var expectedCommand = cache ? vm.SelectSearchCacheFolderCommand : vm.SelectSaveFolderCommand;
            settings.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 3;
            Dispatcher.UIThread.RunJobs();
            var button = Assert.Single(settings.GetVisualDescendants().OfType<Button>(),
                x => ReferenceEquals(x.Command, expectedCommand));
            Assert.True(button.IsEffectivelyEnabled);
            var command = Assert.IsAssignableFrom<IAsyncRelayCommand>(button.Command);

            await ExecuteFromUiAsync(command, () =>
            {
                Assert.Equal(1, settings.PickerCalls);
                Assert.Same(owner, settings.Owner);
                Assert.True(settings.IsVisible);
                Assert.True(command.IsRunning);
                Assert.False(command.CanExecute(null));
                Assert.False(command.ExecutionTask!.IsCompleted);
                Assert.NotNull(settings.PickerOptions);
                Assert.False(settings.PickerOptions.AllowMultiple);
                Assert.Equal(LocalizationService.Get(cache ? "Dialog.Folder.Cache" : "Dialog.Folder.Output"),
                    settings.PickerOptions.Title);
                if (outcome == "cancelled") settings.PickerResult.SetCanceled();
                else if (outcome == "error") settings.PickerResult.SetException(new InvalidOperationException("picker unavailable"));
                else settings.PickerResult.SetResult(Array.Empty<IStorageFolder>());
            });

            Assert.True(settings.IsVisible);
            Assert.True(owner.IsVisible);
            Assert.False(closed);
            Assert.Equal(savePath, vm.SettingParamViewModel.SaveFolderPath);
            Assert.Equal(cachePath, vm.SettingParamViewModel.SearchCacheFolderPath);
            if (outcome == "empty") Assert.Equal(tips, vm.SettingTips);
            else if (outcome == "error") Assert.Equal("picker unavailable", vm.SettingTips);
            else Assert.Equal(new TaskCanceledException(settings.PickerResult.Task).Message, vm.SettingTips);
            Assert.Equal(savePath, ws.Settings.Config.LastSaveFolderPath);
            Assert.Equal(cachePath, ws.Settings.Config.SearchCacheFolderPath);
            Assert.Equal(before, File.ReadAllBytes(ws.ConfigPath));
            Assert.Empty(ws.CreatedFiles);
            Assert.False(command.IsRunning);
            Assert.True(command.CanExecute(null));
        }
        finally
        {
            settings.PickerResult.TrySetResult(Array.Empty<IStorageFolder>());
            settings.Close();
            owner.Close();
        }
    }

    [AvaloniaFact]
    public void MainWindowOpensSettingsWithCorrectOwnerAndReusesIt()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var main = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(main.DataContext);
        try
        {
            main.Show();
            vm.ExecuteSettingCommand.Execute(null);
            var settings = Assert.IsType<SettingWindow>(Assert.Single(main.OwnedWindows));
            Assert.Same(main, settings.Owner);
            Assert.True(settings.IsVisible);
            vm.ExecuteSettingCommand.Execute(null);
            Assert.Same(settings, Assert.Single(main.OwnedWindows));
            var firstViewModel = settings.DataContext;
            settings.Close();
            Assert.Empty(main.OwnedWindows);
            Assert.True(main.IsVisible);

            vm.ExecuteSettingCommand.Execute(null);
            var reopened = Assert.IsType<SettingWindow>(Assert.Single(main.OwnedWindows));
            Assert.NotSame(settings, reopened);
            Assert.NotSame(firstViewModel, reopened.DataContext);
            Assert.Same(main, reopened.Owner);
            Assert.True(reopened.IsVisible);
            var closed = 0;
            reopened.Closed += (_, _) => closed++;
            main.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, closed);
            Assert.False(reopened.IsVisible);
            Assert.False(main.IsVisible);
            Assert.Empty(main.OwnedWindows);
        }
        finally
        {
            main.Close();
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Fact]
    public async Task CachePickerFailureIsReportedWithoutEscapingSettingsCommand()
    {
        using var ws = new TestWorkspace();
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ThrowsAsync(new InvalidOperationException("cache picker unavailable"));
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var path = vm.SettingParamViewModel.SearchCacheFolderPath;

        await vm.SelectSearchCacheFolderCommand.ExecuteAsync(null);

        Assert.Equal("cache picker unavailable", vm.SettingTips);
        Assert.Equal(path, vm.SettingParamViewModel.SearchCacheFolderPath);
        Assert.False(File.Exists(ws.ConfigPath));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellingChangeFolderPreservesPathAndSettings(bool emptyPath)
    {
        using var ws = new TestWorkspace();
        if (emptyPath) ws.Settings.Config.LastSaveFolderPath = "";
        ws.Storage().SaveConfig(ws.Settings);
        var before = File.ReadAllBytes(ws.ConfigPath);
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ReturnsAsync(Array.Empty<IStorageFolder>());
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var path = vm.SettingParamViewModel.SaveFolderPath;
        var tips = vm.SettingTips;

        await vm.SelectSaveFolderCommand.ExecuteAsync(null);

        Assert.Equal(path, vm.SettingParamViewModel.SaveFolderPath);
        Assert.Equal(path, ws.Settings.Config.LastSaveFolderPath);
        Assert.Equal(tips, vm.SettingTips);
        Assert.False(vm.SelectSaveFolderCommand.IsRunning);
        Assert.True(vm.SelectSaveFolderCommand.CanExecute(null));
        Assert.Equal(before, File.ReadAllBytes(ws.ConfigPath));
        Assert.Empty(ws.CreatedFiles);
        ws.Window.Verify(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()), Times.Once);
        ws.Window.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FolderPickerFailureIsReportedWithoutEscapingSettingsCommand()
    {
        using var ws = new TestWorkspace();
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ThrowsAsync(new InvalidOperationException("picker unavailable"));
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);

        await vm.SelectSaveFolderCommand.ExecuteAsync(null);

        Assert.Equal("picker unavailable", vm.SettingTips);
        Assert.Equal(ws.Root, vm.SettingParamViewModel.SaveFolderPath);
        Assert.Equal(ws.Root, ws.Settings.Config.LastSaveFolderPath);
        Assert.False(File.Exists(ws.ConfigPath));
    }

    [AvaloniaFact]
    public async Task MainOutputFolderCancellationDoesNotReportSuccessOrChangePath()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var before = File.ReadAllBytes(ws.ConfigPath);
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ReturnsAsync(Array.Empty<IStorageFolder>());
        var vm = new MainWindowViewModel(ws.Window.Object, ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        try
        {
            var path = vm.LastSaveFolderPath;
            var normal = vm.TipNormalMessage;
            var error = vm.TipErrorMessage;

            await vm.ExecuteSelectOutputFolderCommand.ExecuteAsync(null);

            Assert.Equal(path, vm.LastSaveFolderPath);
            Assert.Equal(normal, vm.TipNormalMessage);
            Assert.Equal(error, vm.TipErrorMessage);
            Assert.Equal(before, File.ReadAllBytes(ws.ConfigPath));
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Fact]
    public async Task ExplicitFolderSelectionSuccessPersistsSelectedPathNotRememberedPath()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var path = Path.Combine(ws.Root, "selected output 目录");
        Directory.CreateDirectory(path);
        var folder = new Mock<IStorageFolder>(MockBehavior.Strict);
        folder.SetupGet(x => x.Path).Returns(new Uri(path));
        var pending = new TaskCompletionSource<IReadOnlyList<IStorageFolder>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>())).Returns(pending.Task);

        var selection = ws.Storage().SelectFolderAndRememberAsync(ws.Settings, ws.Window.Object);
        Assert.False(selection.IsCompleted);
        Assert.Equal(ws.Root, ws.Storage().ReadAppConfig().Config.LastSaveFolderPath);
        pending.SetResult(new[] { folder.Object });

        Assert.Same(folder.Object, await selection.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(path, ws.Settings.Config.LastSaveFolderPath);
        Assert.Equal(path, ws.Storage().ReadAppConfig().Config.LastSaveFolderPath);
        Assert.Empty(ws.CreatedFiles);
        ws.Window.Verify(x => x.OpenFolderPickerAsync(It.Is<FolderPickerOpenOptions>(o =>
            !o.AllowMultiple && o.Title == LocalizationService.Get("Dialog.Folder.Output"))), Times.Once);
        ws.Window.VerifyNoOtherCalls();
    }

    [AvaloniaTheory]
    [InlineData("success")]
    [InlineData("empty")]
    [InlineData("cancelled")]
    [InlineData("error")]
    public async Task MainOutputFolderUiCommandHandlesDeferredPickerCompletion(string outcome)
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var before = File.ReadAllBytes(ws.ConfigPath);
        var path = Path.Combine(ws.Root, "new output 目录");
        var folder = new Mock<IStorageFolder>(MockBehavior.Strict);
        folder.SetupGet(x => x.Path).Returns(new Uri(path));
        var pending = new TaskCompletionSource<IReadOnlyList<IStorageFolder>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>())).Returns(pending.Task);
        var vm = new MainWindowViewModel(ws.Window.Object, ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        try
        {
            var normal = vm.TipNormalMessage;
            var error = vm.TipErrorMessage;
            await ExecuteFromUiAsync(vm.ExecuteSelectOutputFolderCommand, () =>
            {
                Assert.False(vm.ExecuteSelectOutputFolderCommand.ExecutionTask!.IsCompleted);
                Assert.Equal(ws.Root, vm.LastSaveFolderPath);
                Assert.Equal(before, File.ReadAllBytes(ws.ConfigPath));
                if (outcome == "success") pending.SetResult(new[] { folder.Object });
                else if (outcome == "empty") pending.SetResult(Array.Empty<IStorageFolder>());
                else if (outcome == "cancelled") pending.SetCanceled();
                else pending.SetException(new InvalidOperationException("main picker unavailable"));
            });

            Assert.Equal(outcome == "success" ? path : ws.Root, vm.LastSaveFolderPath);
            Assert.Equal(vm.LastSaveFolderPath, ws.Storage().ReadAppConfig().Config.LastSaveFolderPath);
            if (outcome == "success")
            {
                Assert.Equal(LocalizationService.Format("Status.Main.SavePathUpdated", path), vm.TipNormalMessage);
                Assert.Empty(vm.TipErrorMessage);
            }
            else
            {
                Assert.Equal(before, File.ReadAllBytes(ws.ConfigPath));
                if (outcome == "empty")
                {
                    Assert.Equal(normal, vm.TipNormalMessage);
                    Assert.Equal(error, vm.TipErrorMessage);
                }
                else
                {
                    Assert.Empty(vm.TipNormalMessage);
                    Assert.Equal(outcome == "error" ? "main picker unavailable" : new TaskCanceledException(pending.Task).Message,
                        vm.TipErrorMessage);
                }
            }
            Assert.False(vm.ExecuteSelectOutputFolderCommand.IsRunning);
            Assert.True(vm.ExecuteSelectOutputFolderCommand.CanExecute(null));
            Assert.Empty(ws.CreatedFiles);
            ws.Window.Verify(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()), Times.Once);
            ws.Window.VerifyNoOtherCalls();
        }
        finally
        {
            pending.TrySetResult(Array.Empty<IStorageFolder>());
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingOwnerWhilePickerPendingClosesSettingsAndCompletionDoesNotReopenIt(bool cache)
    {
        using var ws = new TestWorkspace();
        var owner = new Window();
        var settings = new PickerSettingWindow(ws);
        var closed = 0;
        settings.Closed += (_, _) => closed++;
        try
        {
            owner.Show();
            settings.Show(owner);
            var vm = Assert.IsType<SettingViewModel>(settings.DataContext);
            var tips = vm.SettingTips;
            await ExecuteFromUiAsync(cache ? vm.SelectSearchCacheFolderCommand : vm.SelectSaveFolderCommand, () =>
            {
                Assert.Equal(1, settings.PickerCalls);
                owner.Close();
                Assert.Equal(1, closed);
                Assert.False(settings.IsVisible);
                settings.PickerResult.SetResult(Array.Empty<IStorageFolder>());
            });
            Assert.False(owner.IsVisible);
            Assert.False(settings.IsVisible);
            Assert.Empty(owner.OwnedWindows);
            Assert.Equal(1, closed);
            Assert.Equal(tips, vm.SettingTips);
            Assert.False(File.Exists(ws.ConfigPath));
        }
        finally
        {
            settings.PickerResult.TrySetResult(Array.Empty<IStorageFolder>());
            settings.Close();
            owner.Close();
        }
    }

    // ICommand.Execute adds an async-void exception-forwarding path that ExecuteAsync alone does not test.
    private static async Task ExecuteFromUiAsync(IAsyncRelayCommand command, Action completePicker)
    {
        var unhandled = new List<Exception>();
        void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs args)
        {
            unhandled.Add(args.Exception);
            args.Handled = true;
        }
        Dispatcher.UIThread.UnhandledException += OnUnhandled;
        try
        {
            command.Execute(null);
            var execution = Assert.IsAssignableFrom<Task>(command.ExecutionTask);
            completePicker();
            try
            {
                await execution.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                // Drain the async-void continuation as well as the command's own Task.
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                Dispatcher.UIThread.RunJobs();
            }
            Assert.Empty(unhandled);
            Assert.True(execution.IsCompletedSuccessfully);
        }
        finally
        {
            Dispatcher.UIThread.UnhandledException -= OnUnhandled;
        }
    }

    [Fact]
    public async Task ExplicitFolderSelectionCancellationDoesNotPersistOrThrow()
    {
        using var ws = new TestWorkspace();
        ws.Window.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>()))
            .ReturnsAsync(Array.Empty<IStorageFolder>());

        var folder = await ws.Storage().SelectFolderAndRememberAsync(ws.Settings, ws.Window.Object);

        Assert.Null(folder);
        Assert.Equal(ws.Root, ws.Settings.Config.LastSaveFolderPath);
        Assert.False(File.Exists(ws.ConfigPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpensCurrentPathAndCreatesMissingDirectory(bool exists)
    {
        using var ws = new TestWorkspace();
        var path = Path.Combine(ws.Root, "output with spaces");
        if (exists) Directory.CreateDirectory(path);
        string? opened = null;
        var vm = new SettingViewModel(ws.Settings, openDirectory: value =>
        {
            Assert.True(Directory.Exists(value));
            opened = value;
        });
        vm.SettingParamViewModel.SaveFolderPath = path;

        vm.OpenSaveFolderCommand.Execute(null);

        Assert.Equal(path, opened);
        Assert.Equal(path, ws.Settings.Config.LastSaveFolderPath);
        Assert.Equal(LocalizationService.Format("Status.Settings.OutputPathOpened", path), vm.SettingTips);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPathDoesNotOpenAnything(string path)
    {
        using var ws = new TestWorkspace();
        var called = false;
        var vm = new SettingViewModel(ws.Settings, openDirectory: _ => called = true);
        vm.SettingParamViewModel.SaveFolderPath = path;

        vm.OpenSaveFolderCommand.Execute(null);

        Assert.False(called);
        Assert.Equal(LocalizationService.Get("Status.Settings.OutputPathEmpty"), vm.SettingTips);
    }

    [Fact]
    public void FileInsteadOfDirectoryReportsErrorWithoutOpening()
    {
        using var ws = new TestWorkspace();
        var path = Path.Combine(ws.Root, "file.txt");
        File.WriteAllText(path, "keep");
        var called = false;
        var vm = new SettingViewModel(ws.Settings, openDirectory: _ => called = true);
        vm.SettingParamViewModel.SaveFolderPath = path;

        vm.OpenSaveFolderCommand.Execute(null);

        Assert.False(called);
        Assert.Equal("keep", File.ReadAllText(path));
        Assert.NotEqual(LocalizationService.Format("Status.Settings.OutputPathOpened", path), vm.SettingTips);
        Assert.False(string.IsNullOrWhiteSpace(vm.SettingTips));
    }

    [Fact]
    public void ShellFailureIsReportedWithoutThrowing()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings,
            openDirectory: _ => throw new InvalidOperationException("shell unavailable"));

        vm.OpenSaveFolderCommand.Execute(null);

        Assert.Equal(LocalizationService.Format("Status.Settings.OpenOutputPathFailed", "shell unavailable"),
            vm.SettingTips);
    }

    [AvaloniaFact]
    public void SavePathRowBindsOpenButtonAndReservesSpaceBesideInput()
    {
        using var ws = new TestWorkspace();
        string? opened = null;
        var vm = new SettingViewModel(ws.Settings, openDirectory: path => opened = path);
        var view = new SettingView { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 700 };
        try
        {
            window.Show();
            view.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 3;
            Dispatcher.UIThread.RunJobs();
            var input = Assert.IsType<TextBox>(view.FindControl<TextBox>("SaveFolderPathTextBox"));
            var button = Assert.IsType<Button>(view.FindControl<Button>("OpenSaveFolderButton"));
            var grid = Assert.IsType<Grid>(input.Parent);
            Assert.Equal(3, grid.ColumnDefinitions.Count);
            Assert.Same(grid, button.Parent);
            Assert.Equal(2, Grid.GetColumn(button));
            Assert.Same(vm.OpenSaveFolderCommand, button.Command);
            Assert.Equal(LocalizationService.Get("UI.Extra22"), button.Content);
            Assert.Equal(ws.Root, input.Text);
            Assert.True(button.Bounds.Width > 0);
            Assert.True(input.Bounds.Width < grid.Bounds.Width - button.Bounds.Width);

            input.Text = Path.Combine(ws.Root, "edited path");
            Dispatcher.UIThread.RunJobs();
            button.Command!.Execute(null);
            Assert.Equal(input.Text, opened);
        }
        finally
        {
            window.Close();
        }
    }
}
