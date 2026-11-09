using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;

namespace MusicLyricAppTest.Regression;

public class CopyLinksRegressionTests
{
    private const string SongUrl = "https://test.invalid/audio.mp3?token=song";
    private const string CoverUrl = "https://test.invalid/cover.png?token=cover";

    private sealed class HeadlessMainViewModel(IWindowProvider window, StorageService storage, SearchService search)
        : MainWindowViewModel(window, storage, search)
    {
        public override bool IsPlaybackSupported => false;
    }

    private sealed class CopyScope : IDisposable
    {
        public TestWorkspace Workspace { get; } = new();
        public Mock<IWindowProvider> Window { get; } = new(MockBehavior.Strict);
        public Mock<IMusicApi> Api { get; } = new(MockBehavior.Strict);
        public HeadlessMainViewModel Vm { get; }
        private int _httpClientRequests;

        public CopyScope()
        {
            var storage = Workspace.Storage(_ =>
            {
                _httpClientRequests++;
                throw new InvalidOperationException("Copying links must not create a network client.");
            });
            storage.SaveConfig(Workspace.Settings);
            Vm = new HeadlessMainViewModel(Window.Object, storage, Workspace.Search(Api, Api));
        }

        public void LoadSong(string? cover = CoverUrl)
        {
            var song = TestWorkspace.Song();
            song.SongVo.Pics = cover!;
            Vm.SearchResultViewModel.SaveVoMap = new() { ["123"] = song };
            Vm.SearchResultViewModel.SongLink = SongUrl;
        }

        public IAsyncRelayCommand Command(bool cover) => cover
            ? Vm.ExecuteCopySongPicCommand
            : Vm.ExecuteCopySongLinkCommand;

        public static string Success(bool cover) => cover
            ? ErrorMsgConst.SONG_PIC_GET_SUCCESS
            : LocalizationService.Get("Status.Main.SongUrlCopied");

