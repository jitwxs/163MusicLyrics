using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using MusicLyricApp.ViewModels;
using MusicLyricApp.Views;
using TKCode.Arc;

namespace MusicLyricAppTest.Regression;

/// <summary>
/// The lyrics on the online lyrics page: one AvaloniaEdit editor, read-only as the preview and writable while
/// editing, with its own find and replace. Covers the edited state, cancel and revert, saving and leaving.
/// </summary>
public class LyricEditorRegressionTests
{
    // ---- Edited state ----

    private static SearchResultViewModel Loaded(string lyrics = "[00:01.000]hello")
    {
        var result = TestWorkspace.Result(TestWorkspace.Song());
        result.ConsoleOutput = lyrics;
        return result;
    }

    [Fact]
    public void EditingTracksChangesAgainstTheGeneratedLyrics()
    {
        var result = Loaded();
        Assert.True(result.CanEdit);
        Assert.True(result.BeginEditCommand.CanExecute(null));
        Assert.False(result.FinishEditCommand.CanExecute(null));

        result.BeginEditCommand.Execute(null);
        Assert.True(result.IsEditing);
        Assert.False(result.BeginEditCommand.CanExecute(null));
        Assert.False(result.IsEdited); // Opening the editor is not an edit.
        Assert.Equal(LocalizationService.Get("Editor.EditingTitle"), result.PreviewTitle);

        result.UpdateEditedText("[00:01.000]hello world");
        Assert.True(result.IsEdited);
        Assert.True(result.HasUnsavedEdits);
        Assert.Equal("[00:01.000]hello world", result.PreviewText);
        Assert.NotNull(result.FormatLockedTip);

        // Typing it back the way it was is no edit, even with other line endings.
        result.UpdateEditedText("[00:01.000]hello");
        Assert.False(result.IsEdited);

        result.UpdateEditedText("line1\nline2");
        Assert.Equal("line1" + Environment.NewLine + "line2", result.EditedTextForSave);

        // 完成 keeps the edits on show.
        result.FinishEditCommand.Execute(null);
        Assert.False(result.IsEditing);
        Assert.Equal("line1\nline2", result.PreviewText);
        Assert.Equal(LocalizationService.Get("Editor.PreviewEdited"), result.PreviewTitle);

        // Changes from the read-only preview are ignored.
        result.UpdateEditedText("not while reading");
        Assert.Equal("line1\nline2", result.PreviewText);
    }

    [Fact]
    public void CancelPutsBackThisSessionAndRevertDropsEverything()
    {
        var result = Loaded();
        result.BeginEditCommand.Execute(null);
        result.UpdateEditedText("first session");
        result.FinishEditCommand.Execute(null);
        result.MarkSaved(); // As if 保存歌词 wrote the first session.

        // 取消 goes back to how it was when this session began: the first session's (saved) edits.
        result.BeginEditCommand.Execute(null);
        result.UpdateEditedText("second session");
        Assert.True(result.HasUnsavedEdits);
        result.CancelEditCommand.Execute(null);
        Assert.False(result.IsEditing);
        Assert.Equal("first session", result.PreviewText);
        Assert.False(result.HasUnsavedEdits);

        // 还原 drops every change back to the generated lyrics. That differs from what was saved, so it is unsaved.
        result.BeginEditCommand.Execute(null);
        result.RevertEditsCommand.Execute(null);
        Assert.False(result.IsEdited);
        Assert.Equal("[00:01.000]hello", result.PreviewText);
        Assert.True(result.HasUnsavedEdits);
        Assert.True(result.IsEditing); // Still editing; ⌘Z in the editor brings the changes back.

        // Cancelling a session that began without edits forgets everything typed in it.
        result.FinishEditCommand.Execute(null);
        result.MarkSaved();
        result.BeginEditCommand.Execute(null);
        result.UpdateEditedText("typed then cancelled");
        result.CancelEditCommand.Execute(null);
        Assert.False(result.IsEdited);
        Assert.False(result.HasUnsavedEdits);

        // ⌘E opens the editor and closes it again, keeping the changes.
        result.ToggleEditCommand.Execute(null);
        result.UpdateEditedText("kept");
        result.ToggleEditCommand.Execute(null);
        Assert.False(result.IsEditing);
        Assert.Equal("kept", result.PreviewText);
    }

