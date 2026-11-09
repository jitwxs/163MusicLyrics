using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MusicLyricApp.Core;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;
using TKCode.Arc.DesignTokens;

namespace MusicLyricApp.Views;

/// <summary>Body of the keyboard shortcuts dialog, built from <see cref="AppShortcuts"/>.</summary>
public static class ShortcutsDialog
{
    public static Control Build()
    {
        var root = new StackPanel { MinWidth = 360 };
        ArcTokens.Bind(root, StackPanel.SpacingProperty, ArcDesignTokenKind.Space5);
        root.Children.Add(Group("Shortcuts.Group.Search", "Shortcuts.Group.SearchHint", ShortcutScope.Search));
        root.Children.Add(Group("Shortcuts.Group.Global", "Shortcuts.Group.GlobalHint", ShortcutScope.Global));
        return root;
    }

    private static Control Group(string titleKey, string hintKey, ShortcutScope scope)
    {
        var group = new StackPanel();
        ArcTokens.Bind(group, StackPanel.SpacingProperty, ArcDesignTokenKind.Space2);

        var title = new TextBlock { Text = LocalizationService.Get(titleKey), FontWeight = FontWeight.SemiBold };
        ArcTokens.Bind(title, TextBlock.ForegroundProperty, ArcDesignTokenKind.Foreground);
        var hint = new TextBlock { Text = LocalizationService.Get(hintKey), TextWrapping = TextWrapping.Wrap };
        ArcTokens.Bind(hint, TextBlock.ForegroundProperty, ArcDesignTokenKind.TextSecondary);
        ArcTokens.Bind(hint, TextBlock.FontSizeProperty, ArcDesignTokenKind.TypeSM);
        group.Children.Add(title);
        group.Children.Add(hint);

        foreach (var shortcut in AppShortcuts.InScope(scope))
        {
            group.Children.Add(Row(shortcut));
        }

        return group;
    }

    private static Control Row(AppShortcut shortcut)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 2) };
        var label = new TextBlock { Text = shortcut.Label, VerticalAlignment = VerticalAlignment.Center };
        ArcTokens.Bind(label, TextBlock.ForegroundProperty, ArcDesignTokenKind.Foreground);
        row.Children.Add(label);

        var keys = new StackPanel { Orientation = Orientation.Horizontal, [Grid.ColumnProperty] = 1 };
        ArcTokens.Bind(keys, StackPanel.SpacingProperty, ArcDesignTokenKind.Space1);
        foreach (var key in shortcut.Keys)
        {
            keys.Children.Add(KeyCap(key));
        }

        row.Children.Add(keys);
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
