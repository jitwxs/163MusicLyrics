using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.ViewModels.Messages;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class ViewModelRegressionTests
{
    [Fact]
    public void SearchOptionsUpdatePersistedModel()
    {
        using var ws = new TestWorkspace();
        var vm = ws.Param("query");
        vm.SelectedSearchSourceItem = vm.SearchSources.Single(x => x.Value == SearchSourceEnum.QQ_MUSIC);
        vm.SelectedSearchTypeItem = vm.SearchTypes.Single(x => x.Value == SearchTypeEnum.ALBUM_ID);
        vm.SelectedOutputFormatItem = vm.OutputFormats.Single(x => x.Value == OutputFormatEnum.SRT);
        vm.SelectedOutputEncodingItem = vm.OutputEncodings.Single(x => x.Value == OutputEncodingEnum.UNICODE);
        vm.SelectedLrcTypeItem = vm.LrcTypes.Single(x => x.Value == ShowLrcTypeEnum.MERGE);
        vm.LrcMergeSeparator = " | ";
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, ws.Settings.Param.SearchSource);
        Assert.Equal(SearchTypeEnum.ALBUM_ID, ws.Settings.Param.SearchType);
        Assert.Equal(OutputFormatEnum.SRT, ws.Settings.Param.OutputFileFormat);
        Assert.Equal(OutputEncodingEnum.UNICODE, ws.Settings.Param.Encoding);
        Assert.Equal(ShowLrcTypeEnum.MERGE, ws.Settings.Param.ShowLrcType);
        Assert.Equal(" | ", ws.Settings.Param.LrcMergeSeparator);
    }

    [Fact]
    public void ResetResultClearsStaleMetadata()
    {
        var vm = TestWorkspace.Result();
        vm.Singer = vm.SongName = vm.SongLink = vm.SongDuration = vm.PublishDate = vm.Album = vm.SongSource = "old";
        vm.ResetConsoleOutput("new");
        Assert.All(new[] { vm.Singer, vm.SongName, vm.SongLink, vm.SongDuration, vm.PublishDate, vm.Album, vm.SongSource }, Assert.Empty);
        Assert.Equal("new", vm.ConsoleOutput);
    }

    [Fact]
    public void BatchSynchronizesRetriesSelectionDeletionAndCounts()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        var input = new List<InputSongId> { TestWorkspace.Input(), TestWorkspace.Input("456") };
        var results = new Dictionary<string, ResultVo<SaveVo>> { ["123"] = new(TestWorkspace.Song()), ["456"] = ResultVo<SaveVo>.Failure("failed") };
        vm.AddSearchResults(results, input, "123,456");
        Assert.Equal(2, vm.TotalSongsCount);
        Assert.Equal(1, vm.SongsWithLyricsCount);
        Assert.Equal("Failed", vm.Items[1].Status);
        vm.AddSearchResults(results, input);
        Assert.Equal(2, vm.Items.Count);
        results["456"] = new(TestWorkspace.Song("456"));
        vm.AddSearchResults(results, input);
        Assert.Equal(2, vm.SongsWithLyricsCount);
        Assert.Equal("Ready", vm.Items[1].Status);
        vm.CacheSongLink("123", "https://test.invalid/audio");
        Assert.True(vm.TryGetCachedSongLink("123", out var link));
        Assert.Equal("https://test.invalid/audio", link);
        vm.ExecuteSelectAllCommand.Execute(null);
        Assert.All(vm.Items, x => Assert.True(x.IsSelected));
        vm.ExecuteClearSelectedCommand.Execute(null);
        Assert.All(vm.Items, x => Assert.False(x.IsSelected));
        vm.SelectSongsByIds(["123"]);
        vm.ExecuteDeleteSelectedCommand.Execute(null);
        Assert.Equal("456", Assert.Single(vm.Items).SongId);
        Assert.False(vm.TryGetCachedSaveVo("123", out _));
        Assert.False(vm.TryGetCachedSongLink("123", out _));
        Assert.Equal(1, vm.TotalSongsCount);
    }

    [Fact]
    public async Task BatchSaveSelectedWritesOnlySelectedSong()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()), ["456"] = new(TestWorkspace.Song("456")) }, [TestWorkspace.Input(), TestWorkspace.Input("456")]);
        vm.SelectSongsByIds(["456"]);
        await vm.ExecuteSaveSelectedCommand.ExecuteAsync(null);
        Assert.Contains("456", Assert.Single(ws.CreatedFiles));
        Assert.Equal(1, vm.SaveSuccessCount);
        Assert.Equal("Saved", vm.Items.Single(x => x.SongId == "456").Status);
        Assert.Equal("Ready", vm.Items.Single(x => x.SongId == "123").Status);
    }

    [Fact]
    public async Task BatchSaveWithNoSelectionIsNoOp()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        await vm.ExecuteSaveSelectedCommand.ExecuteAsync(null);
        Assert.Empty(ws.CreatedFiles);
        Assert.Equal("未选择可保存项。", vm.TipNormalMessage);
    }

    [AvaloniaFact]
    public void SettingsWindowLoadsAndOutputOrderIsPersisted()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.OutputLyricTypes = "0,1";
        var window = new SettingWindow(ws.Settings, ws.Window.Object);
        try
        {
            window.Show();
            var vm = Assert.IsType<SettingViewModel>(window.DataContext);
            Assert.IsType<SettingView>(window.Content);
            vm.SelectedLyricsTypeItem = vm.LyricsTypes[1];
            Assert.True(vm.MoveUpCommand.CanExecute(null));
            vm.MoveUpCommand.Execute(null);
            vm.OnClosing();
            Assert.Equal("1,0", ws.Settings.Config.OutputLyricTypes);
            vm.TimestampTipsCommand.Execute(null);
            Assert.Contains("毫秒", vm.SettingTips);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BatchWindowLoadsXamlAndBindsItems()
    {
        using var ws = new TestWorkspace();
        var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
        var window = new BatchSearchWindow(vm);
        try
        {
            window.Show();
            window.AddResults(new() { ["123"] = new(TestWorkspace.Song()) }, [TestWorkspace.Input()]);
            Dispatcher.UIThread.RunJobs();
            var grid = Assert.Single(window.GetVisualDescendants().OfType<DataGrid>());
            Assert.Same(vm.Items, grid.ItemsSource);
            Assert.Single(vm.Items);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FuzzyResultsSelectionSendsDownloadMessage()
    {
        var result = new SearchResultVo { SearchSource = SearchSourceEnum.QQ_MUSIC };
        result.SongVos.Add(new() { DisplayId = "123", Title = "song", AuthorName = ["singer"], AlbumName = "album" });
        var window = new BlurSearchWindow([result]);
        var receiver = new object();
        string? selected = null;
        WeakReferenceMessenger.Default.Register<BlurSearchResultsMessage>(receiver, (_, message) => selected = message.Value);
        try
        {
            window.Show();
            var vm = Assert.IsType<BlurSearchViewModel>(window.DataContext);
            vm.SelectedItems = new List<object> { Assert.Single(vm.SearchResults) };
            vm.DownloadSelectedCommand.Execute(null);
            Assert.Equal("qq.com/songDetail/123", selected);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(receiver); window.Close(); }
    }

    [AvaloniaFact]
    public async Task FormatConversionWindowDebouncesInputAndSwapsFormats()
    {
        using var ws = new TestWorkspace();
        var window = new FormatConvertWindow(ws.Settings);
        var vm = Assert.IsType<FormatConvertViewModel>(window.DataContext);
        try
        {
            window.Show();
            var input = window.GetVisualDescendants().OfType<TextBox>().Single(x => !x.IsReadOnly);
            input.Text = "[00:01.000]hello\n[00:03.000]world";
            await WaitUntil(() => vm.OutputText.Contains("00:00:03,000 --> 00:00:06,000"));
            Assert.Equal(input.Text, vm.InputText);
            Assert.Contains("LRC -> SRT", vm.TipMessage);
            vm.SwapFormatCommand.Execute(null);
            await WaitUntil(() => vm.TipMessage.Contains("SRT -> LRC"));
            Assert.Equal("[00:01.000]hello" + Environment.NewLine + "[00:03.000]world", vm.OutputText);
            vm.InputText = "";
            await WaitUntil(() => vm.OutputText == "");
        }
        finally { vm.InputText = ""; window.Close(); }
    }

    [AvaloniaFact]
    public async Task FormatConversionReportsInvalidInput()
    {
        var vm = new FormatConvertViewModel(new());
        vm.SourceFormatItem = vm.Formats.Single(x => x.Value == OutputFormatEnum.SRT);
        vm.TargetFormatItem = vm.Formats.Single(x => x.Value == OutputFormatEnum.LRC);
        vm.InputText = "1\ninvalid --> 00:00:01,000\nhello";
        await WaitUntil(() => vm.TipMessage.StartsWith("转换失败"));
        Assert.Empty(vm.OutputText);
    }

    [AvaloniaFact]
    public void ThemesMapToAvaloniaVariants()
    {
        var app = new MusicLyricApp.App();
        app.SetTheme(ThemeModeEnum.DARK);
        Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
        app.SetTheme(ThemeModeEnum.LIGHT);
        Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
        app.SetTheme(ThemeModeEnum.FOLLOW_SYSTEM);
        Assert.Equal(ThemeVariant.Default, app.RequestedThemeVariant);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.True(condition(), "UI state did not settle within 4 seconds.");
    }
}
