using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
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
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConsoleOutput), nameof(ShowIdleState), nameof(ShowNoLyricsState))]
    private string _consoleOutput = string.Empty;

    public bool HasConsoleOutput => !string.IsNullOrWhiteSpace(ConsoleOutput);

    /// <summary>Nothing searched yet: the preview area shows how to start.</summary>
    public bool ShowIdleState => !HasSong && !HasConsoleOutput;

    /// <summary>A song was found but there are no lyrics to preview.</summary>
    public bool ShowNoLyricsState => HasSong && !HasConsoleOutput;
    
    [ObservableProperty] private Dictionary<string, SaveVo> _saveVoMap = new();

    private SearchSourceEnum? _songSourceEnum;

    public SearchResultViewModel()
    {
        // The source label is derived from the enum, so it follows UI language switches.
        MusicLyricApp.Core.Localization.LocalizationService.Observe(this, static vm =>
        {
            if (vm._songSourceEnum is { } source) vm.SongSource = SourceLabel(source);
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