    [Fact]
    public void NewOptionsKeepTheEdits()
    {
        var result = Loaded();
        result.BeginEditCommand.Execute(null);
        result.UpdateEditedText("mine");
        result.ConsoleOutput = "[00:01.0]hello";
        Assert.Equal("mine", result.PreviewText);

        // Reverting shows the lyrics from the latest options.
        result.RevertEditsCommand.Execute(null);
        Assert.Equal("[00:01.0]hello", result.PreviewText);
    }

    [Fact]
    public void LyricsSavedAsSeveralFilesAreNotEditable()
    {
        var result = Loaded();
        result.OutputPartCount = 2;
        Assert.False(result.CanEdit);
        Assert.False(result.BeginEditCommand.CanExecute(null));
        Assert.False(result.ToggleEditCommand.CanExecute(null));
        Assert.Equal(LocalizationService.Get("Editor.SplitFiles"), result.EditUnavailableTip);

        Assert.False(TestWorkspace.Result().CanEdit); // No song.
        Assert.False(TestWorkspace.Result(TestWorkspace.Song()).CanEdit); // No lyrics.
    }

    // ---- Saving and copying ----

    [Fact]
    public async Task SavingWritesTheEditedLyricsWithPlatformLineEndings()
    {
        using var ws = new TestWorkspace();
        var result = TestWorkspace.Result(TestWorkspace.Song());
        result.ConsoleOutput = "[00:01.000]测试歌词";
        result.BeginEditCommand.Execute(null);
        result.UpdateEditedText("[00:01.000]改过的歌词\n[00:02.000]第二行");

        await ws.Storage().SaveResult(result, ws.Settings, ws.Window.Object);

        Assert.Equal("[00:01.000]改过的歌词" + Environment.NewLine + "[00:02.000]第二行",
            File.ReadAllText(Assert.Single(ws.CreatedFiles)));
    }

    // ---- Main flow ----

    private sealed class HeadlessMainViewModel(TestWorkspace ws, MusicLyricApp.Core.Service.SearchService search)
        : MainWindowViewModel(ws.Window.Object, ws.Storage(), search)
    {
        public override bool IsPlaybackSupported => false;
    }

    private static Mock<IMusicApi> Api(params SaveVo[] songs)
    {
        var api = new Mock<IMusicApi>(MockBehavior.Strict);
        foreach (var song in songs)
        {
            var id = song.SongVo.DisplayId;
            api.Setup(x => x.GetSongVo(It.Is<string[]>(ids => ids.SequenceEqual(new[] { id }))))
                .Returns(new Dictionary<string, ResultVo<SongVo>> { [id] = new(song.SongVo) });
            api.Setup(x => x.GetLyricVo(id, id, false)).Returns(new ResultVo<LyricVo>(song.LyricVo));
            api.Setup(x => x.GetSongLink(id)).Returns(new ResultVo<string>($"https://test.invalid/{id}.mp3"));
        }

        return api;
    }

    [AvaloniaFact]
    public async Task UnsavedEditsAskBeforeAnotherSongReplacesThem()
    {
        using var ws = new TestWorkspace();
        ws.Storage().SaveConfig(ws.Settings);
        var vm = new HeadlessMainViewModel(ws, ws.Search(Api(TestWorkspace.Song("1"), TestWorkspace.Song("2", "[00:02.000]第二首"))));
        try
        {
            var asked = 0;
            var answer = false;
            vm.ConfirmDiscardEditsAsync = () => { asked++; return Task.FromResult(answer); };
            var result = vm.SearchResultViewModel;

            vm.SearchParamViewModel.SearchText = "1";
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            result.BeginEditCommand.Execute(null);
            result.UpdateEditedText("[00:01.000]我的修改");

            vm.SearchParamViewModel.SearchText = "2";
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.Equal(1, asked);
            Assert.Equal("测试歌曲1", result.SongName); // Declined: nothing replaced.
            Assert.Equal("[00:01.000]我的修改", result.PreviewText);

            // Copying takes the lyrics as shown.
            await vm.ExecuteCopyLyricsCommand.ExecuteAsync(null);
            Assert.Equal("[00:01.000]我的修改", ws.Clipboard);

            // Saved edits need no question.
            await vm.ExecuteSaveCommand.ExecuteAsync(null);
            Assert.Equal("[00:01.000]我的修改", File.ReadAllText(Assert.Single(ws.CreatedFiles)));
            Assert.False(result.HasUnsavedEdits);
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.Equal(1, asked);
            Assert.Equal("测试歌曲2", result.SongName);
            Assert.False(result.IsEditing);
            Assert.False(result.IsEdited);
            Assert.Equal("[00:02.000]第二首", result.PreviewText);

            // Confirming discards the edits and loads the song.
            result.BeginEditCommand.Execute(null);
            result.UpdateEditedText("changed");
            answer = true;
            vm.SearchParamViewModel.SearchText = "1";
            await vm.ExecuteExactSearchWithModeCommand.ExecuteAsync(null);
            Assert.Equal(2, asked);
            Assert.Equal("测试歌曲1", result.SongName);
            Assert.False(result.HasUnsavedEdits);
        }
        finally { WeakReferenceMessenger.Default.UnregisterAll(vm); }
    }

