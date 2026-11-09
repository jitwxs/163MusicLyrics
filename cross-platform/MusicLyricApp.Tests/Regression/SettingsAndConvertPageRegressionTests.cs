using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;
using TKCode.Arc;

namespace MusicLyricAppTest.Regression;

/// <summary>The Arc settings and format convert pages, and shared controls themed by Arc alone.</summary>
public class SettingsAndConvertPageRegressionTests
{
    [AvaloniaFact]
    public void SettingsSectionsAreSidebarChildrenAndShareOneViewModel()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Settings.Config.OutputLyricTypes = "0,1";
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(Moq.MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var sidebar = Assert.Single(window.GetVisualDescendants().OfType<Sidebar>());
            var parent = sidebar.Sections.SelectMany(s => s.Items).Single(i => i.Key == "settings");
            Assert.Equal(SettingViewModel.Sections.Select(x => MainWindowViewModel.SettingsKeyPrefix + x),
                parent.Items.Select(i => i.Key));

            // The page has no navigation of its own any more.
            var view = window.FindControl<SettingView>("SettingsPage")!;
            Assert.Empty(view.GetVisualDescendants().OfType<SettingsPage>());

            sidebar.SelectedKey = MainWindowViewModel.SettingsKeyPrefix + SettingViewModel.SectionNetwork;
            Dispatcher.UIThread.RunJobs();
            var settings = Assert.IsType<SettingViewModel>(vm.SettingViewModel);
            Assert.True(vm.IsSettingsPage);
            Assert.True(view.FindControl<StackPanel>("NetworkSection")!.IsVisible);
            Assert.False(view.FindControl<StackPanel>("LyricsSection")!.IsVisible);

            // Moving between sections keeps edits in the same view model; leaving settings applies them.
            settings.SelectedLyricsTypeItem = settings.LyricsTypes[1];
            settings.MoveUpCommand.Execute(null);
            sidebar.SelectedKey = MainWindowViewModel.SettingsKeyPrefix + SettingViewModel.SectionDownload;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(settings, vm.SettingViewModel);
            Assert.True(view.FindControl<StackPanel>("DownloadSection")!.IsVisible);
            Assert.Equal("0,1", vm.SettingBean.Config.OutputLyricTypes);

            vm.CurrentPageKey = MainWindowViewModel.PageSearch;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("1,0", vm.SettingBean.Config.OutputLyricTypes);

            // An unknown section falls back to the first one.
            vm.CurrentPageKey = MainWindowViewModel.SettingsKeyPrefix + "missing";
            Assert.Equal(MainWindowViewModel.PageSettings, vm.CurrentPageKey);
            Assert.Equal(SettingViewModel.SectionLyrics, vm.SettingViewModel!.CurrentSection);
        }
        finally { window.Close(); CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public void HelpActionsLiveInTheSidebarAndDoNotChangePage()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(Moq.MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        var previous = MusicLyricApp.Core.DialogHelper.OwnerResolver;
        MusicLyricApp.Core.DialogHelper.OwnerResolver = () => window;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Null(window.TitleBar!.RightContent);
            var sidebar = Assert.Single(window.GetVisualDescendants().OfType<Sidebar>());
            var help = sidebar.Sections.Single(s => Equals(s.Header, LocalizationService.Get("UI.Help")));
            Assert.Equal(new[] { "help/home", "help/wiki", "help/issue", "help/shortcuts" }, help.Items.Select(i => i.Key));
            Assert.All(help.Items, i => Assert.True(i.IsAction));

            var shortcuts = help.Items.Single(i => i.Key == "help/shortcuts");
            sidebar.RaiseEvent(new SidebarItemInvokedEventArgs(Sidebar.ItemInvokedEvent, shortcuts));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageSearch, vm.CurrentPageKey);
            var layer = Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(window)!;
            // The dialog lists every shortcut from the single shortcut table.
            var texts = layer.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.All(MusicLyricApp.Core.AppShortcuts.All, sc => Assert.Contains(sc.Label, texts));
        }
        finally
        {
            MusicLyricApp.Core.DialogHelper.OwnerResolver = previous;
            window.Close();
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [AvaloniaFact]
    public void TitleBarDragMovesTheWindowOffWindows()
    {
        if (!TitleBarDrag.IsNeeded) return;
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(Moq.MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var bar = window.TitleBar!;
            var start = window.Position;
            // The bar spans the top of the window; grab its empty middle.
            var grab = new Avalonia.Point(window.Bounds.Width / 2, bar.Bounds.Height / 2);
            Avalonia.Headless.HeadlessWindowExtensions.MouseDown(window, grab, Avalonia.Input.MouseButton.Left);
            Avalonia.Headless.HeadlessWindowExtensions.MouseMove(window, grab + new Avalonia.Point(40, 20));
            Avalonia.Headless.HeadlessWindowExtensions.MouseUp(window, grab + new Avalonia.Point(40, 20), Avalonia.Input.MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.NotEqual(start, window.Position);
        }
        finally { window.Close(); CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public void LyricTypesReorderAndToggleThroughTheList()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.OutputLyricTypes = "0,1";
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var list = Assert.IsType<ReorderableList>(view.FindControl<ReorderableList>("LyricTypesList"));
            Assert.Same(vm.LyricsTypes, list.ItemsSource);
            Assert.Equal(vm.LyricsTypes[0].Name, list.ItemTitleSelector!(vm.LyricsTypes[0]));

            // Moving in the list moves the bound collection, which is the order that gets saved.
            var first = vm.LyricsTypes[0];
            list.MoveItem(0, 1);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(first, vm.LyricsTypes[1]);

            // Each row's checkbox flips whether that type is output.
            var third = vm.LyricsTypes[2];
            Assert.False(third.IsSelected);
            var box = Assert.Single(view.GetVisualDescendants().OfType<Checkbox>(), c => Equals(c.Content, third.Name));
            box.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(third.IsSelected);

            vm.OnClosing();
            Assert.Equal($"1,0,{third.Id}", ws.Settings.Config.OutputLyricTypes);
        }
        finally { window.Close(); }
    }

    [Fact]
    public void ShortcutTableHasUniqueGesturesAndPlatformModifier()
    {
        var all = MusicLyricApp.Core.AppShortcuts.All;
        Assert.Equal(all.Count, all.Select(s => s.Id).Distinct().Count());
        // No two shortcuts in reach at the same time share a key combination.
        foreach (var scope in new[] { MusicLyricApp.Core.ShortcutScope.Search, MusicLyricApp.Core.ShortcutScope.Global })
        {
            var reachable = all.Where(s => s.Scope == scope || s.Scope == MusicLyricApp.Core.ShortcutScope.Global)
                .SelectMany(s => s.AllGestures).Select(g => g.ToString()).ToList();
            Assert.Equal(reachable.Count, reachable.Distinct().Count());
        }

        var save = MusicLyricApp.Core.AppShortcuts.Get(MusicLyricApp.Core.AppShortcuts.Save);
        Assert.Equal(MusicLyricApp.Core.AppShortcuts.CommandModifier, save.Gesture.KeyModifiers);
        Assert.Equal(OperatingSystem.IsMacOS() ? "⌘ S" : "Ctrl + S", save.KeysText);
        // Earlier releases used Ctrl everywhere; it keeps working on macOS.
        Assert.Equal(OperatingSystem.IsMacOS(), save.Aliases.Any(g => g.KeyModifiers == Avalonia.Input.KeyModifiers.Control));
    }

    [AvaloniaFact]
    public void GlobalShortcutsSwitchPagesAndFocusSearchFromAnywhere()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(Moq.MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            void Press(string id)
            {
                var g = MusicLyricApp.Core.AppShortcuts.Get(id).Gesture;
                var raw = Avalonia.Input.RawInputModifiers.None;
                if (g.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta)) raw |= Avalonia.Input.RawInputModifiers.Meta;
                if (g.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control)) raw |= Avalonia.Input.RawInputModifiers.Control;
                Avalonia.Headless.HeadlessWindowExtensions.KeyPress(window, g.Key, raw, Avalonia.Input.PhysicalKey.None, null);
                Avalonia.Headless.HeadlessWindowExtensions.KeyRelease(window, g.Key, raw, Avalonia.Input.PhysicalKey.None, null);
                Dispatcher.UIThread.RunJobs();
            }

            Press(MusicLyricApp.Core.AppShortcuts.GoBatch);
            Assert.Equal(MainWindowViewModel.PageBatch, vm.CurrentPageKey);
            Press(MusicLyricApp.Core.AppShortcuts.GoConvert);
            Assert.Equal(MainWindowViewModel.PageConvert, vm.CurrentPageKey);
            Press(MusicLyricApp.Core.AppShortcuts.OpenSettings);
            Assert.True(vm.IsSettingsPage);

            Press(MusicLyricApp.Core.AppShortcuts.FocusSearch);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageSearch, vm.CurrentPageKey);
            var box = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "SearchTextBox");
            Assert.True(box.IsFocused);
        }
        finally { window.Close(); CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public void SettingTipsCloseAndReturnWithTheNextTip()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            var alert = Assert.IsType<Alert>(view.FindControl<Alert>("SettingTipsAlert"));
            Assert.True(alert.IsDismissible);
            vm.OutputTipsCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(alert.IsOpen);

            alert.IsOpen = false;   // what the close button does
            Dispatcher.UIThread.RunJobs();
            Assert.False(vm.HasSettingTips);

            vm.TimestampTipsCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(alert.IsOpen);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PathRowsStackAndApplicationSectionHasNoStaticShortcutList()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object) { CurrentSection = SettingViewModel.SectionDownload };
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var paths = view.GetVisualDescendants().OfType<SettingsRow>().Where(r => r.Classes.Contains("path") && r.IsEffectivelyVisible).ToList();
            Assert.Equal(2, paths.Count);
            Assert.All(paths, r => Assert.True(r.IsStacked));

            vm.CurrentSection = SettingViewModel.SectionApplication;
            Dispatcher.UIThread.RunJobs();
            var headers = view.GetVisualDescendants().OfType<SettingsGroup>().Where(g => g.IsEffectivelyVisible).Select(g => g.Header).ToList();
            Assert.DoesNotContain(LocalizationService.Get("UI.Shortcuts"), headers);
            var config = Assert.Single(view.GetVisualDescendants().OfType<SettingsRow>(), r => r.Classes.Contains("path") && r.IsEffectivelyVisible);
            Assert.True(config.IsStacked);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LongCookieKeepsTheFieldAtTheRowControlWidth()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.NetEaseCookie = string.Concat(Enumerable.Repeat("MUSIC_U=0123456789abcdef; ", 40));
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object) { CurrentSection = SettingViewModel.SectionExtensions };
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var fields = view.GetVisualDescendants().OfType<PasswordField>().Where(f => f.IsEffectivelyVisible).ToList();
            Assert.NotEmpty(fields);
            // A long value scrolls inside the field instead of widening the row (and pushing its label away).
            Assert.All(fields, f => Assert.Equal(280, f.Bounds.Width, 1));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InvalidProxyPortShowsOnTheField()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            var port = Assert.IsType<Input>(view.FindControl<Input>("ProxyPortInput"));
            vm.SettingParamViewModel.ProxyPortText = "70000";
            Dispatcher.UIThread.RunJobs();
            Assert.False(string.IsNullOrEmpty(port.ErrorMessage));
            Assert.True(port.IsInvalid);
            vm.SettingParamViewModel.ProxyPortText = "8080";
            Dispatcher.UIThread.RunJobs();
            Assert.True(string.IsNullOrEmpty(port.ErrorMessage));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SettingTipsAppearInAnAlert()
    {
        using var ws = new TestWorkspace();
        var vm = new SettingViewModel(ws.Settings, ws.Window.Object);
        var view = new SettingView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            var alert = Assert.IsType<Alert>(view.FindControl<Alert>("SettingTipsAlert"));
            vm.SettingTips = "";
            Dispatcher.UIThread.RunJobs();
            Assert.False(alert.IsOpen);
            vm.TimestampTipsCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(alert.IsOpen);
            Assert.Contains(alert.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == vm.SettingTips);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task ConvertPageShowsResultEmptyStateAndFailures()
    {
        using var ws = new TestWorkspace();
        var vm = new FormatConvertViewModel(ws.Settings);
        var view = new FormatConvertView();
        var window = TestWorkspace.Host(view, vm);
        try
        {
            window.Show();
            var empty = view.FindControl<EmptyState>("OutputEmptyState")!;
            var output = view.FindControl<CodeBlock>("OutputBlock")!;
            var alert = view.FindControl<Alert>("TipAlert")!;
            Assert.True(empty.IsVisible);
            Assert.False(alert.IsVisible);

            vm.InputText = "[00:01.000]hello";
            await WaitUntil(() => vm.HasOutput);
            Assert.False(empty.IsVisible);
            Assert.Contains("00:00:01,000", output.Code);
            Assert.False(vm.IsTipError);

            vm.SourceFormatItem = vm.Formats.Single(x => x.Value == OutputFormatEnum.SRT);
            vm.TargetFormatItem = vm.Formats.Single(x => x.Value == OutputFormatEnum.LRC);
            vm.InputText = "1\ninvalid --> 00:00:01,000\nhello";
            await WaitUntil(() => vm.IsTipError);
            Assert.True(alert.IsVisible);
            Assert.Equal(vm.TipMessage, alert.Title);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SharedControlsAreThemedWithoutFluent()
    {
        var scroll = new ScrollViewer { Content = new TextBlock { Text = "x" } };
        var window = new Window { Content = scroll, Width = 200, Height = 200 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            // A templated ScrollViewer has a content presenter; an unthemed one renders nothing.
            Assert.NotEmpty(scroll.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>());
            Assert.Contains(scroll.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "x");
        }
        finally { window.Close(); }
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.True(condition());
    }
}
