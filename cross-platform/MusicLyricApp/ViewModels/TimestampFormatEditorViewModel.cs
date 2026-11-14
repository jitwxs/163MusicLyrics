using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.ViewModels;

/// <summary>One choice in the timestamp format select: a ready-made format, or "Custom…" when <see cref="Format"/> is null.</summary>
public sealed class TimestampPreset(string? format) : LocalizedObject
{
    public string? Format { get; } = format;

    public bool IsCustom => Format == null;

    public string DisplayName => Format ?? LocalizationService.Get("Settings.Timestamp.Custom");

    public override string ToString() => DisplayName;
}

/// <summary>A button that inserts one format token (HH, mm, ss, SSS, SS, S) into a custom format.</summary>
public sealed class TimestampToken(string token) : LocalizedObject
{
    public string Token { get; } = token;

    public string Label => LocalizationService.Get("Settings.Timestamp.Token." + Token);

    public string Tip => LocalizationService.Get("Settings.Timestamp.TokenTip." + Token);
}

/// <summary>
/// Picks an LRC or SRT timestamp format from presets, or edits a custom one with token buttons, a live preview and
/// validation. Only a valid format is passed to <c>commit</c>, so the saved setting is never broken.
/// </summary>
public partial class TimestampFormatEditorViewModel : ObservableObject
{
    /// <summary>The sample time shown in the preview: 1:23.456, which shows truncation and rounding apart.</summary>
    public const long SampleMilliseconds = 83_456;

    private readonly bool _isLrc;
    private readonly Func<DotTypeEnum> _dotType;
    private readonly Action<string> _commit;
    private bool _loading;

    public ObservableCollection<TimestampPreset> Presets { get; }

    public ObservableCollection<TimestampToken> Tokens { get; } =
        [new("HH"), new("mm"), new("ss"), new("SSS"), new("SS"), new("S")];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private TimestampPreset? _selectedPreset;

    /// <summary>The format being edited; equals the selected preset's format unless "Custom…" is chosen.</summary>
    [ObservableProperty] private string _text = string.Empty;

    [ObservableProperty] private string _preview = string.Empty;

    [ObservableProperty] private string _error = string.Empty;

    [ObservableProperty] private string _warning = string.Empty;

    /// <summary>The last valid format, the one in the settings.</summary>
    public string Format { get; private set; } = string.Empty;

    public bool IsCustom => SelectedPreset?.IsCustom == true;

    public TimestampFormatEditorViewModel(bool isLrc, Func<DotTypeEnum> dotType, Action<string> commit)
    {
        _isLrc = isLrc;
        _dotType = dotType;
        _commit = commit;
        Presets = isLrc
            ?
            [
                new("[mm:ss.SSS]"),
                new("[mm:ss.SS]"),
                new("[mm:ss.S]"),
                new("[mm:ss]"),
                new(null),
            ]
            :
            [
                new("HH:mm:ss,SSS"),
                new("HH:mm:ss.SSS"),
                new(null),
            ];

        LocalizationService.Observe(this, static editor => editor.Evaluate(commit: false));
    }

    /// <summary>Shows a saved format: its preset when one matches exactly, otherwise "Custom…" with the text.</summary>
    public void Load(string? format)
    {
        _loading = true;
        try
        {
            Format = format ?? string.Empty;
            Text = Format;
            SelectedPreset = Presets.FirstOrDefault(p => p.Format == Format) ?? Presets.Single(p => p.IsCustom);
        }
        finally
        {
            _loading = false;
        }

        Evaluate();
    }

    partial void OnSelectedPresetChanged(TimestampPreset? value)
    {
        // "Custom…" starts from the current format, so the user edits rather than retypes it.
        if (_loading || value?.Format == null) return;
        Text = value.Format;
    }

    partial void OnTextChanged(string value)
    {
        if (_loading) return;
        Evaluate();
    }

    /// <summary>Rebuilds the preview, for example after the millisecond rounding rule changed.</summary>
    public void RefreshPreview() => Evaluate(commit: false);

    private void Evaluate(bool commit = true)
    {
        var result = TimestampFormatValidator.Validate(Text, _isLrc);
        Error = result.ErrorKey == null ? string.Empty : LocalizationService.Get(result.ErrorKey);
        Warning = result.WarningKey == null ? string.Empty : LocalizationService.Get(result.WarningKey);

        if (result.IsValid && commit && Text != Format)
        {
            Format = Text;
            _commit(Text);
        }

        // While the text is invalid the saved format still applies, so the preview keeps showing it. The row keeps
        // its height too, instead of jumping each time a keystroke makes the format briefly invalid.
        var shown = result.IsValid ? Text : Format;
        Preview = string.IsNullOrEmpty(shown)
            ? string.Empty
            : LocalizationService.Format("Settings.Timestamp.Preview",
                new LyricTimestamp(SampleMilliseconds).PrintTimestamp(shown, _dotType()));
    }
}