    // ---- The page ----

    private sealed class Scope : IDisposable
    {
        public TestWorkspace Ws { get; } = new();
        public MainWindow Window { get; }
        public MainWindowViewModel Vm { get; }
        public SearchView Page { get; }
        public SearchResultViewModel Result => Vm.SearchResultViewModel;

        public Scope(string? saveFolder = null)
        {
            Ws.Settings.Config.ConfirmBeforeExit = false;
            if (saveFolder != null) Ws.Settings.Config.LastSaveFolderPath = saveFolder;
            Ws.Storage().SaveConfig(Ws.Settings);
            Window = new MainWindow(Ws.Storage(), Ws.Search(new(MockBehavior.Strict))) { Width = 1080, Height = 760 };
            Vm = Assert.IsType<MainWindowViewModel>(Window.DataContext);
            Window.Show();
            Dispatcher.UIThread.RunJobs();
            Page = Window.FindControl<SearchView>("SearchPage")!;
        }

        public void LoadSong(string lyrics)
        {
            Result.SaveVoMap.Add("123", TestWorkspace.Song());
            Result.NotifySongsChanged();
            Result.SongName = "测试歌曲123";
            Result.ConsoleOutput = lyrics;
            Dispatcher.UIThread.RunJobs();
        }

        public T Find<T>(string name) where T : Control =>
            Assert.Single(Page.GetVisualDescendants().OfType<T>(), x => x.Name == name);

        public TextEditor Editor => Find<TextEditor>("LyricPreview");

        public bool Visible(string name) => Find<Control>(name).IsEffectivelyVisible;

        public void Click(string button)
        {
            Find<Avalonia.Controls.Button>(button).Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();
        }

        public void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var raw = RawInputModifiers.None;
            if (modifiers.HasFlag(KeyModifiers.Control)) raw |= RawInputModifiers.Control;
            if (modifiers.HasFlag(KeyModifiers.Meta)) raw |= RawInputModifiers.Meta;
            if (modifiers.HasFlag(KeyModifiers.Alt)) raw |= RawInputModifiers.Alt;
            if (modifiers.HasFlag(KeyModifiers.Shift)) raw |= RawInputModifiers.Shift;
            Window.KeyPress(key, raw, PhysicalKey.None, null);
            Window.KeyRelease(key, raw, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Window.Close();
            WeakReferenceMessenger.Default.UnregisterAll(Vm);
            Ws.Dispose();
        }
    }

    private const string TwoLines = "[00:01.000]hello\n[00:02.000]hello again";

