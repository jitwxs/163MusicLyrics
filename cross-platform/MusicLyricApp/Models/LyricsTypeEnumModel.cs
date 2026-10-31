using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicLyricApp.Models;

public partial class LyricsTypeEnumModel : ObservableObject
{
    private readonly LyricsTypeEnum _type;
    public int Id => (int)_type;
    public string Name => MusicLyricApp.Core.Localization.LocalizationService.Get($"Enum.{_type.GetType().Name}.{_type}");

    public LyricsTypeEnumModel(LyricsTypeEnum type)
    {
        _type = type;
        MusicLyricApp.Core.Localization.LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Name));
    }

    [ObservableProperty] private bool _isSelected;
}
