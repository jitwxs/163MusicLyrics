using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.ViewModels;
using MusicLyricApp.ViewModels.Messages;
using MusicLyricApp.Views;
using TKCode.Arc;

namespace MusicLyricAppTest.Regression;

/// <summary>The Arc batch page: table selection mirrored onto rows, status badges, row actions and empty state.</summary>
public class BatchPageRegressionTests
{
    private sealed class Scope : IDisposable
    {
        public TestWorkspace Ws { get; } = new();
        public BatchSearchViewModel Vm { get; }
        public BatchSearchView View { get; } = new();
        public Window Window { get; }

        public Scope(bool withRows = true)
        {
            Vm = new BatchSearchViewModel(Ws.Settings, Ws.Search(new(MockBehavior.Strict)), Ws.Storage(), Ws.Window.Object);
            if (withRows)
            {
                Vm.AddSearchResults(new()
                {
                    ["123"] = new(TestWorkspace.Song()),
                    ["456"] = MusicLyricApp.Models.ResultVo<MusicLyricApp.Models.SaveVo>.Failure("lookup failed")
                }, [TestWorkspace.Input(), TestWorkspace.Input("456")]);
            }
            Window = TestWorkspace.Host(View, Vm);
            Window.Show();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Window.Close();
            Ws.Dispose();
        }
    }

    [AvaloniaFact]
    public void TableSelectionAndRowFlagsStayInSync()
    {
        using var scope = new Scope();
        var table = scope.View.Table;

        // Rows to table: the view model selects songs by id (as fuzzy search does).
        scope.Vm.SelectSongsByIds(["456"]);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(scope.Vm.Items[1], Assert.Single(table.SelectedItems!.Cast<object>()));

        // Table to rows: the header checkbox selects everything.
        table.SelectAll();
        Dispatcher.UIThread.RunJobs();
        Assert.All(scope.Vm.Items, x => Assert.True(x.IsSelected));

        table.ClearSelection();
        Dispatcher.UIThread.RunJobs();
        Assert.All(scope.Vm.Items, x => Assert.False(x.IsSelected));
    }

    [AvaloniaFact]
    public void StatusBadgesUseStateToneAndLocalizedWords()
    {
        using var scope = new Scope();
        var badges = scope.View.GetVisualDescendants().OfType<Badge>().Where(b => b.IsEffectivelyVisible).ToList();
        Assert.Contains(badges, b => b.Tone == BadgeTone.Info && Equals(b.Content, LocalizationService.Get("Status.Batch.Ready")));
        Assert.Contains(badges, b => b.Tone == BadgeTone.Danger && Equals(b.Content, LocalizationService.Get("Status.Batch.Failed")));
        Assert.Contains(scope.View.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "lookup failed");
    }

    [AvaloniaFact]
    public void RowActionsBindToViewModelCommandsWithTheirRow()
    {
        using var scope = new Scope();
        var buttons = scope.View.GetVisualDescendants().OfType<TKCode.Arc.Button>()
            .Where(b => ReferenceEquals(b.Command, scope.Vm.ExecuteViewDetailCommand)).ToList();
        Assert.Equal(2, buttons.Count);
        Assert.Contains(buttons, b => ReferenceEquals(b.CommandParameter, scope.Vm.Items[0]));
        Assert.Equal(2, scope.View.GetVisualDescendants().OfType<TKCode.Arc.Button>()
            .Count(b => ReferenceEquals(b.Command, scope.Vm.ExecuteContextSaveCommand)));

        var receiver = new object();
        SongDetailRequest? request = null;
        WeakReferenceMessenger.Default.Register<OpenSongDetailMessage>(receiver, (_, m) => request = m.Value);
        try
        {
            var first = buttons.Single(b => ReferenceEquals(b.CommandParameter, scope.Vm.Items[0]));
            first.Command!.Execute(first.CommandParameter);
            Assert.Equal("123", request?.SongId);
            Assert.False(scope.Vm.Items[0].IsSelected); // viewing a row does not tick it
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(receiver); }
    }

    [AvaloniaFact]
    public async Task RowSaveButtonSavesOnlyItsRowAndKeepsTheSelection()
    {
        using var scope = new Scope(withRows: false);
        scope.Vm.AddSearchResults(new()
        {
            ["123"] = new(TestWorkspace.Song("123")),
            ["789"] = new(TestWorkspace.Song("789"))
        }, [TestWorkspace.Input("123"), TestWorkspace.Input("789")]);
        scope.Vm.SelectSongsByIds(["789"]);
        Dispatcher.UIThread.RunJobs();

        await scope.Vm.ExecuteContextSaveCommand.ExecuteAsync(scope.Vm.Items[0]);

        Assert.Equal(BatchItemStateEnum.SAVED, scope.Vm.Items[0].State);
        Assert.NotEqual(BatchItemStateEnum.SAVED, scope.Vm.Items[1].State);
        Assert.Single(scope.Ws.CreatedFiles);
        Assert.False(scope.Vm.Items[0].IsSelected);
        Assert.True(scope.Vm.Items[1].IsSelected);
    }

