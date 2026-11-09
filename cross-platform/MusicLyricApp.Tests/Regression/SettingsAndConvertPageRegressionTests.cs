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
    public void SettingsPageHasOneSectionPerFormerTab()
    {
        using var ws = new TestWorkspace();
        var view = new SettingView();
        var window = TestWorkspace.Host(view, new SettingViewModel(ws.Settings, ws.Window.Object));
        try
        {
            window.Show();
            var page = Assert.IsType<SettingsPage>(view.FindControl<SettingsPage>("SettingsPage"));
            var headers = page.Items.OfType<SettingsSection>().Select(s => s.Header).ToList();
            Assert.Equal(
                new object[]
                {
                    LocalizationService.Get("UI.LyricCustomization"), LocalizationService.Get("UI.Extensions"),
                    LocalizationService.Get("UI.Network"), LocalizationService.Get("UI.DownloadCache"),
                    LocalizationService.Get("UI.Application")
                },
                headers);
            Assert.Equal(LocalizationService.Get("Settings.NavigationName"), page.Labels.NavigationName);
        }
        finally { window.Close(); }
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
            Assert.False(alert.IsVisible);
            vm.TimestampTipsCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(alert.IsVisible);
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
