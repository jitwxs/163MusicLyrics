using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;
using TKCode.Arc.DesignTokens;

namespace MusicLyricApp.Views;

/// <summary>
/// The 快捷键 settings section, built from <see cref="AppShortcuts"/>: one settings group per scope, one row per
/// shortcut, so a longer list scrolls with the page instead of outgrowing a dialog.
/// </summary>
public static class ShortcutList
{
    public static Control Build()
    {
        var root = new StackPanel();
        ArcTokens.Bind(root, StackPanel.SpacingProperty, ArcDesignTokenKind.Space6);
        root.Children.Add(Group("Shortcuts.Group.Global", "Shortcuts.Group.GlobalHint", ShortcutScope.Global));
        root.Children.Add(Group("Shortcuts.Group.Search", "Shortcuts.Group.SearchHint", ShortcutScope.Search));
        root.Children.Add(Group("Shortcuts.Group.Editor", "Shortcuts.Group.EditorHint", ShortcutScope.Editor));
        return root;
    }

    private static Control Group(string titleKey, string hintKey, ShortcutScope scope)
    {
        var group = new SettingsGroup
        {
            Header = LocalizationService.Get(titleKey),
            Description = LocalizationService.Get(hintKey)
        };
        foreach (var shortcut in AppShortcuts.InScope(scope))
        {
            group.Items.Add(Row(shortcut));
        }

        return group;
    }

    private static Control Row(AppShortcut shortcut)
    {
        var keys = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        ArcTokens.Bind(keys, StackPanel.SpacingProperty, ArcDesignTokenKind.Space1);
        foreach (var key in shortcut.Keys)
        {
            keys.Children.Add(KeyCap(key));
        }

        var row = new SettingsRow { Header = shortcut.Label, Content = keys };
        AutomationExtensions(row, shortcut);
        return row;
    }

    /// <summary>A key drawn as a small cap, like a key on the keyboard.</summary>
    private static Control KeyCap(string key)
    {
        var text = new TextBlock { Text = key, HorizontalAlignment = HorizontalAlignment.Center };
        ArcTokens.Bind(text, TextBlock.FontFamilyProperty, ArcDesignTokenKind.MonoFontFamily);
        ArcTokens.Bind(text, TextBlock.FontSizeProperty, ArcDesignTokenKind.TypeSM);
        ArcTokens.Bind(text, TextBlock.ForegroundProperty, ArcDesignTokenKind.Foreground);

        var cap = new Border { MinWidth = 28, Padding = new Thickness(8, 2), BorderThickness = new Thickness(1), Child = text };
        ArcTokens.Bind(cap, Border.BackgroundProperty, ArcDesignTokenKind.SurfaceMuted);
        ArcTokens.Bind(cap, Border.BorderBrushProperty, ArcDesignTokenKind.Border);
        ArcTokens.Bind(cap, Border.CornerRadiusProperty, ArcDesignTokenKind.RadiusControl);
        return cap;
    }

    /// <summary>Screen readers read the row as "label, keys" instead of separate caps.</summary>
    private static void AutomationExtensions(Control row, AppShortcut shortcut) =>
        Avalonia.Automation.AutomationProperties.SetName(row, $"{shortcut.Label}, {shortcut.KeysText}");
}
