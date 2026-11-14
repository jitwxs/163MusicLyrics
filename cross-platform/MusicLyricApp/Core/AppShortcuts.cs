using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core;

/// <summary>
/// Where a shortcut works: inside the lyrics search page, inside its lyrics pane (where it may take over a global
/// key, as ⌘F finds in the lyrics there), or anywhere in the window.
/// </summary>
public enum ShortcutScope
{
    Search,
    Editor,
    Global
}

/// <summary>One keyboard shortcut. <see cref="Gesture"/> is shown; <see cref="Aliases"/> also trigger it.</summary>
public sealed record AppShortcut(string Id, string LabelKey, ShortcutScope Scope, KeyGesture Gesture,
    IReadOnlyList<KeyGesture> Aliases)
{
    public string Label => LocalizationService.Get(LabelKey);

    public IEnumerable<KeyGesture> AllGestures => Aliases.Prepend(Gesture);

    /// <summary>Keys as separate caps for display, e.g. ["⌘", "Enter"] or ["Ctrl", "Enter"].</summary>
    public IReadOnlyList<string> Keys => AppShortcuts.KeyCaps(Gesture);

    /// <summary>Keys as one string for tooltips, e.g. "⌘ Enter" or "Ctrl + Enter".</summary>
    public string KeysText => AppShortcuts.Format(Gesture);
}

/// <summary>
/// The single list of keyboard shortcuts. Key bindings, the shortcuts dialog and button tooltips are all built
/// from it, so a shortcut is defined in one place. The command modifier is ⌘ on macOS and Ctrl elsewhere.
/// </summary>
public static class AppShortcuts
{
    public const string ExactSearch = "exact-search";
    public const string FuzzySearch = "fuzzy-search";
    public const string Save = "save";
    public const string EditLyrics = "edit-lyrics";
    public const string FindInLyrics = "find-in-lyrics";
    public const string ReplaceInLyrics = "replace-in-lyrics";
    public const string FindNext = "find-next";
    public const string FindPrevious = "find-previous";
    public const string FocusSearch = "focus-search";
    public const string GoSearch = "go-search";
    public const string GoBatch = "go-batch";
    public const string GoConvert = "go-convert";
    public const string GoLocal = "go-local";
    public const string OpenSettings = "open-settings";
    public const string ShowShortcuts = "show-shortcuts";

    /// <summary>How Enter is written in texts; the shortcut list draws it as the return glyph instead.</summary>
    public const string EnterKeyName = "Enter";

    private static bool IsMac => OperatingSystem.IsMacOS();

    /// <summary>⌘ on macOS, Ctrl elsewhere — the platform's usual modifier for app commands.</summary>
    public static KeyModifiers CommandModifier => IsMac ? KeyModifiers.Meta : KeyModifiers.Control;

    public static IReadOnlyList<AppShortcut> All { get; } = Build();

    public static AppShortcut Get(string id) => All.Single(s => s.Id == id);

    public static IEnumerable<AppShortcut> InScope(ShortcutScope scope) => All.Where(s => s.Scope == scope);

    private static IReadOnlyList<AppShortcut> Build()
    {
        var cmd = CommandModifier;

        // Earlier releases used Ctrl on every platform; keep those working on macOS as aliases.
        IReadOnlyList<KeyGesture> LegacyCtrl(Key key) =>
            IsMac ? [new KeyGesture(key, KeyModifiers.Control)] : [];

        AppShortcut S(string id, string label, ShortcutScope scope, Key key, KeyModifiers modifiers,
            IReadOnlyList<KeyGesture>? aliases = null) =>
            new(id, label, scope, new KeyGesture(key, modifiers), aliases ?? []);

        return
        [
            S(ExactSearch, "Shortcuts.ExactSearch", ShortcutScope.Search, Key.Enter, KeyModifiers.None),
            S(FuzzySearch, "Shortcuts.FuzzySearch", ShortcutScope.Search, Key.Enter, cmd, LegacyCtrl(Key.Enter)),
            S(Save, "Shortcuts.Save", ShortcutScope.Search, Key.S, cmd, LegacyCtrl(Key.S)),
            S(EditLyrics, "Shortcuts.EditLyrics", ShortcutScope.Search, Key.E, cmd),
            // Inside the lyric editor ⌘F finds in the lyrics; everywhere else it focuses the song search box.
            S(FindInLyrics, "Shortcuts.FindInLyrics", ShortcutScope.Editor, Key.F, cmd),
            IsMac
                ? S(ReplaceInLyrics, "Shortcuts.ReplaceInLyrics", ShortcutScope.Editor, Key.F, cmd | KeyModifiers.Alt)
                : S(ReplaceInLyrics, "Shortcuts.ReplaceInLyrics", ShortcutScope.Editor, Key.H, cmd),
            // Each platform's usual keys: ⌘G and ⇧⌘G on macOS, F3 and Shift+F3 elsewhere.
            IsMac
                ? S(FindNext, "Shortcuts.FindNext", ShortcutScope.Editor, Key.G, cmd)
                : S(FindNext, "Shortcuts.FindNext", ShortcutScope.Editor, Key.F3, KeyModifiers.None),
            IsMac
                ? S(FindPrevious, "Shortcuts.FindPrevious", ShortcutScope.Editor, Key.G, cmd | KeyModifiers.Shift)
                : S(FindPrevious, "Shortcuts.FindPrevious", ShortcutScope.Editor, Key.F3, KeyModifiers.Shift),
            S(FocusSearch, "Shortcuts.FocusSearch", ShortcutScope.Global, Key.F, cmd),
            S(GoSearch, "Shortcuts.GoSearch", ShortcutScope.Global, Key.D1, cmd),
            S(GoBatch, "Shortcuts.GoBatch", ShortcutScope.Global, Key.D2, cmd),
            S(GoConvert, "Shortcuts.GoConvert", ShortcutScope.Global, Key.D3, cmd),
            S(GoLocal, "Shortcuts.GoLocal", ShortcutScope.Global, Key.D4, cmd),
            S(OpenSettings, "Shortcuts.OpenSettings", ShortcutScope.Global, Key.OemComma, cmd),
            S(ShowShortcuts, "Shortcuts.ShowShortcuts", ShortcutScope.Global, Key.Oem2, cmd)
        ];
    }

    public static IReadOnlyList<string> KeyCaps(KeyGesture gesture)
    {
        var caps = new List<string>();
        var m = gesture.KeyModifiers;
        if (IsMac)
        {
            if (m.HasFlag(KeyModifiers.Control)) caps.Add("⌃");
            if (m.HasFlag(KeyModifiers.Alt)) caps.Add("⌥");
            if (m.HasFlag(KeyModifiers.Shift)) caps.Add("⇧");
            if (m.HasFlag(KeyModifiers.Meta)) caps.Add("⌘");
        }
        else
        {
            if (m.HasFlag(KeyModifiers.Control)) caps.Add("Ctrl");
            if (m.HasFlag(KeyModifiers.Alt)) caps.Add("Alt");
            if (m.HasFlag(KeyModifiers.Shift)) caps.Add("Shift");
            if (m.HasFlag(KeyModifiers.Meta)) caps.Add("Win");
        }

        caps.Add(KeyName(gesture.Key));
        return caps;
    }

    public static string Format(KeyGesture gesture) => string.Join(IsMac ? " " : " + ", KeyCaps(gesture));

    private static string KeyName(Key key) => key switch
    {
        Key.Enter => EnterKeyName,
        Key.OemComma => ",",
        Key.Oem2 => "/",
        >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(),
        _ => key.ToString()
    };
}
