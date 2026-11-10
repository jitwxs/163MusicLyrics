using System;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.ViewModels;

/// <summary>
/// A whole-number setting typed into an Input (bind <see cref="Text"/> and <see cref="Error"/>). Only a number in
/// [min, max] is passed to <c>apply</c>; anything else shows an error and leaves the setting as it was. The error
/// follows UI language switches.
/// </summary>
public sealed class NumberSetting : LocalizedObject
{
    private readonly int _min;
    private readonly int _max;
    private readonly Func<string> _notANumber;
    private readonly Func<string> _outOfRange;
    private readonly Action<int> _apply;
    private string _text = string.Empty;
    private Func<string>? _error;

    /// <param name="outOfRange">Message for a number outside the range.</param>
    /// <param name="notANumber">Message for text that is not a number; the range message when null.</param>
    public NumberSetting(int min, int max, Func<string> outOfRange, Action<int> apply, Func<string>? notANumber = null)
    {
        _min = min;
        _max = max;
        _outOfRange = outOfRange;
        _notANumber = notANumber ?? outOfRange;
        _apply = apply;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value ?? string.Empty)) return;
            _error = !int.TryParse(_text, out var number) ? _notANumber
                : number < _min || number > _max ? _outOfRange
                : null;
            OnPropertyChanged(nameof(Error));
            if (_error == null) _apply(number);
        }
    }

    /// <summary>Why <see cref="Text"/> was not applied; empty when it was.</summary>
    public string Error => _error?.Invoke() ?? string.Empty;

    public bool IsValid => _error == null;

    /// <summary>Shows a saved value.</summary>
    public void Load(int value) => Text = value.ToString();
}
