using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class SearchResultViewModel : ViewModelBase
{
    // 歌曲信息属性
    [ObservableProperty] private string _singer = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSong), nameof(ShowIdleState), nameof(ShowNoLyricsState))]
    private string _songName = string.Empty;

    /// <summary>True once a single song has been rendered; drives the empty state of the search page.</summary>
    public bool HasSong => !string.IsNullOrWhiteSpace(SongName);

    [ObservableProperty] private string _album = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSongLinkText))]
    private string _songLink = string.Empty;

    public bool HasSongLinkText => !string.IsNullOrWhiteSpace(SongLink);

    [ObservableProperty] private string _songDuration = string.Empty;

    [ObservableProperty] private string _publishDate = string.Empty;

    [ObservableProperty] private string _songSource = string.Empty;
    
    /// <summary>The lyrics as generated from the song and the current options.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConsoleOutput), nameof(ShowIdleState), nameof(ShowNoLyricsState),
        nameof(PreviewText), nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(BeginEditCommand), nameof(ToggleEditCommand))]
    private string _consoleOutput = string.Empty;

    /// <summary>How many files the generated lyrics are saved as (several when isolated lyrics get their own file).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit), nameof(EditUnavailableTip))]
    [NotifyCanExecuteChangedFor(nameof(BeginEditCommand), nameof(ToggleEditCommand))]
    private int _outputPartCount = 1;

    // ---- Editing ----
    // The page shows the lyrics in one text editor: read-only as a preview, writable while editing. The editor
    // reports each change through UpdateEditedText; PreviewText is what it shows when it is not being typed in.

    /// <summary>The person's version of the lyrics, or null while they match the generated ones.</summary>
    private string? _editedText;

    /// <summary>What was there when editing began, so 取消 can put it back.</summary>
    private (string? Edited, bool Unsaved) _beforeEdit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyCanExecuteChangedFor(nameof(BeginEditCommand), nameof(FinishEditCommand), nameof(CancelEditCommand),
        nameof(RevertEditsCommand))]
    private bool _isEditing;

    /// <summary>Edits made since the lyrics were last saved; leaving them asks first.</summary>
    [ObservableProperty] private bool _hasUnsavedEdits;

    /// <summary>The lyrics differ from the generated ones; saving writes this version.</summary>
    public bool IsEdited => _editedText != null;

    /// <summary>What the preview shows and saving writes: the edited lyrics, or else the generated ones.</summary>
    public string PreviewText => _editedText ?? ConsoleOutput;

    /// <summary>The edited lyrics, with the platform's line endings, or null when not edited.</summary>
    public string? EditedTextForSave => _editedText == null ? null : NormalizeLineEndings(_editedText, Environment.NewLine);

    /// <summary>
    /// One song with lyrics that are saved as one file. Lyrics split over several files are not editable: the
    /// preview joins them, and there is no telling where one file would end after an edit.
    /// </summary>
    public bool CanEdit => SaveVoMap.Count == 1 && HasConsoleOutput && OutputPartCount == 1;

    public string? EditUnavailableTip => OutputPartCount > 1 ? LocalizationService.Get("Editor.SplitFiles") : null;

    public string PreviewTitle => LocalizationService.Get(
        IsEditing ? "Editor.EditingTitle" : IsEdited ? "Editor.PreviewEdited" : "Search.PreviewTitle");

    /// <summary>Shown on the format selects while edited: changing the format would not reach the edited text.</summary>
    public string? FormatLockedTip => IsEdited ? LocalizationService.Get("Editor.FormatLocked") : null;

    [RelayCommand(CanExecute = nameof(CanBeginEdit))]
    private void BeginEdit()
    {
        _beforeEdit = (_editedText, HasUnsavedEdits);
        IsEditing = true;
    }

    private bool CanBeginEdit() => !IsEditing && CanEdit;

    /// <summary>Keeps the changes and goes back to the preview.</summary>
    [RelayCommand(CanExecute = nameof(IsEditing))]
    private void FinishEdit() => IsEditing = false;

    /// <summary>Puts back the lyrics as they were when editing began and goes back to the preview.</summary>
    [RelayCommand(CanExecute = nameof(IsEditing))]
    private void CancelEdit()
    {
        SetEditedText(_beforeEdit.Edited);
        HasUnsavedEdits = _beforeEdit.Unsaved;
        IsEditing = false;
    }

    /// <summary>⌘E: opens the editor, or keeps the changes and closes it.</summary>
    [RelayCommand(CanExecute = nameof(CanToggleEdit))]
    private void ToggleEdit()
    {
        if (IsEditing) FinishEdit();
        else BeginEdit();
    }

    private bool CanToggleEdit() => IsEditing || CanEdit;

    /// <summary>
    /// Drops every change, not only this session's, and shows the lyrics as generated from the current options.
    /// The editor applies it as one edit, so ⌘Z brings the changes back.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsEditing))]
    private void RevertEdits()
    {
        SetEditedText(null);
        HasUnsavedEdits = !Equals(_beforeEdit.Edited, null) || _beforeEdit.Unsaved;
    }

    /// <summary>Leaves the editor and forgets the edits, for a new song.</summary>
    public void DiscardEdits()
    {
        SetEditedText(null);
        HasUnsavedEdits = false;
        IsEditing = false;
    }

    public void MarkSaved() => HasUnsavedEdits = false;

    /// <summary>The editor's text after a change typed while editing.</summary>
    public void UpdateEditedText(string text)
    {
        if (!IsEditing) return;

        var same = NormalizeLineEndings(text, "\n") == NormalizeLineEndings(ConsoleOutput, "\n");
        var edited = same ? null : text;
        if (edited == _editedText) return;

        _editedText = edited;
        HasUnsavedEdits = true;
        // PreviewText is not raised: the editor already shows this text, and setting it again would reset it.
        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(PreviewTitle));
        OnPropertyChanged(nameof(FormatLockedTip));
        RevertEditsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Options changed the generated lyrics; edits are kept, and without any the editor follows.</summary>
    partial void OnConsoleOutputChanged(string value)
    {
        if (!CanEdit && !IsEdited)
        {
            IsEditing = false;
        }
    }

    private void SetEditedText(string? text)
    {
        var changed = _editedText != text;
        _editedText = text;
        OnPropertyChanged(nameof(PreviewText));
        if (!changed) return;

        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(PreviewTitle));
        OnPropertyChanged(nameof(FormatLockedTip));
    }

    public static string NormalizeLineEndings(string text, string newLine) =>
        System.Text.RegularExpressions.Regex.Replace(text, "\r\n|\r|\n", newLine);

    public bool HasConsoleOutput => !string.IsNullOrWhiteSpace(ConsoleOutput);

    /// <summary>Nothing searched yet: the preview area shows how to start.</summary>
    public bool ShowIdleState => !HasSong && !HasConsoleOutput;

    /// <summary>A song was found but there are no lyrics to preview.</summary>
    public bool ShowNoLyricsState => HasSong && !HasConsoleOutput;
    
    [ObservableProperty] private Dictionary<string, SaveVo> _saveVoMap = new();

    /// <summary>The song map is filled in place, so whoever fills it says when editing may have changed.</summary>
    public void NotifySongsChanged()
    {
        OnPropertyChanged(nameof(CanEdit));
        BeginEditCommand.NotifyCanExecuteChanged();
        ToggleEditCommand.NotifyCanExecuteChanged();
    }

    private SearchSourceEnum? _songSourceEnum;

    public SearchResultViewModel()
    {
        // The source label is derived from the enum, so it follows UI language switches.
        LocalizationService.Observe(this, static vm =>
        {
            if (vm._songSourceEnum is { } source) vm.SongSource = SourceLabel(source);
            vm.OnPropertyChanged(nameof(PreviewTitle));
            vm.OnPropertyChanged(nameof(FormatLockedTip));
            vm.OnPropertyChanged(nameof(EditUnavailableTip));
        });
    }

    public void SetSongSource(SearchSourceEnum source)
    {
        _songSourceEnum = source;
        SongSource = SourceLabel(source);
    }

    public static string SourceLabel(SearchSourceEnum source) => MusicLyricApp.Core.MusicSources.Get(source).Name;

    public void ResetConsoleOutput(string consoleOutput)
    {
        DiscardEdits();
        OutputPartCount = 1;
        _songSourceEnum = null;
        Singer = "";
        SongName = "";
        Album = "";
        SongLink = "";
        SongDuration = "";
        PublishDate = "";
        SongSource = "";
        ConsoleOutput = consoleOutput;
    }
}
