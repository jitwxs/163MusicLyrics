using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Service;
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
            Assert.NotNull(window.FindControl<TextBox>("SearchTextBox"));
            Assert.True(window.IsVisible);
            Assert.Contains("MusicLyricApp", vm.AppTitle);
        }
        finally { window.Close(); WeakReferenceMessenger.Default.UnregisterAll(vm); }
        Assert.False(window.IsVisible);
        Assert.False(ws.Storage().ReadAppConfig().Config.ConfirmBeforeExit);
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

    [Fact]
    public async Task BatchSkippedSongsMustNotBeMarkedSaved()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()), ["456"] = new(TestWorkspace.Song("456", "")) }, [TestWorkspace.Input(), TestWorkspace.Input("456")]);
        vm.ExecuteSelectAllCommand.Execute(null);
        await vm.ExecuteSaveSelectedCommand.ExecuteAsync(null);
        Assert.Equal("Saved", vm.Items.Single(x => x.SongId == "123").Status);
        Assert.NotEqual("Saved", vm.Items.Single(x => x.SongId == "456").Status);
        Assert.Equal(1, vm.SaveSuccessCount);
        Assert.Equal(1, vm.SaveFailedCount);
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
