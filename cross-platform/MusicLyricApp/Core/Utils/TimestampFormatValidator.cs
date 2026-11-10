using System.Linq;
using System.Text.RegularExpressions;

namespace MusicLyricApp.Core.Utils;

/// <summary>
/// Checks a user-written LRC or SRT timestamp format before it is used for output.
/// </summary>
/// <remarks>
/// <see cref="Models.LyricTimestamp.PrintTimestamp"/> replaces <c>HH</c>, <c>mm</c>, <c>ss</c> and one run of
/// <c>S</c>, so a format without minutes or seconds, or with several millisecond runs, produces broken timestamps.
/// Those are errors and the format is not saved. A missing <c>[ ]</c> (LRC) or <c>HH</c> (SRT) still prints a
/// timestamp, but most players reject it, so it is only a warning.
/// </remarks>
public static partial class TimestampFormatValidator
{
    [GeneratedRegex("S+")]
    private static partial Regex MillisecondRunRegex();

    public readonly record struct Result(string? ErrorKey, string? WarningKey)
    {
        public bool IsValid => ErrorKey == null;
    }

    public static Result Validate(string? format, bool isLrc)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return new Result("Settings.Timestamp.Error.Empty", null);
        }

        if (!format.Contains("mm"))
        {
            return new Result("Settings.Timestamp.Error.Minutes", null);
        }

        if (!format.Contains("ss"))
        {
            return new Result("Settings.Timestamp.Error.Seconds", null);
        }

        var runs = MillisecondRunRegex().Matches(format);
        if (runs.Count > 1 || runs.Any(run => run.Length > 3))
        {
            return new Result("Settings.Timestamp.Error.Milliseconds", null);
        }

        if (isLrc && !(format.StartsWith('[') && format.EndsWith(']')))
        {
            return new Result(null, "Settings.Timestamp.Warn.LrcBrackets");
        }

        if (!isLrc && !format.Contains("HH"))
        {
            return new Result(null, "Settings.Timestamp.Warn.SrtHours");
        }

        return new Result(null, null);
    }

    /// <summary>Digits of milliseconds the format prints: 1, 2 or 3 (no <c>S</c> keeps full precision).</summary>
    public static int MillisecondDigits(string? format)
    {
        var run = format == null ? null : MillisecondRunRegex().Match(format);
        return run is { Success: true } && run.Length < 3 ? run.Length : 3;
    }

    /// <summary>
    /// Inserts <paramref name="token"/> at the caret. A millisecond token replaces the existing millisecond run
    /// instead, so switching precision never leaves two runs behind.
    /// </summary>
    /// <returns>The new text and the caret position after the inserted token.</returns>
    public static (string Text, int Caret) InsertToken(string? text, string token, int selectionStart, int selectionEnd)
    {
        text ??= string.Empty;
        var start = System.Math.Clamp(System.Math.Min(selectionStart, selectionEnd), 0, text.Length);
        var end = System.Math.Clamp(System.Math.Max(selectionStart, selectionEnd), 0, text.Length);

        if (token.All(c => c == 'S'))
        {
            var run = MillisecondRunRegex().Match(text);
            if (run.Success)
            {
                start = run.Index;
                end = run.Index + run.Length;
            }
        }

        return (text[..start] + token + text[end..], start + token.Length);
    }
}
