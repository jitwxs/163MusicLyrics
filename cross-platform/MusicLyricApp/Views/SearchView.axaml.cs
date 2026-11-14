using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;
using AvaloniaEdit;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>Online lyrics page (在线歌词): query, song details, playback, preview and save.</summary>
public partial class SearchView : UserControl
{
    private MainWindowViewModel? _viewModel;

    public SearchView()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBox>("SearchTextBox")!.PointerEntered += SearchTextBox_PointerEntered;
        this.FindControl<BlurSearchView>("BlurResultsView")!.BackRequested += (_, _) =>
            _viewModel?.ExecuteCloseBlurResultsCommand.Execute(null);

        ApplyShortcutTexts();
        LocalizationService.Observe(this, static view => view.ApplyShortcutTexts(), LanguageChangePhase.View);

        // An Arc button measures its content without a width limit, so a long path would be centred and clipped
        // rather than trimmed. Capping the text at the button's inner width lets PathSegmentEllipsis apply.
        var folderButton = this.FindControl<Avalonia.Controls.Button>("SaveFolderButton")!;
        var folderText = this.FindControl<TextBlock>("SaveFolderText")!;
        folderButton.SizeChanged += (_, e) => folderText.MaxWidth =
            Math.Max(0, e.NewSize.Width - folderButton.Padding.Left - folderButton.Padding.Right -
                        folderButton.BorderThickness.Left - folderButton.BorderThickness.Right);

