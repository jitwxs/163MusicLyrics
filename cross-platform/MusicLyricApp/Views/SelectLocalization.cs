using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>
/// Keeps Arc <see cref="Select"/> labels in the current UI language.
/// </summary>
/// <remarks>
/// Select caches each option's text when it is first shown and does not follow item property changes, so
/// after a language switch the trigger kept the old language. Changing <see cref="Select.TextSelector"/> makes it
/// recompute every label. Delegates to the same method compare equal, so it is cleared first: otherwise only the
/// first switch is seen as a change. Options' <c>ToString()</c> returns their localized display name.
/// Selects are looked up in the logical tree as well as the visual one: a section that has never been shown has
/// no template yet, so its selects are not in the visual tree, but they already cached their text.
/// </remarks>
public static class SelectLocalization
{
    private static string? Label(object? item) => item?.ToString();

    /// <summary>Applies the selector to every Select under <paramref name="root"/> and refreshes on language change.</summary>
    public static void Attach(Control root)
    {
        void Refresh()
        {
            var selects = root.GetLogicalDescendants().OfType<Select>()
                .Union(root.GetVisualDescendants().OfType<Select>());
            foreach (var select in selects)
            {
                select.TextSelector = null;
                select.TextSelector = Label;
            }
        }

        root.AttachedToVisualTree += (_, _) => Refresh();
        LocalizationService.Instance.PropertyChanged += (_, _) => Refresh();
    }
}
