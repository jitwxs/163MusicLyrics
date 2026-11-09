using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class MainFlowRegressionTests
{
    // Headless tests must never initialize a physical audio device or fetch audio.
    private sealed class HeadlessMainViewModel(TestWorkspace ws, SearchService search)
        : MainWindowViewModel(ws.Window.Object, ws.Storage(), search)
    {
        public override bool IsPlaybackSupported => false;
    }

    [AvaloniaFact]
    public async Task SearchDownloadAndSaveFlowThroughMainCommands()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var song = TestWorkspace.Song();
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        api.Setup(x => x.GetSongVo(It.IsAny<string[]>())).Returns(new Dictionary<string, ResultVo<SongVo>> { ["123"] = new(song.SongVo) });
        api.Setup(x => x.GetLyricVo("123", "123", false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
        api.Setup(x => x.GetSongLink("123")).Returns(new ResultVo<string>("https://test.invalid/song.mp3"));
        var vm = new HeadlessMainViewModel(ws, ws.Search(api));
        try
        {
            vm.SearchParamViewModel.SearchText = "123";
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.Empty(vm.TipErrorMessage);
            Assert.Equal("测试歌曲123", vm.SearchResultViewModel.SongName);
            Assert.Equal("https://test.invalid/song.mp3", vm.SearchResultViewModel.SongLink);
            Assert.True(vm.HasSongLink);
            await vm.ExecuteSaveCommand.ExecuteAsync(null);
            Assert.Equal("[00:01.000]测试歌词", File.ReadAllText(Assert.Single(ws.CreatedFiles)));
            Assert.Contains("成功 1", vm.TipNormalMessage);
            // An output-format change must not erase the loaded song or its playable link.
            vm.SearchParamViewModel.SelectedOutputFormatItem = vm.SearchParamViewModel.OutputFormats.Single(x => x.Value == OutputFormatEnum.SRT);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("00:00:01,000 -->", vm.SearchResultViewModel.ConsoleOutput);
            Assert.Equal("测试歌曲123", vm.SearchResultViewModel.SongName);
            Assert.Equal("https://test.invalid/song.mp3", vm.SearchResultViewModel.SongLink);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public async Task MainCommandReportsInvalidSearchAndPlaybackCapability()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var vm = new MainWindowViewModel(ws.Window.Object, ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        try
        {
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.False(string.IsNullOrWhiteSpace(vm.TipErrorMessage));
            Assert.Empty(vm.SearchResultViewModel.SaveVoMap);
            Assert.Equal(OperatingSystem.IsWindows(), vm.IsPlaybackSupported);
            Assert.False(vm.HasSongLink);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public void MainWindowLoadsControlsAndClosesWithIsolatedSettings()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Assert.Single(window.GetVisualDescendants().OfType<TextBox>(), x => x.Name == "SearchTextBox");
            Assert.True(window.IsVisible);
            Assert.Contains("MusicLyricApp", vm.AppTitle);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
        Assert.False(window.IsVisible);
        Assert.False(ws.Storage().ReadAppConfig().Config.ConfirmBeforeExit);
    }

    [AvaloniaFact]
    public void SidebarNavigationSwitchesPagesAndFlushesSettingsOnLeave()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Settings.Config.OutputLyricTypes = "0,1";
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var sidebar = Assert.Single(window.GetVisualDescendants().OfType<TKCode.Arc.Sidebar>());
            Control Page(string name) => window.FindControl<Control>(name)!;

            Assert.Equal(MainWindowViewModel.PageSearch, sidebar.SelectedKey);
            Assert.True(Page("SearchPage").IsVisible);
            Assert.False(Page("SettingsPage").IsVisible);

            // Choosing a sidebar row drives the view model.
            sidebar.SelectedKey = MainWindowViewModel.PageSettings;
            Dispatcher.UIThread.RunJobs();
            Assert.True(vm.IsSettingsPage);
            Assert.True(Page("SettingsPage").IsVisible);
            Assert.False(Page("SearchPage").IsVisible);
            var settings = Assert.IsType<SettingViewModel>(vm.SettingViewModel);
            Assert.Same(settings, Page("SettingsPage").DataContext);
            settings.SelectedLyricsTypeItem = settings.LyricsTypes[1];
            settings.MoveUpCommand.Execute(null);
            Assert.Equal("0,1", vm.SettingBean.Config.OutputLyricTypes);

            // Commands drive the sidebar, and leaving settings applies them like closing the old window did.
            vm.ExecuteFormatConvertCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageConvert, sidebar.SelectedKey);
            Assert.Equal("1,0", vm.SettingBean.Config.OutputLyricTypes);
            Assert.True(Page("ConvertPage").IsVisible);
            Assert.Same(vm.FormatConvertViewModel, Page("ConvertPage").DataContext);

            vm.ExecuteOpenDownloadManagerCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindowViewModel.PageBatch, sidebar.SelectedKey);
            Assert.True(Page("BatchPage").IsVisible);
            Assert.Same(vm.BatchSearchViewModel, Page("BatchPage").DataContext);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [AvaloniaFact]
    public void ClosingWithConfirmationShowsArcDialogAndKeepsWindowOpen()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = true;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.IsVisible);
            // Arc dialogs render their surface on the window's overlay layer.
            var layer = Assert.IsAssignableFrom<Avalonia.Controls.Primitives.OverlayLayer>(
                Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(window));
            var texts = layer.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(LocalizationService.Get("Dialog.Exit.Title"), texts);
            Assert.Contains(LocalizationService.Get("Dialog.Exit.Confirm"), texts);
            Assert.Contains(LocalizationService.Get("Dialog.Exit.Action"), texts);
            Assert.Contains(LocalizationService.Get("Dialog.Action.Cancel"), texts);
        }
        finally
        {
            vm.SettingBean.Config.ConfirmBeforeExit = false;
            window.Close();
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [AvaloniaFact]
    public async Task DialogHelperWithoutOwnerDoesNotThrowOrConfirm()
    {
        var previous = MusicLyricApp.Core.DialogHelper.OwnerResolver;
        MusicLyricApp.Core.DialogHelper.OwnerResolver = () => null;
        try
        {
            await MusicLyricApp.Core.DialogHelper.ShowMessage("notice");
            Assert.False(await MusicLyricApp.Core.DialogHelper.ConfirmAsync("t", "m", "ok"));
            Assert.False(await MusicLyricApp.Core.DialogHelper.ShowReleaseNotesAsync("notes"));
        }
        finally { MusicLyricApp.Core.DialogHelper.OwnerResolver = previous; }
    }

    [AvaloniaFact]
    public void BatchSidebarBadgeFollowsItemCount()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var window = new MainWindow(ws.Storage(), ws.Search(new(MockBehavior.Strict)));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            var sidebar = Assert.Single(window.GetVisualDescendants().OfType<TKCode.Arc.Sidebar>());
            var batch = sidebar.Sections.SelectMany(s => s.Items).Single(i => i.Key == MainWindowViewModel.PageBatch);
            Assert.Null(batch.Badge);
            vm.BatchSearchViewModel.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()) }, [TestWorkspace.Input()]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("1", batch.Badge);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("65536", false)]
    [InlineData("abc", false)]
    [InlineData("1", true)]
    [InlineData("65535", true)]
    public void ProxyPortValidation(string value, bool valid)
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);
        vm.ProxyPortText = value;
        Assert.Equal(valid, string.IsNullOrEmpty(vm.ProxyPortError));
        Assert.Equal(valid ? int.Parse(value) : 80, ws.Settings.Config.ProxyPort);
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("2049", false)]
    [InlineData("abc", false)]
    [InlineData("1", true)]
    [InlineData("2048", true)]
    public void CacheSizeValidation(string value, bool valid)
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);
        vm.SearchCacheMaxSizeMbText = value;
        Assert.Equal(valid, string.IsNullOrEmpty(vm.SearchCacheSizeError));
        Assert.Equal(valid ? int.Parse(value) : 128, ws.Settings.Config.SearchCacheMaxSizeMb);
    }

    // Language changes notify UI subscribers left by other windows; keep Avalonia services active.
    [AvaloniaTheory]
    [InlineData("zh-CN")]
    [InlineData("en-US")]
    public async Task BatchSkippedSongsMustNotBeMarkedSaved(string language)
    {
        var originalLanguage = LocalizationService.Instance.Language;
        var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            LocalizationService.Instance.SetLanguage(language);
            using var ws = new TestWorkspace();
            var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
            vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()), ["456"] = new(TestWorkspace.Song("456", "")) }, [TestWorkspace.Input(), TestWorkspace.Input("456")]);
            vm.ExecuteSelectAllCommand.Execute(null);
            await vm.ExecuteSaveSelectedCommand.ExecuteAsync(null);
            var saved = vm.Items.Single(x => x.SongId == "123");
            var skipped = vm.Items.Single(x => x.SongId == "456");
            Assert.Equal(LocalizationService.Get("Status.Batch.Saved"), saved.Status);
            Assert.Equal(LocalizationService.Get("Status.Batch.Skipped"), skipped.Status);
            Assert.Equal(BatchItemStateEnum.SAVED, saved.State);
            Assert.Equal(BatchItemStateEnum.SKIPPED, skipped.State);
            Assert.Equal(100, saved.Progress);
            Assert.Equal(0, skipped.Progress);
            Assert.Empty(saved.Error);
            Assert.Equal(LocalizationService.Get("Error.LRC_NOT_EXIST"), skipped.Error);
            Assert.Contains("123", Assert.Single(ws.CreatedFiles));
            Assert.Equal(1, vm.SaveSuccessCount);
            Assert.Equal(1, vm.SaveFailedCount);
        }
        finally
        {
            LocalizationService.Instance.SetLanguage(originalLanguage);
            System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ChangingLyricModeMustNotReuseIncompatibleDiskCache()
    {
        using var ws = new TestWorkspace();
        LocalSongCacheService.SaveCache(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", TestWorkspace.Song());
        ws.Settings.Config.VerbatimLyricMode = VerbatimLyricModeEnum.STANDARD_MODE;
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "123", out _));
    }
}
