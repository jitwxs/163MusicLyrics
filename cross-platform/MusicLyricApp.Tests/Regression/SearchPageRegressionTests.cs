using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.ViewModels.Messages;
using MusicLyricApp.Views;
using TKCode.Arc;

namespace MusicLyricAppTest.Regression;

/// <summary>The Arc search page: query controls, empty states, fuzzy results in place and toasts.</summary>
public class SearchPageRegressionTests
{
    private sealed class Scope : IDisposable
    {
        public TestWorkspace Ws { get; } = new();
        public MainWindow Window { get; }
        public MainWindowViewModel Vm { get; }
        public SearchView Page { get; }

        public Scope()
        {
            Ws.Settings.Config.ConfirmBeforeExit = false;
            Ws.Storage().SaveConfig(Ws.Settings);
            Window = new MainWindow(Ws.Storage(), Ws.Search(new(MockBehavior.Strict)));
            Vm = Assert.IsType<MainWindowViewModel>(Window.DataContext);
            Window.Show();
            Dispatcher.UIThread.RunJobs();
            Page = Window.FindControl<SearchView>("SearchPage")!;
        }

        public T Find<T>(string name) where T : Control =>
            Assert.Single(Page.GetVisualDescendants().OfType<T>(), x => x.Name == name);

        public void Dispose()
        {
            Window.Close();
            WeakReferenceMessenger.Default.UnregisterAll(Vm);
            Ws.Dispose();
        }
    }

    [AvaloniaFact]
    public void EnterInSearchFieldRunsOneExactSearch()
    {
        using var scope = new Scope();
        var tips = new List<(string Message, bool IsError)>();
        scope.Vm.TipRaised += (message, isError) => tips.Add((message, isError));
        scope.Vm.IsBlurSearch = true;

        var field = scope.Find<SearchField>("SearchTextBox");
        field.Focus();
        scope.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        scope.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        // Enter forces an exact search; an empty query reports exactly one error and opens no fuzzy results.
        Assert.False(scope.Vm.IsBlurSearch);
        var tip = Assert.Single(tips);
        Assert.True(tip.IsError);
        Assert.False(scope.Vm.IsShowingBlurResults);
    }

    [AvaloniaFact]
    public void SegmentedControlsAndViewModelStayInSync()
    {
        using var scope = new Scope();
        var source = scope.Find<SegmentedControl>("SourceSegment");
        var type = scope.Find<SegmentedControl>("TypeSegment");
        var param = scope.Vm.SearchParamViewModel;

        Assert.Equal(param.SelectedSearchSource, source.SelectedValue);
        Assert.Equal(param.SelectedSearchType, type.SelectedValue);

        source.SelectedValue = SearchSourceEnum.QQ_MUSIC;
        type.SelectedValue = SearchTypeEnum.PLAYLIST_ID;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, param.SelectedSearchSource);
        Assert.Equal(SearchTypeEnum.PLAYLIST_ID, param.SelectedSearchType);
        Assert.Equal(SearchSourceEnum.QQ_MUSIC, scope.Vm.SettingBean.Param.SearchSource);

        param.SelectedSearchSourceItem = param.SearchSources.Single(x => x.Value == SearchSourceEnum.NET_EASE_MUSIC);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SearchSourceEnum.NET_EASE_MUSIC, source.SelectedValue);
    }

    [AvaloniaFact]
    public void EmptyStatesShowUntilASongIsRendered()
    {
        using var scope = new Scope();
        var songEmpty = scope.Find<EmptyState>("SongEmptyState");
        var previewEmpty = scope.Find<EmptyState>("PreviewEmptyState");
        Assert.True(songEmpty.IsVisible);
        Assert.True(previewEmpty.IsVisible);
        Assert.Equal(LocalizationService.Get("Search.EmptyTitle"), songEmpty.Title);

        scope.Vm.SearchResultViewModel.SongName = "song";
        scope.Vm.SearchResultViewModel.ConsoleOutput = "[00:01.000]hello";
        Dispatcher.UIThread.RunJobs();
        Assert.False(songEmpty.IsVisible);
        Assert.False(previewEmpty.IsVisible);
        Assert.Equal("[00:01.000]hello", scope.Find<CodeBlock>("LyricPreview").Code);
        Assert.Equal("song", scope.Find<SelectableTextBlock>("SongTitle").Text);

        // Badge.Icon renders a raw Geometry as its type name; every badge icon must be a control.
        var badges = scope.Page.GetVisualDescendants().OfType<Badge>().ToList();
        Assert.NotEmpty(badges);
        Assert.All(badges, b => Assert.IsType<ArcGlyph>(b.Icon));
    }

    [AvaloniaFact]
    public void FuzzyResultsReplacePreviewAndBackReturnsToIt()
    {
        using var scope = new Scope();
        var result = new SearchResultVo { SearchSource = SearchSourceEnum.QQ_MUSIC };
        result.SongVos.Add(new() { DisplayId = "1", Title = "song", AuthorName = ["singer"], AlbumName = "album" });
        var results = new BlurSearchViewModel([result]);
        results.LoadTypeAResults();
        var save = scope.Find<TKCode.Arc.Button>("SaveButton");
        var view = scope.Find<BlurSearchView>("BlurResultsView");
        Assert.False(view.IsVisible);
        Assert.True(save.IsEffectivelyVisible);

        scope.Vm.BlurResults = results;
        Dispatcher.UIThread.RunJobs();
        Assert.True(view.IsVisible);
        Assert.Same(results, view.DataContext);
        Assert.Single(results.SearchResults);
        // The results view carries its own primary action, so the page's save bar steps aside.
        Assert.False(save.IsEffectivelyVisible);

        view.FindControl<TKCode.Arc.Button>("BackButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Null(scope.Vm.BlurResults);
        Assert.False(view.IsVisible);
        Assert.True(save.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void ChoosingFuzzyResultsClosesThemAndRequestsLyrics()
    {
        using var scope = new Scope();
        var result = new SearchResultVo { SearchSource = SearchSourceEnum.QQ_MUSIC };
        result.SongVos.Add(new() { DisplayId = "1", Title = "song", AuthorName = ["singer"], AlbumName = "album" });
        var results = new BlurSearchViewModel([result]);
        results.LoadTypeAResults();
        scope.Vm.BlurResults = results;
        var receiver = new object();
        string? requested = null;
        WeakReferenceMessenger.Default.Register<BlurSearchResultsMessage>(receiver, (_, m) => requested = m.Value);
        try
        {
            results.SelectedItems = results.SearchResults.Cast<object>().ToList();
            results.DownloadSelectedCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("qq.com/songDetail/1", requested);
            Assert.Null(scope.Vm.BlurResults);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(receiver); }
    }

    [AvaloniaFact]
    public void StatusMessagesAppearAsToasts()
    {
        using var scope = new Scope();
        scope.Vm.ExecuteCloseBlurResultsCommand.Execute(null);
        scope.Vm.ExecuteOpenDownloadManagerCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var stack = Assert.IsType<ToastStack>(ToastHost.GetStack(scope.Window));
        Assert.True(stack.Count > 0);
        Assert.Equal(LocalizationService.Get("Status.Main.DownloadOpened"), scope.Vm.TipNormalMessage);
    }
}
