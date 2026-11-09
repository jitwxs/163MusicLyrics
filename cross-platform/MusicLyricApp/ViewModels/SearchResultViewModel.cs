using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

public partial class SearchResultViewModel : ViewModelBase
{
    // 歌曲信息属性
    [ObservableProperty] private string _singer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSong))]
    private string _songName;

    /// <summary>True once a single song has been rendered; drives the empty state of the search page.</summary>
    public bool HasSong => !string.IsNullOrWhiteSpace(SongName);

    [ObservableProperty] private string _album;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSongLinkText))]
    private string _songLink;

    public bool HasSongLinkText => !string.IsNullOrWhiteSpace(SongLink);

    [ObservableProperty] private string _songDuration;

    [ObservableProperty] private string _publishDate;

    [ObservableProperty] private string _songSource;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConsoleOutput))]
    private string _consoleOutput;

    public bool HasConsoleOutput => !string.IsNullOrWhiteSpace(ConsoleOutput);
    
    [ObservableProperty] private Dictionary<string, SaveVo> _saveVoMap = new();

    public void ResetConsoleOutput(string consoleOutput)
    {
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