        var editor = LyricEditor;
        LyricEditorSetup.Apply(editor);
        editor.TextChanged += (_, _) =>
        {
            if (!_syncingEditor) _viewModel?.SearchResultViewModel.UpdateEditedText(editor.Text);
        };
    }

    private TextEditor LyricEditor => this.FindControl<TextEditor>("LyricPreview")!;

    private bool _syncingEditor;

    /// <summary>
    /// Shows <see cref="SearchResultViewModel.PreviewText"/>. While editing (a revert) it is one undoable change;
    /// otherwise (a new song or new options) it is a fresh document with no history to undo into.
    /// </summary>
    private void ShowPreviewText()
    {
        if (_viewModel == null) return;

        var result = _viewModel.SearchResultViewModel;
        var editor = LyricEditor;
        var text = result.PreviewText;
        if (editor.Text == text) return;

        _syncingEditor = true;
        try
        {
            if (result.IsEditing)
            {
                editor.Document.Replace(0, editor.Document.TextLength, text);
            }
            else
            {
                editor.Text = text;
                editor.ScrollToHome();
            }
        }
        finally
        {
            _syncingEditor = false;
        }
    }

    private void ApplyEditingState()
    {
        if (_viewModel == null) return;

        var editing = _viewModel.SearchResultViewModel.IsEditing;
        var editor = LyricEditor;
        editor.IsReadOnly = !editing;
        editor.Options.HighlightCurrentLine = editing;
        if (editing)
        {
            // Ready to type where the person was reading.
            Dispatcher.UIThread.Post(() => editor.TextArea.Focus(), DispatcherPriority.Input);
            return;
        }

        // Back to the preview: no replace row, and nothing to undo into (the edit state could not follow it).
        if (editor.SearchPanel is { } panel) panel.IsReplaceMode = false;
        editor.Document.UndoStack.ClearAll();
    }

    /// <summary>Opens the find panel over the lyrics, with replace while editing, seeded with a one-line selection.</summary>
    private void OpenFind(bool replace)
    {
        var editor = LyricEditor;
        if (editor.SearchPanel is not { } panel || !LyricPaneVisible) return;

        var selected = editor.SelectedText;
        if (!string.IsNullOrEmpty(selected) && !selected.Contains('\n'))
        {
            panel.SearchPattern = selected;
        }

        panel.IsReplaceMode = replace && !editor.IsReadOnly;
        panel.Open();
        // The first time, the panel gets its template only on the next layout pass, and with it the find box
        // that Reactivate focuses; so focus once that has happened.
        Dispatcher.UIThread.Post(panel.Reactivate, DispatcherPriority.Loaded);
    }

    private bool LyricPaneVisible => this.FindControl<Control>("LyricPane") is { IsEffectivelyVisible: true };

    private void OnFindInLyricsClick(object? sender, RoutedEventArgs e) => OpenFind(replace: false);

    private void OnFindReplaceClick(object? sender, RoutedEventArgs e) => OpenFind(replace: true);

    private bool IsFocusInLyrics() =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Visual focused &&
        this.FindControl<Control>("LyricPane") is { IsEffectivelyVisible: true } pane &&
        (ReferenceEquals(pane, focused) || pane.IsVisualAncestorOf(focused));

    /// <summary>Search page shortcuts only apply while focus is inside the page.</summary>
    private void BindShortcuts(MainWindowViewModel vm)
    {
        KeyBindings.Clear();
        void Bind(string id, System.Windows.Input.ICommand command)
        {
            foreach (var gesture in AppShortcuts.Get(id).AllGestures)
            {
                KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
            }
        }

        // Key bindings run before the focused control sees the key, so Enter would start a search from the
        // lyrics too: a new line in the editor, the next match in its find box. A binding whose command cannot
        // execute leaves the key to them.
        System.Windows.Input.ICommand OutsideLyrics(System.Windows.Input.ICommand command) =>
            new CommunityToolkit.Mvvm.Input.RelayCommand(() => command.Execute(null),
                () => !IsFocusInLyrics() && command.CanExecute(null));
        Bind(AppShortcuts.ExactSearch, OutsideLyrics(vm.ExecuteExactSearchWithModeCommand));
        Bind(AppShortcuts.FuzzySearch, OutsideLyrics(vm.ExecuteBlurSearchWithModeCommand));
        Bind(AppShortcuts.Save, vm.ExecuteSaveCommand);
        Bind(AppShortcuts.EditLyrics, vm.SearchResultViewModel.ToggleEditCommand);

        // Find keys work while focus is in the lyrics (the editor or its find panel); ⌘F elsewhere stays the
        // song search. They sit on the pane, nearer the focus than the window's bindings, so they win there.
        var pane = this.FindControl<Control>("LyricPane")!;
        pane.KeyBindings.Clear();
        void BindLyrics(string id, Action action)
        {
            var command = new CommunityToolkit.Mvvm.Input.RelayCommand(action);
            foreach (var gesture in AppShortcuts.Get(id).AllGestures)
            {
                pane.KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command });
            }
        }

        BindLyrics(AppShortcuts.FindInLyrics, () => OpenFind(replace: false));
        BindLyrics(AppShortcuts.ReplaceInLyrics, () => OpenFind(replace: true));
        BindLyrics(AppShortcuts.FindNext, () => LyricEditor.SearchPanel?.FindNext());
        BindLyrics(AppShortcuts.FindPrevious, () => LyricEditor.SearchPanel?.FindPrevious());
    }

    /// <summary>Hints that name keys use the same platform-aware text as the shortcuts dialog.</summary>
    private void ApplyShortcutTexts()
    {
        var exact = AppShortcuts.Get(AppShortcuts.ExactSearch);
        var fuzzy = AppShortcuts.Get(AppShortcuts.FuzzySearch);
        var save = AppShortcuts.Get(AppShortcuts.Save);
        var edit = AppShortcuts.Get(AppShortcuts.EditLyrics);
        var find = AppShortcuts.Get(AppShortcuts.FindInLyrics);
        var replace = AppShortcuts.Get(AppShortcuts.ReplaceInLyrics);
        string Tip(string label, AppShortcut s) => LocalizationService.Format("Shortcuts.TooltipFormat", label, s.KeysText);

        this.FindControl<EmptyState>("SongEmptyState")!.Description =
            LocalizationService.Format("Search.EmptyDescription", exact.KeysText, fuzzy.KeysText);
        ToolTip.SetTip(this.FindControl<Control>("SearchButton")!, Tip(exact.Label, exact));
        ToolTip.SetTip(this.FindControl<Control>("FuzzySwitch")!, Tip(fuzzy.Label, fuzzy));
        ToolTip.SetTip(this.FindControl<Control>("SaveButton")!, Tip(save.Label, save));
        ToolTip.SetTip(this.FindControl<Control>("FindInLyricsButton")!, Tip(find.Label, find));
        ToolTip.SetTip(this.FindControl<Control>("FindReplaceButton")!, Tip(replace.Label, replace));
        ToolTip.SetTip(this.FindControl<Control>("FinishEditButton")!,
            Tip(LocalizationService.Get("Editor.DoneTip"), edit));
        ToolTip.SetTip(this.FindControl<Control>("EditLyricsButton")!,
            _viewModel?.SearchResultViewModel.EditUnavailableTip ?? Tip(edit.Label, edit));
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.SearchResultViewModel.PropertyChanged -= SearchResultOnPropertyChanged;
            _viewModel.ConfirmDiscardEditsAsync = null;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel != null)
        {
            _viewModel.SearchResultViewModel.PropertyChanged += SearchResultOnPropertyChanged;
            _viewModel.ConfirmDiscardEditsAsync = () => DialogHelper.ConfirmOnAsync(this,
                LocalizationService.Get("Editor.Discard.Title"),
                LocalizationService.Get("Editor.Discard.Message"),
                LocalizationService.Get("Editor.Discard.Action"), isDanger: true);
            BindShortcuts(_viewModel);
            ApplyShortcutTexts();
            ApplyEditingState();
            ShowPreviewText();
        }
    }

    private async void SearchResultOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SearchResultViewModel.IsEditing):
                ApplyEditingState();
                break;
            case nameof(SearchResultViewModel.PreviewText):
                ShowPreviewText();
                break;
            case nameof(SearchResultViewModel.EditUnavailableTip):
                ApplyShortcutTexts();
                break;
        }

        if (_viewModel != null &&
            e.PropertyName == nameof(SearchResultViewModel.SongName) &&
            _viewModel.SearchResultViewModel.SaveVoMap.Count > 0)
        {
            await UpdateAlbumCover(_viewModel);
        }
    }

    private async Task UpdateAlbumCover(MainWindowViewModel viewModel)
    {
        var image = this.FindControl<Image>("AlbumCoverImage")!;
        var saveVo = viewModel.SearchResultViewModel.SaveVoMap.Values.First();
        if (string.IsNullOrWhiteSpace(saveVo.SongVo.Pics))
        {
            image.Source = null;
            return;
        }

        try
        {
            using var client = NetworkClientFactory.CreateHttpClient();
            var imageData = await client.GetByteArrayAsync(saveVo.SongVo.Pics);
            using var stream = new MemoryStream(imageData);
            image.Source = new Bitmap(stream);
        }
        catch (Exception)
        {
            image.Source = null;
        }
    }

    private async void SearchTextBox_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel is not { SettingBean.Config.AutoReadClipboard: true })
        {
            return;
        }

        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        var message = await clipboard.TryGetTextAsync();
        if (message != null)
        {
            _viewModel.SearchParamViewModel.SearchText = message;
        }
    }
}
