using Avalonia.Platform.Storage;
using Moq;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;

namespace MusicLyricAppTest.Regression;

/// <summary>Opening pages and folders, picking folders and offering updates, each shared by several pages.</summary>
public class ShellAndUpdateRegressionTests
{
    private static UpdateCheckService Latest(string tag) => new(() => new GitHubInfo { TagName = tag });

    [Fact]
    public async Task ANewerReleaseShowsItsNotesAndOpensThePageOnlyWhenAccepted()
    {
        var opened = new List<string>();
        string? notes = null;

        Assert.True(await Latest("v999.0").OfferUpdateAsync(n => { notes = n; return Task.FromResult(false); }, opened.Add));
        Assert.NotNull(notes);
        Assert.Empty(opened);

        Assert.True(await Latest("v999.0").OfferUpdateAsync(_ => Task.FromResult(true), opened.Add));
        Assert.Equal([AppInfo.ReleasesUrl], opened);
    }

    [Fact]
    public async Task TheRunningVersionIsComparedWithoutShowingAnything()
    {
        var shown = false;
        Assert.False(await Latest(AppInfo.Title.Split(' ')[^1]).OfferUpdateAsync(_ => { shown = true; return Task.FromResult(true); }, _ => { }));
        Assert.False(shown);
        Assert.Matches(@"^MusicLyricApp v\d+\.\d+$", AppInfo.Title);
    }

    [Fact]
    public void TheCacheFolderOpensThroughTheSameOpenerAsTheSaveFolder()
    {
        using var ws = new TestWorkspace();
        string? opened = null;
        var vm = new SettingViewModel(ws.Settings, openDirectory: path => opened = path);

        vm.OpenSearchCachePathCommand.Execute(null);

        Assert.Equal(ws.Settings.Config.SearchCacheFolderPath, opened);
        Assert.True(Directory.Exists(opened));
    }

    [Fact]
    public async Task APickedFolderGivesItsLocalPathAndACancelGivesNull()
    {
        using var ws = new TestWorkspace();
        Assert.Equal(ws.Root, (await ws.Window.Object.PickFolderAsync("Dialog.Folder.Cache")).LocalPath()?.TrimEnd(Path.DirectorySeparatorChar));

        var cancelled = new Mock<IWindowProvider>();
        cancelled.Setup(x => x.OpenFolderPickerAsync(It.IsAny<FolderPickerOpenOptions>())).ReturnsAsync([]);
        Assert.Null((await cancelled.Object.PickFolderAsync("Dialog.Folder.Cache")).LocalPath());
    }
}
