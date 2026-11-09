using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

/// <summary>Where a batch row is in its life cycle; drives the status badge tone.</summary>
public enum BatchItemStateEnum
{
    READY,
    FAILED,
    SAVED,
    SKIPPED
}

public partial class BatchSearchItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady), nameof(IsFailed), nameof(IsSaved), nameof(IsSkipped))]
    private BatchItemStateEnum _state;

    public bool IsReady => State == BatchItemStateEnum.READY;
    public bool IsFailed => State == BatchItemStateEnum.FAILED;
    public bool IsSaved => State == BatchItemStateEnum.SAVED;
    public bool IsSkipped => State == BatchItemStateEnum.SKIPPED;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _error = "";

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _songSource = "";
    [ObservableProperty] private string _songId = "";
    [ObservableProperty] private string _songName = "";
    [ObservableProperty] private string _singer = "";
    [ObservableProperty] private string _album = "";
    [ObservableProperty] private SearchSourceEnum _sourceEnum;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private int _progress;
}
