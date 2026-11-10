using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicLyricApp.Core.Localization;

/// <summary>
/// Base for objects whose text comes from <see cref="LocalizationService"/>, such as Select options and list items.
/// Compute such text in a getter (<c>public string Name => LocalizationService.Get(...)</c>) instead of storing it:
/// after a language switch every property is reported as changed, so bindings read the new text.
/// </summary>
public abstract class LocalizedObject : ObservableObject
{
    protected LocalizedObject()
    {
        LocalizationService.Observe(this, static o => o.OnLanguageChanged());
    }

    /// <summary>Runs after a language switch; by default reports every property as changed.</summary>
    protected virtual void OnLanguageChanged() => OnPropertyChanged(string.Empty);
}