        public void VerifyOnlyClipboard(string? expected = null)
        {
            if (expected is not null)
                Window.Verify(x => x.SetTextAsync(expected), Times.Once);
            Window.VerifyNoOtherCalls();
            Api.VerifyNoOtherCalls();
            Workspace.Folder.Verify(x => x.CreateFileAsync(It.IsAny<string>()), Times.Never);
            Assert.Empty(Workspace.CreatedFiles);
            Assert.Equal(0, _httpClientRequests);
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(Vm);
            Workspace.Dispose();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopiesLoadedUrlWithoutSearchingDownloadingOrSaving(bool cover)
    {
        using var scope = new CopyScope();
        scope.LoadSong();
        var expected = cover ? CoverUrl : SongUrl;
        scope.Window.Setup(x => x.SetTextAsync(expected)).Returns(Task.CompletedTask);

        await scope.Command(cover).ExecuteAsync(null);

        Assert.Empty(scope.Vm.TipErrorMessage);
        Assert.Equal(CopyScope.Success(cover), scope.Vm.TipNormalMessage);
        Assert.False(scope.Vm.IsPlaybackSupported);
        scope.VerifyOnlyClipboard(expected);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoSearchResultsDoesNotCopy(bool cover)
    {
        using var scope = new CopyScope();

        await scope.Command(cover).ExecuteAsync(null);

        Assert.Equal(cover ? ErrorMsgConst.MUST_SEARCH_BEFORE_GET_SONG_PIC
            : LocalizationService.Get("Status.Main.NoSongUrl"), scope.Vm.TipErrorMessage);
        Assert.Empty(scope.Vm.TipNormalMessage);
        scope.VerifyOnlyClipboard();
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task MissingSongUrlDoesNotCopyEvenWithLoadedSong(string? link)
    {
        using var scope = new CopyScope();
        scope.LoadSong();
        scope.Vm.SearchResultViewModel.SongLink = link!;

        await scope.Vm.ExecuteCopySongLinkCommand.ExecuteAsync(null);

        Assert.False(scope.Vm.HasSongLink);
        Assert.Equal(LocalizationService.Get("Status.Main.NoSongUrl"), scope.Vm.TipErrorMessage);
        Assert.Empty(scope.Vm.TipNormalMessage);
        scope.VerifyOnlyClipboard();
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task MissingCoverDoesNotCopySongUrlInstead(string? cover)
    {
        using var scope = new CopyScope();
        scope.LoadSong(cover);

        await scope.Vm.ExecuteCopySongPicCommand.ExecuteAsync(null);

        Assert.Equal(ErrorMsgConst.SONG_PIC_GET_FAILED, scope.Vm.TipErrorMessage);
        Assert.Empty(scope.Vm.TipNormalMessage);
        scope.VerifyOnlyClipboard();
    }

    [AvaloniaFact]
    public async Task MultipleSongsRejectCoverCopyRatherThanChoosingFirst()
    {
        using var scope = new CopyScope();
        scope.LoadSong();
        scope.Vm.SearchResultViewModel.SaveVoMap.Add("456", TestWorkspace.Song("456"));

        await scope.Vm.ExecuteCopySongPicCommand.ExecuteAsync(null);

        Assert.Equal(LocalizationService.Get("Status.Main.SingleSongRequired"), scope.Vm.TipErrorMessage);
        Assert.Empty(scope.Vm.TipNormalMessage);
        scope.VerifyOnlyClipboard();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClipboardMustCompleteBeforeReportingSuccess(bool cover)
    {
        using var scope = new CopyScope();
        scope.LoadSong();
        var expected = cover ? CoverUrl : SongUrl;
        var clipboard = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Window.Setup(x => x.SetTextAsync(expected)).Returns(clipboard.Task);

        var execution = scope.Command(cover).ExecuteAsync(null);
        try
        {
            Assert.False(execution.IsCompleted);
            Assert.True(scope.Command(cover).IsRunning);
            Assert.Empty(scope.Vm.TipNormalMessage);
            Assert.Empty(scope.Vm.TipErrorMessage);
        }
        finally
        {
            clipboard.TrySetResult();
            await execution;
        }

        Assert.Equal(CopyScope.Success(cover), scope.Vm.TipNormalMessage);
        Assert.Empty(scope.Vm.TipErrorMessage);
        scope.VerifyOnlyClipboard(expected);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsynchronousClipboardFailureIsReportedWithoutSuccess(bool cover)
    {
        using var scope = new CopyScope();
        scope.LoadSong();
        var expected = cover ? CoverUrl : SongUrl;
        var clipboard = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("clipboard unavailable (regression)");
        scope.Window.Setup(x => x.SetTextAsync(expected)).Returns(clipboard.Task);

        var execution = scope.Command(cover).ExecuteAsync(null);
        try
        {
            Assert.False(execution.IsCompleted);
            Assert.Empty(scope.Vm.TipNormalMessage);
            Assert.Empty(scope.Vm.TipErrorMessage);
        }
        finally
        {
            clipboard.TrySetException(failure);
            await execution;
        }

        Assert.Equal(LocalizationService.Format(cover ? "Status.Main.CopyCoverUrlFailed"
            : "Status.Main.CopySongUrlFailed", failure.Message), scope.Vm.TipErrorMessage);
        Assert.Contains(failure.Message, scope.Vm.TipErrorMessage);
        Assert.Empty(scope.Vm.TipNormalMessage);
        scope.VerifyOnlyClipboard(expected);
    }

    [AvaloniaFact]
    public void PlaybackContextMenuBindsCopyCommandsAndPreservesSeparateDownloads()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.ConfirmBeforeExit = false;
        ws.Storage().SaveConfig(ws.Settings);
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        // Keep songs empty so opening the headless window cannot trigger cover downloads.
        var window = new MainWindow(ws.Storage(_ => throw new InvalidOperationException("Unexpected network access")),
            ws.Search(api, api));
        var vm = Assert.IsType<MainWindowViewModel>(window.DataContext);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var panel = Assert.IsType<Border>(window.FindControl<Border>("SongPlaybackPanel"));
            var menu = Assert.IsType<ContextMenu>(panel.ContextMenu);
            menu.Open(panel);
            Dispatcher.UIThread.RunJobs();
            var items = menu.Items.OfType<MenuItem>().ToList();
            MenuItem Item(string key) => Assert.Single(items, x => Equals(x.Header, LocalizationService.Get(key)));
            var copySong = Item("UI.CopySongLink");
            var copyCover = Item("UI.CopyCoverLink");
            var downloadSong = Item("UI.DownloadLink");
            var downloadCover = Item("UI.DownloadCover");

            Assert.Same(vm.ExecuteCopySongLinkCommand, copySong.Command);
            Assert.Same(vm.ExecuteCopySongPicCommand, copyCover.Command);
            Assert.Same(vm.ExecuteSongLinkCommand, downloadSong.Command);
            Assert.Same(vm.ExecuteSongPicCommand, downloadCover.Command);
            Assert.NotSame(copySong.Command, downloadSong.Command);
            Assert.NotSame(copyCover.Command, downloadCover.Command);
            Assert.NotSame(copySong.Command, copyCover.Command);
            Assert.False(copySong.IsEnabled);
            Assert.True(copyCover.IsEnabled);

            // Exercise the IsEnabled binding without setting SongLink, which could initialize audio on Windows.
            vm.HasSongLink = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(copySong.IsEnabled);
            vm.HasSongLink = false;
            Dispatcher.UIThread.RunJobs();
            Assert.False(copySong.IsEnabled);
            Assert.True(copyCover.IsEnabled);
            api.VerifyNoOtherCalls();
            Assert.Empty(ws.CreatedFiles);
        }
        finally
        {
            window.FindControl<Border>("SongPlaybackPanel")?.ContextMenu?.Close();
            window.Close();
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }
}
