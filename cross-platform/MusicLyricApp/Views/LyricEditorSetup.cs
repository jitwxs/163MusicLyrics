using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Search;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;
using TKCode.Arc.DesignTokens;

namespace MusicLyricApp.Views;

/// <summary>
/// Dresses an AvaloniaEdit <see cref="TextEditor"/> for lyrics in Arc's tokens. AvaloniaEdit's own Fluent and Simple
/// themes read Fluent or Simple resources this app does not load, so App.axaml merges only its base templates and
/// this class supplies the few resources they ask for, again whenever the theme changes.
/// </summary>
public static class LyricEditorSetup
{
    public static void Apply(TextEditor editor)
    {
        editor.ShowLineNumbers = true;
        editor.WordWrap = true;
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;
        editor.Options.AllowScrollBelowDocument = false;
        editor.Padding = new Avalonia.Thickness(0, 8, 8, 8);
        editor.LineNumbersMargin = new Avalonia.Thickness(16, 0, 12, 0);
        // The editor installs its own find panel (find and replace, every match highlighted, Escape closes) when
        // it gets its template; its theme is Views/LyricSearchPanelTheme.axaml. The panel's own messages ("2 of 5")
        // come from AvaloniaEdit's resources, so they follow the app's language rather than the system's.
        ApplyLanguage();
        LocalizationService.Observe(editor, static _ => ApplyLanguage());

        // The panel is created with the editor's template and gets its own template when first shown.
        editor.TemplateApplied += (_, _) =>
        {
            if (editor.SearchPanel is { } panel) panel.TemplateApplied += (_, e) => WireFindButtons(panel, e.NameScope);
        };

        // Tokens resolve only once the editor is in a window's tree.
        editor.AttachedToVisualTree += (_, _) => ApplyTheme(editor);
        editor.ActualThemeVariantChanged += (_, _) =>
        {
            if (editor.IsAttachedToVisualTree()) ApplyTheme(editor);
        };
    }

    /// <summary>Find and replace buttons call the panel directly (see Views/LyricSearchPanelTheme.axaml).</summary>
    private static void WireFindButtons(SearchPanel panel, INameScope names)
    {
        void On(string name, Action action)
        {
            if (names.Find<Avalonia.Controls.Button>(name) is { } button) button.Click += (_, _) => action();
        }

        On("FindPreviousButton", panel.FindPrevious);
        On("FindNextButton", () => panel.FindNext());
        On("CloseFindButton", panel.Close);
        On("ReplaceNextButton", panel.ReplaceNext);
        On("ReplaceAllButton", panel.ReplaceAll);
    }

    /// <summary>AvaloniaEdit has Simplified Chinese; Traditional Chinese reads it better than the English fallback.</summary>
    private static void ApplyLanguage()
    {
        var language = LocalizationService.Instance.Language;
        SR.Culture = new CultureInfo(language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh-Hans" : language);
    }

    private static void ApplyTheme(TextEditor editor)
    {
        Color C(ArcDesignTokenKind kind) => ArcTokens.GetColor(editor, kind);
        IBrush B(ArcDesignTokenKind kind) => new SolidColorBrush(C(kind));

        editor.FontFamily = ArcTokens.Get<FontFamily>(editor, ArcDesignTokenKind.MonoFontFamily);
        editor.FontSize = ArcTokens.Get<double>(editor, ArcDesignTokenKind.TypeSM);
        editor.Foreground = B(ArcDesignTokenKind.Foreground);
        editor.Background = Brushes.Transparent;
        editor.LineNumbersForeground = B(ArcDesignTokenKind.TextMuted);
        editor.TextArea.TextView.CurrentLineBackground = B(ArcDesignTokenKind.SurfaceMuted);
        editor.TextArea.TextView.CurrentLineBorder = new Pen(Brushes.Transparent);
        editor.TextArea.CaretBrush = B(ArcDesignTokenKind.Foreground);

        // Matches stand out from the selection: a warm tint for every match, the accent tint for the one selected.
        // The editor hands this brush on to its find panel.
        var matches = C(ArcDesignTokenKind.Warning);
        editor.SearchResultsBrush = new SolidColorBrush(Color.FromArgb(0x59, matches.R, matches.G, matches.B));

        // Read by AvaloniaEdit's base templates (TextArea and SearchPanel) as dynamic resources.
        // Strong enough to mark the current match over the match tint.
        var accent = C(ArcDesignTokenKind.Accent);
        editor.Resources["TextAreaSelectionBrush"] = new SolidColorBrush(Color.FromArgb(0x66, accent.R, accent.G, accent.B));
        editor.Resources["SearchPanelBackgroundBrush"] = B(ArcDesignTokenKind.SurfaceRaised);
        editor.Resources["SearchPanelBorderBrush"] = B(ArcDesignTokenKind.Border);
        editor.Resources["SearchPanelFontSize"] = ArcTokens.Get<double>(editor, ArcDesignTokenKind.TypeSM);
        editor.Resources["SearchPanelFontFamily"] = ArcTokens.Get<FontFamily>(editor, ArcDesignTokenKind.BodyFontFamily);

        editor.SyntaxHighlighting = Highlighting(C(ArcDesignTokenKind.TextSecondary));
    }

    /// <summary>Timestamps and tags in a quieter color, so the words read first. Covers LRC, verbatim and SRT.</summary>
    private static IHighlightingDefinition Highlighting(Color quiet)
    {
        var hex = $"#{quiet.R:X2}{quiet.G:X2}{quiet.B:X2}";
        var xshd = $$"""
            <SyntaxDefinition name="Lyrics" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
              <Color name="Time" foreground="{{hex}}" />
              <RuleSet>
                <Rule color="Time">^\[[A-Za-z]+:[^\]]*\]\s*$</Rule>
                <Rule color="Time">[\[&lt;(]\d{1,3}:\d{1,2}(?:[.:]\d{1,3})?[\]&gt;)]</Rule>
                <Rule color="Time">\d{2}:\d{2}:\d{2},\d{3}\s*--&gt;\s*\d{2}:\d{2}:\d{2},\d{3}</Rule>
                <Rule color="Time">^\d+\s*$</Rule>
              </RuleSet>
            </SyntaxDefinition>
            """;
        using var reader = XmlReader.Create(new StringReader(xshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