    [AvaloniaFact]
    public void PreviewAndEditorAreOneEditorWithTheirOwnHeaderActions()
    {
        using var scope = new Scope();
        scope.LoadSong(TwoLines);
        var editor = scope.Editor;

        // Preview: read-only, with find, edit and copy beside the title.
        Assert.Equal(TwoLines, editor.Text);
        Assert.True(editor.IsReadOnly);
        Assert.True(editor.ShowLineNumbers);
        Assert.True(scope.Visible("FindInLyricsButton"));
        Assert.True(scope.Visible("EditLyricsButton"));
        Assert.True(scope.Visible("CopyLyricsButton"));
        Assert.False(scope.Visible("FinishEditButton"));
        Assert.Equal("· lrc", scope.Vm.PreviewFormatLabel);

        scope.Click("EditLyricsButton");
        Assert.False(editor.IsReadOnly);
        Assert.True(editor.TextArea.IsFocused);
        Assert.False(scope.Visible("EditLyricsButton"));
        Assert.True(scope.Visible("CancelEditButton"));
        Assert.True(scope.Visible("FinishEditButton"));
        Assert.True(scope.Visible("FindReplaceButton"));
        Assert.False(scope.Visible("RevertEditsButton")); // Nothing to revert yet.

        // Typing in the editor is an edit, and locks the format.
        editor.Document.Insert(editor.Document.TextLength, "\n[00:03.000]new line");
        Dispatcher.UIThread.RunJobs();
        Assert.True(scope.Result.IsEdited);
        Assert.True(scope.Visible("RevertEditsButton"));
        Assert.False(scope.Find<Select>("LrcTypeSelect").IsEnabled);
        Assert.False(scope.Find<Select>("OutputFormatSelect").IsEnabled);

        // 还原 is one undoable change in the editor.
        scope.Click("RevertEditsButton");
        Assert.Equal(TwoLines, editor.Text);
        Assert.False(scope.Result.IsEdited);
        editor.Undo();
        Dispatcher.UIThread.RunJobs();
        Assert.EndsWith("[00:03.000]new line", editor.Text);
        Assert.True(scope.Result.IsEdited);

        // 完成 keeps it, read-only again, with no history left to undo into.
        scope.Click("FinishEditButton");
        Assert.True(editor.IsReadOnly);
        Assert.EndsWith("[00:03.000]new line", editor.Text);
        Assert.False(editor.CanUndo);
        Assert.Equal(LocalizationService.Get("Editor.PreviewEdited"), scope.Find<TextBlock>("LyricTitle").Text);

        // 取消 puts back what was there when this session began.
        scope.Click("EditLyricsButton");
        editor.Document.Text = "scrapped";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("scrapped", scope.Result.PreviewText);
        scope.Click("CancelEditButton");
        Assert.EndsWith("[00:03.000]new line", editor.Text);
        Assert.True(editor.IsReadOnly);
    }

    [AvaloniaFact]
    public void KeysInTheLyricsStayInTheLyrics()
    {
        using var scope = new Scope();
        scope.LoadSong(TwoLines);
        var editor = scope.Editor;
        var find = AppShortcuts.Get(AppShortcuts.FindInLyrics).Gesture;

        // ⌘F in the preview opens the editor's find panel, not the song search box.
        editor.TextArea.Focus();
        Dispatcher.UIThread.RunJobs();
        scope.Press(find.Key, find.KeyModifiers);
        var panel = editor.SearchPanel!;
        Assert.True(panel.IsOpened);
        Assert.False(panel.IsReplaceMode); // Read-only: find only.
        Assert.False(scope.Find<SearchField>("SearchTextBox").IsFocused);

        // Enter in the find box goes to the next match (an exact search would report an empty query).
        var box = panel.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_searchTextBox");
        Assert.True(box.IsFocused);
        box.Text = "hello";
        Dispatcher.UIThread.RunJobs();
        var first = editor.SelectionStart;
        scope.Press(Key.Enter);
        Assert.Equal("hello", editor.SelectedText);
        Assert.NotEqual(first, editor.SelectionStart);
        Assert.Empty(scope.Vm.TipErrorMessage);
        Assert.True(box.IsFocused);

        panel.SearchPattern = "again";
        panel.FindNext();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("again", editor.SelectedText);
        panel.Close();

        // While editing, Enter is a new line rather than an exact search, and find can replace.
        scope.Click("EditLyricsButton");
        editor.TextArea.Focus();
        editor.CaretOffset = 0;
        Dispatcher.UIThread.RunJobs();
        scope.Press(Key.Enter);
        Assert.StartsWith(Environment.NewLine + "[00:01.000]hello", editor.Text);
        Assert.Empty(scope.Vm.TipErrorMessage);

        var replace = AppShortcuts.Get(AppShortcuts.ReplaceInLyrics).Gesture;
        scope.Press(replace.Key, replace.KeyModifiers);
        Assert.True(panel.IsOpened);
        Assert.True(panel.IsReplaceMode);
        panel.SearchPattern = "hello";
        panel.ReplacePattern = "hi";
        Dispatcher.UIThread.RunJobs();
        void ClickInPanel(string name)
        {
            var button = panel.GetVisualDescendants().OfType<Avalonia.Controls.Button>().Single(b => b.Name == name);
            Assert.True(button.IsEffectivelyEnabled, name);
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }

        // The panel's own buttons work while focus is in its fields: replace one, then the rest.
        ClickInPanel("ReplaceNextButton");
        Assert.Contains("hi", editor.Text);
        Assert.Contains("hello", editor.Text);
        ClickInPanel("ReplaceAllButton");
        Assert.DoesNotContain("hello", editor.Text);
        Assert.Contains("hi again", scope.Result.PreviewText);
        ClickInPanel("CloseFindButton");
        Assert.False(panel.IsOpened);

        // Leaving the editor drops the replace row.
        scope.Click("FinishEditButton");
        Assert.False(panel.IsReplaceMode);
    }

