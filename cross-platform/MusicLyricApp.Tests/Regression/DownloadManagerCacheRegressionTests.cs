using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// Download manager and local cache: startup restore limit, newest-first order, filtering, deleting with the cache,
/// and cache files that no longer depend on the UI language.
/// </summary>
public class DownloadManagerCacheRegressionTests
{
    private static BatchSearchViewModel NewVm(TestWorkspace ws) =>
        new(ws.Settings, ws.Search(new(MockBehavior.Strict)), ws.Storage(), ws.Window.Object);

    private static void Cache(TestWorkspace ws, string id, SearchSourceEnum source = SearchSourceEnum.NET_EASE_MUSIC) =>
        LocalSongCacheService.SaveCache(ws.Settings, source, id, TestWorkspace.Song(id, source: source));

    private static void WithLanguage(string language, Action action)
    {
        var original = LocalizationService.Instance.Language;
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            LocalizationService.Instance.SetLanguage(language);
            action();
        }
        finally
        {
            LocalizationService.Instance.SetLanguage(original);
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(10, 3)]
    public async Task StartupLoadHonoursTheConfiguredLimit(int limit, int expected)
    {
        using var ws = new TestWorkspace();
        foreach (var id in new[] { "1", "2", "3" }) Cache(ws, id);
        ws.Settings.Config.SearchCacheStartupLoadLimit = limit;

        var vm = NewVm(ws);
        await vm.LoadFromLocalCacheAsync();

        Assert.Equal(expected, vm.Items.Count);
        Assert.Equal(expected, vm.VisibleItems.Count);
    }

    [Fact]
    public void StartupLoadLimitDefaultsTo500AndOldConfigsGetIt()
    {
        Assert.Equal(500, new ConfigBean().SearchCacheStartupLoadLimit);
        var config = "{\"Config\":{\"SearchCacheMaxSizeMb\":64}}".ToEntity<SettingBean>();
        Assert.Equal(500, config!.Config.SearchCacheStartupLoadLimit);
    }

    [Theory]
    [InlineData("-1", false)]
    [InlineData("5001", false)]
    [InlineData("abc", false)]
    [InlineData("0", true)]
    [InlineData("5000", true)]
    public void StartupLoadLimitValidation(string value, bool valid)
    {
        using var ws = new TestWorkspace();
        var vm = new SettingParamViewModel();
        vm.Bind(ws.Settings);
        Assert.Equal("500", vm.SearchCacheStartupLoadLimitText);
        vm.SearchCacheStartupLoadLimitText = value;
        Assert.Equal(valid, string.IsNullOrEmpty(vm.SearchCacheStartupLoadLimitError));
        Assert.Equal(valid ? int.Parse(value) : 500, ws.Settings.Config.SearchCacheStartupLoadLimit);
    }

    [AvaloniaFact]
    public void NewestSearchResultsGoToTheTopInTheOrderAsked()
    {
        using var ws = new TestWorkspace();
        var vm = NewVm(ws);
        vm.AddSearchResults(new() { ["1"] = new(TestWorkspace.Song("1")) }, [TestWorkspace.Input("1")]);
        vm.AddSearchResults(new() { ["2"] = new(TestWorkspace.Song("2")), ["3"] = new(TestWorkspace.Song("3")) },
            [TestWorkspace.Input("2"), TestWorkspace.Input("3")]);
        Assert.Equal(["2", "3", "1"], vm.Items.Select(i => i.SongId));

        // Searching a song again makes it the newest.
        vm.AddSearchResults(new() { ["1"] = new(TestWorkspace.Song("1")) }, [TestWorkspace.Input("1")]);
        Assert.Equal(["1", "2", "3"], vm.Items.Select(i => i.SongId));
    }

