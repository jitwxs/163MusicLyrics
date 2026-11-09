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
    public void EmptyListShowsEmptyStateAndHidesBulkActions()
    {
        using var scope = new Scope(withRows: false);
        var empty = Assert.Single(scope.View.GetVisualDescendants().OfType<EmptyState>());
        Assert.True(empty.IsVisible);
        Assert.False(scope.View.Table.IsVisible);
        Assert.False(scope.View.FindControl<TKCode.Arc.Button>("SaveSelectedButton")!.IsEffectivelyVisible);

        scope.Vm.AddSearchResults(new() { ["123"] = new(TestWorkspace.Song()) }, [TestWorkspace.Input()]);
        Dispatcher.UIThread.RunJobs();
        Assert.False(empty.IsVisible);
        Assert.True(scope.View.Table.IsVisible);
        Assert.True(scope.View.FindControl<TKCode.Arc.Button>("SaveSelectedButton")!.IsEffectivelyVisible);
        Assert.Equal("1", scope.View.FindControl<TextBlock>("TotalCountText")!.Text);
    }
}