    [AvaloniaFact]
    public async Task StartupRestoresCachedSongsWithoutDuplicatesOrStaleEntries()
    {
        using var scope = new Scope(withRows: false);
        var settings = scope.Ws.Settings;
        MusicLyricApp.Core.Service.LocalSongCacheService.SaveCache(settings, MusicLyricApp.Models.SearchSourceEnum.NET_EASE_MUSIC, "111", TestWorkspace.Song("111"), "http://link/111");
        MusicLyricApp.Core.Service.LocalSongCacheService.SaveCache(settings, MusicLyricApp.Models.SearchSourceEnum.QQ_MUSIC, "222", TestWorkspace.Song("222", source: MusicLyricApp.Models.SearchSourceEnum.QQ_MUSIC));
        File.WriteAllText(Path.Combine(settings.Config.SearchCacheFolderPath, "broken.json"), "{not json");

        // Already in the list (searched before the cache finished loading): kept as is, not duplicated.
        scope.Vm.AddSearchResults(new() { ["111"] = new(TestWorkspace.Song("111")) }, [TestWorkspace.Input("111")]);
        await scope.Vm.LoadFromLocalCacheAsync();

        Assert.Equal(["111", "222"], scope.Vm.Items.Select(i => i.SongId));
        var restored = scope.Vm.Items[1];
        Assert.Equal(MusicLyricApp.Models.SearchSourceEnum.QQ_MUSIC, restored.SourceEnum);
        Assert.True(restored.IsReady);
        Assert.False(restored.IsSelected);
        Assert.Equal(2, scope.Vm.TotalSongsCount);
        Assert.True(scope.Vm.TryGetCachedSaveVo("222", out _));
        Assert.True(scope.Vm.TryGetCachedSongLink("111", out var link));
        Assert.Equal("http://link/111", link);
    }

    [AvaloniaFact]
    public void CacheLoaderKeepsTheNewestSongsNewestFirstAndSkipsOtherLyricModes()
    {
        using var ws = new TestWorkspace();
        var folder = ws.Settings.Config.SearchCacheFolderPath;
        for (var i = 1; i <= 3; i++)
        {
            MusicLyricApp.Core.Service.LocalSongCacheService.SaveCache(ws.Settings, MusicLyricApp.Models.SearchSourceEnum.NET_EASE_MUSIC, $"{i}", TestWorkspace.Song($"{i}"));
        }

        // Rewrite timestamps so the order is deterministic: 1 oldest, 3 newest.
        foreach (var file in Directory.GetFiles(folder))
        {
            var text = File.ReadAllText(file);
            var id = System.Text.RegularExpressions.Regex.Match(text, "\"SongId\":\\s*\"(\\d)\"").Groups[1].Value;
            text = System.Text.RegularExpressions.Regex.Replace(text, "\"UpdatedAt\":\\s*\"[^\"]+\"", $"\"UpdatedAt\":\"2026-01-0{id}T00:00:00\"");
            File.WriteAllText(file, text);
        }

        var recent = MusicLyricApp.Core.Service.LocalSongCacheService.LoadRecent(ws.Settings, 2);
        Assert.Equal(["3", "2"], recent.Select(s => s.SongId)); // newest first

        ws.Settings.Config.VerbatimLyricMode = MusicLyricApp.Models.VerbatimLyricModeEnum.STANDARD_MODE;
        Assert.Empty(MusicLyricApp.Core.Service.LocalSongCacheService.LoadRecent(ws.Settings, 10));
    }

    [AvaloniaFact]
    public void EmptyListShowsEmptyStateAndHidesBulkActions()
    {
        using var scope = new Scope(withRows: false);
        var empty = Assert.Single(scope.View.GetVisualDescendants().OfType<EmptyState>());
        Assert.True(empty.IsEffectivelyVisible);
        Assert.False(scope.View.Table.IsVisible);
        Assert.False(scope.View.FindControl<TKCode.Arc.Button>("SaveSelectedButton")!.IsEffectivelyVisible);

        scope.Vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()) }, [TestWorkspace.Input()]);
        Dispatcher.UIThread.RunJobs();
        Assert.False(empty.IsEffectivelyVisible);
        Assert.True(scope.View.Table.IsVisible);
        Assert.True(scope.View.FindControl<TKCode.Arc.Button>("SaveSelectedButton")!.IsEffectivelyVisible);
        Assert.Equal("1", scope.View.FindControl<TextBlock>("TotalCountText")!.Text);
    }
}