    [AvaloniaFact]
    public void FilterNarrowsTheTableAndUnticksHiddenRows()
    {
        using var ws = new TestWorkspace();
        var vm = NewVm(ws);
        var songs = new Dictionary<string, ResultVo<SaveVo>>
        {
            ["1"] = new(TestWorkspace.Song("1")),
            ["2"] = new(TestWorkspace.Song("2")),
            ["30"] = new(TestWorkspace.Song("30"))
        };
        vm.AddSearchResults(songs, [TestWorkspace.Input("1"), TestWorkspace.Input("2"), TestWorkspace.Input("30")]);
        var view = new BatchSearchView();
        var window = TestWorkspace.Host(view, vm);
        window.Show();
        try
        {
            vm.ExecuteSelectAllCommand.Execute(null);
            vm.FilterText = "测试歌曲3"; // song name of "30" only
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("30", Assert.Single(vm.VisibleItems).SongId);
            Assert.Same(vm.VisibleItems, view.Table.ItemsSource);
            Assert.Equal(["30"], vm.Items.Where(i => i.IsSelected).Select(i => i.SongId));
            Assert.Equal(LocalizationService.Format("Batch.FilterCount", 1, 3), vm.FilterCountText);

            vm.FilterText = "歌手乙"; // artist, case-insensitive match on every row
            Assert.Equal(3, vm.VisibleItems.Count);

            vm.FilterText = "nothing-matches";
            Assert.Empty(vm.VisibleItems);
            Assert.Equal(LocalizationService.Get("Batch.FilterEmpty"), vm.TableEmptyMessage);

            // Picking songs by id (from fuzzy search) clears the filter so they can be seen.
            vm.SelectSongsByIds(["2"]);
            Assert.Equal("", vm.FilterText);
            Assert.Equal(3, vm.VisibleItems.Count);
            Assert.Equal(LocalizationService.Get("Batch.EmptyTitle"), vm.TableEmptyMessage);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task DeletingAsksFirstThenClearsTheListAndTheCache()
    {
        using var ws = new TestWorkspace();
        Cache(ws, "1");
        Cache(ws, "2");
        // A file written by an older version under the English display name is removed too.
        var folder = ws.Settings.Config.SearchCacheFolderPath;
        File.Copy(Path.Combine(folder, "NET_EASE_MUSIC_1.json"), Path.Combine(folder, "NetEase Music_1.json"));

        var vm = NewVm(ws);
        await vm.LoadFromLocalCacheAsync();
        var asked = 0;
        var answer = false;
        vm.ConfirmDeleteAsync = count => { asked = count; return Task.FromResult(answer); };

        vm.SelectSongsByIds(["1"]);
        await vm.ExecuteDeleteSelectedCommand.ExecuteAsync(null);
        Assert.Equal(1, asked);
        Assert.Equal(2, vm.Items.Count); // cancelled: nothing removed
        Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "1", out _));

        answer = true;
        await vm.ExecuteDeleteSelectedCommand.ExecuteAsync(null);
        Assert.Equal("2", Assert.Single(vm.Items).SongId);
        Assert.False(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "1", out _));
        Assert.Equal(["NET_EASE_MUSIC_2.json"], Directory.GetFiles(folder).Select(Path.GetFileName));

        // Deleted songs do not come back at the next start.
        var next = NewVm(ws);
        await next.LoadFromLocalCacheAsync();
        Assert.Equal("2", Assert.Single(next.Items).SongId);
    }

    [AvaloniaFact]
    public void CacheIsFoundAfterSwitchingLanguage()
    {
        using var ws = new TestWorkspace();
        WithLanguage("zh-CN", () => Cache(ws, "1", SearchSourceEnum.QQ_MUSIC));
        WithLanguage("en-US", () =>
            Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.QQ_MUSIC, "1", out _)));
        Assert.Equal(["QQ_MUSIC_1.json"], Directory.GetFiles(ws.Settings.Config.SearchCacheFolderPath).Select(Path.GetFileName));
    }

    [AvaloniaTheory]
    [InlineData("网易云_7.json")]
    [InlineData("NetEase Music_7.json")]
    public void LegacyCacheNamesAreReadInAnyLanguageAndMigratedOnSave(string legacyName)
    {
        using var ws = new TestWorkspace();
        var folder = ws.Settings.Config.SearchCacheFolderPath;
        Cache(ws, "7");
        LocalSongCacheService.UpdateSongLink(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "7", "http://link/7");
        File.Move(Path.Combine(folder, "NET_EASE_MUSIC_7.json"), Path.Combine(folder, legacyName));

        foreach (var language in new[] { "zh-CN", "en-US" })
        {
            WithLanguage(language, () =>
            {
                Assert.True(LocalSongCacheService.TryLoadSaveVo(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "7", out _));
                Assert.True(LocalSongCacheService.TryLoadSongLink(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "7", out var link));
                Assert.Equal("http://link/7", link);
            });
        }

        // Saving again moves it to the stable name and keeps the stored link.
        Cache(ws, "7");
        Assert.Equal(["NET_EASE_MUSIC_7.json"], Directory.GetFiles(folder).Select(Path.GetFileName));
        Assert.True(LocalSongCacheService.TryLoadSongLink(ws.Settings, SearchSourceEnum.NET_EASE_MUSIC, "7", out var kept));
        Assert.Equal("http://link/7", kept);
    }
}
