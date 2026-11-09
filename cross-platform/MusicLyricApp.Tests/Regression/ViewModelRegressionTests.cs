using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Localization;
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
        Assert.Equal(LocalizationService.Get("Status.Batch.Failed"), vm.Items[1].Status);
        Assert.Equal(BatchItemStateEnum.FAILED, vm.Items[1].State);
        Assert.True(vm.Items[1].HasError);
        vm.AddSearchResults(results, input);
        Assert.Equal(2, vm.Items.Count);
        results["456"] = new(TestWorkspace.Song("456"));
        vm.AddSearchResults(results, input);
        Assert.Equal(2, vm.SongsWithLyricsCount);
        Assert.Equal(LocalizationService.Get("Status.Batch.Ready"), vm.Items[1].Status);
        Assert.Equal(BatchItemStateEnum.READY, vm.Items[1].State);
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

    // Language changes notify UI subscribers left by other windows; keep Avalonia services active.
    [AvaloniaTheory]
    [InlineData("zh-CN")]
    [InlineData("en-US")]
    [InlineData("zh-TW")]
    public async Task BatchSaveSelectedWritesOnlySelectedSong(string language)
    {
        var originalLanguage = LocalizationService.Instance.Language;
        var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            LocalizationService.Instance.SetLanguage(language);
            using var ws = new TestWorkspace();
            var vm = new BatchSearchViewModel(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);
            vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()), ["456"] = new(TestWorkspace.Song("456")) }, [TestWorkspace.Input(), TestWorkspace.Input("456")]);
            vm.SelectSongsByIds(["456"]);
            await vm.ExecuteSaveSelectedCommand.ExecuteAsync(null);
            Assert.Contains("456", Assert.Single(ws.CreatedFiles));
            Assert.Equal(1, vm.SaveSuccessCount);
            var saved = vm.Items.Single(x => x.SongId == "456");
            Assert.Equal(LocalizationService.Get("Status.Batch.Saved"), saved.Status);
            Assert.Equal(100, saved.Progress);
            Assert.Empty(saved.Error);
            Assert.Equal(LocalizationService.Get("Status.Batch.Ready"), vm.Items.Single(x => x.SongId == "123").Status);
            Assert.Equal(BatchItemStateEnum.SAVED, saved.State);
            Assert.Equal(0, vm.SaveFailedCount);
        }
        finally
        {
            LocalizationService.Instance.SetLanguage(originalLanguage);
            System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
        }
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
        var window = TestWorkspace.Host(new SettingView(), new SettingViewModel(ws.Settings, ws.Window.Object));
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
        var window = TestWorkspace.Host(new BatchSearchView(), vm);
        try
        {
            window.Show();
            vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()) }, [TestWorkspace.Input()]);
            Dispatcher.UIThread.RunJobs();
            var table = Assert.Single(window.GetVisualDescendants().OfType<TKCode.Arc.Table>());
            Assert.Same(vm.VisibleItems, table.ItemsSource); // the table shows the filtered rows
            Assert.Single(vm.Items);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FuzzyResultsSelectionSendsDownloadMessage()
    {
        var result = new SearchResultVo { SearchSource = SearchSourceEnum.QQ_MUSIC };
        result.SongVos.Add(new() { DisplayId = "123", Title = "song", AuthorName = ["singer"], AlbumName = "album" });
        var vm = new BlurSearchViewModel([result]);
        vm.LoadTypeAResults();
        var view = new BlurSearchView();
        var window = TestWorkspace.Host(view, vm);
        var receiver = new object();
        string? selected = null;
        WeakReferenceMessenger.Default.Register<BlurSearchResultsMessage>(receiver, (_, message) => selected = message.Value);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            // Song results get the song columns, and nothing can be fetched until a row is chosen.
            Assert.Equal(5, view.Table.Columns.Count);
            Assert.Equal(LocalizationService.Get("UI.BlurGrid.Song"), view.Table.Columns[0].Header);
            Assert.False(vm.DownloadSelectedCommand.CanExecute(null));
            view.Table.SelectAll();
            Dispatcher.UIThread.RunJobs();
            Assert.Same(Assert.Single(vm.SearchResults), Assert.Single(vm.SelectedItems.Cast<object>()));
            Assert.True(vm.DownloadSelectedCommand.CanExecute(null));
            vm.DownloadSelectedCommand.Execute(null);
            Assert.Equal("qq.com/songDetail/123", selected);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(receiver); window.Close(); }
    }

    [AvaloniaFact]
    public async Task FormatConversionWindowDebouncesInputAndSwapsFormats()
    {
        using var ws = new TestWorkspace();
        var window = TestWorkspace.Host(new FormatConvertView(), new FormatConvertViewModel(ws.Settings));
        var vm = Assert.IsType<FormatConvertViewModel>(window.DataContext);
        try
        {
            window.Show();
            var input = window.GetVisualDescendants().OfType<TextBox>().Single(x => x.AcceptsReturn && !x.IsReadOnly);
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
    public void ThemesMapToAvaloniaVariantsAndArcAppearance()
    {
        Assert.Equal(TKCode.Arc.ArcAppearance.Dark, ThemeService.ToAppearance(ThemeModeEnum.DARK));
        Assert.Equal(TKCode.Arc.ArcAppearance.Light, ThemeService.ToAppearance(ThemeModeEnum.LIGHT));
        Assert.Equal(TKCode.Arc.ArcAppearance.System, ThemeService.ToAppearance(ThemeModeEnum.FOLLOW_SYSTEM));

        var app = Avalonia.Application.Current!;
        ThemeService.Apply(ThemeModeEnum.DARK);
        Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
        Assert.Equal(TKCode.Arc.ArcAppearance.Dark, TKCode.Arc.ArcTheme.State.Appearance);
        ThemeService.Apply(ThemeModeEnum.LIGHT);
        Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);
        ThemeService.Apply(ThemeModeEnum.FOLLOW_SYSTEM);
        Assert.Equal(TKCode.Arc.ArcAppearance.System, TKCode.Arc.ArcTheme.State.Appearance);
        Assert.NotNull(app.RequestedThemeVariant);
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