    [AvaloniaFact]
    public void BarsFitAtTheDefaultWindowSizeWhileEditing() => TestWorkspace.WithLanguage("zh-CN", () =>
    {
        // Headless text uses a stand-in font whose Latin glyphs are wider than real ones, so only the Chinese
        // layout is measured here; the English one is checked in the real window.
        using var scope = new Scope("/Users/someone/Downloads/Music/Lyrics/Albums/2026/");
        scope.LoadSong("[00:01.000]hello");
        var page = scope.Page;
        double Left(Control c) => Avalonia.VisualExtensions.TranslatePoint(c, new Avalonia.Point(0, 0), page)!.Value.X;
        double Right(Control c) => Left(c) + c.Bounds.Width;
        void InOrder(params string[] names)
        {
            for (var i = 1; i < names.Length; i++)
            {
                var (a, b) = (scope.Find<Control>(names[i - 1]), scope.Find<Control>(names[i]));
                Assert.True(Right(a) <= Left(b) + 0.5, $"{names[i - 1]} ends at {Right(a)}, {names[i]} starts at {Left(b)}");
            }
        }

        // A trailing separator is dropped, so the shortened path still ends in the folder's name, and the path is
        // laid out no wider than its button, so it trims instead of being clipped mid-path.
        var text = scope.Find<TextBlock>("SaveFolderText");
        Assert.Equal("/Users/someone/Downloads/Music/Lyrics/Albums/2026", text.Text);
        var folder = scope.Find<Control>("SaveFolderButton").Bounds.Width;
        Assert.True(text.Bounds.Width <= Math.Max(0, folder - 24), $"text {text.Bounds.Width} in button {folder}");

        var paneRight = Right(scope.Find<Control>("LyricPane"));
        InOrder("EncodingSelect", "SaveFolderButton", "SaveButton");
        InOrder("LyricTitle", "FindInLyricsButton", "EditLyricsButton", "CopyLyricsButton");
        Assert.True(Right(scope.Find<Control>("CopyLyricsButton")) <= paneRight);

        scope.Click("EditLyricsButton");
        scope.Result.UpdateEditedText("[00:01.000]edited"); // Shows 还原 too.
        Dispatcher.UIThread.RunJobs();
        InOrder("LyricTitle", "FindReplaceButton", "RevertEditsButton", "CancelEditButton", "FinishEditButton");
        Assert.True(Right(scope.Find<Control>("FinishEditButton")) <= paneRight);
    });

    [AvaloniaFact]
    public void ShortcutsDialogListsTheLyricsKeysInTheirOwnGroup()
    {
        var texts = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(ShortcutsDialog.Build())
            .OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        Assert.Contains(LocalizationService.Get("Shortcuts.Group.Editor"), texts);
        Assert.Contains(LocalizationService.Get("Shortcuts.FindInLyrics"), texts);
        Assert.Contains(LocalizationService.Get("Shortcuts.ReplaceInLyrics"), texts);
        Assert.Contains(LocalizationService.Get("Shortcuts.EditLyrics"), texts);
    }
}
