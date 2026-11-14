using Avalonia;
using MusicLyricApp.Models;
using TKCode.Arc;

namespace MusicLyricApp.Core.Service;

/// <summary>
/// Maps the persisted <see cref="ThemeModeEnum"/> onto Arc's appearance and Avalonia's theme variant.
/// </summary>
public static class ThemeService
{
    /// <summary>Arc accent used across the app; neutral between NetEase red and QQ green.</summary>
    public const ArcAccentPreset Accent = ArcAccentPreset.Violet;

    public static ArcAppearance ToAppearance(ThemeModeEnum mode) => mode switch
    {
        ThemeModeEnum.LIGHT => ArcAppearance.Light,
        ThemeModeEnum.DARK => ArcAppearance.Dark,
        _ => ArcAppearance.System
    };

    /// <summary>
    /// Applies the mode at run time. Arc owns <see cref="Application.RequestedThemeVariant"/> and resolves
    /// <see cref="ArcAppearance.System"/> to the OS appearance. No-op before the application exists.
    /// </summary>
    public static void Apply(ThemeModeEnum mode)
    {
        if (Application.Current == null)
        {
            return;
        }

        ArcTheme.Apply(t => t.Appearance(ToAppearance(mode)));
    }
}
