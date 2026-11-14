using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Localization;
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

/// <summary>A download manager row. Its source and status text are derived from enums, so they follow the UI language.</summary>
public partial class BatchSearchItemViewModel : LocalizedObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady), nameof(IsFailed), nameof(IsSaved), nameof(IsSkipped), nameof(Status))]
    private BatchItemStateEnum _state;

    public string Status => LocalizationService.Get(State switch
    {
        BatchItemStateEnum.FAILED => "Status.Batch.Failed",
        BatchItemStateEnum.SAVED => "Status.Batch.Saved",
        BatchItemStateEnum.SKIPPED => "Status.Batch.Skipped",
        _ => "Status.Batch.Ready",
    });

    public bool IsReady => State == BatchItemStateEnum.READY;
    public bool IsFailed => State == BatchItemStateEnum.FAILED;
    public bool IsSaved => State == BatchItemStateEnum.SAVED;
    public bool IsSkipped => State == BatchItemStateEnum.SKIPPED;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _error = "";

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _songId = "";
    [ObservableProperty] private string _songName = "";
    [ObservableProperty] private string _singer = "";
    [ObservableProperty] private string _album = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SongSource))]
    private SearchSourceEnum _sourceEnum;

    public string SongSource => SearchResultViewModel.SourceLabel(SourceEnum);
    [ObservableProperty] private int _progress;
}
