using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Service.LocalMusic;

public sealed record LocalSaveOptions(string ScanRoot, string OutputRoot, bool BesideAudio, bool Overwrite);
public sealed record LocalWriteResult(bool Skipped, IReadOnlyList<string> Paths);

public sealed class LocalLyricWriter
{
    public static string OutputStem(string audioPath, LocalSaveOptions options)
    {
        var root = Path.GetFullPath(options.ScanRoot);
        var relative = Path.GetRelativePath(root, Path.GetFullPath(audioPath));
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new IOException(LocalizationService.Get("LocalScan.OutsideRoot"));
        var output = options.BesideAudio ? Path.GetFullPath(audioPath)
            : Path.Combine(Path.GetFullPath(options.OutputRoot), relative);
        return Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output));
    }

    public static string Extension(SettingBean settings) => settings.Param.OutputFileFormat == OutputFormatEnum.SRT ? ".srt" : ".lrc";

    public static bool HasExistingLyrics(string audioPath, LocalSaveOptions options, SettingBean settings)
    {
        var stem = OutputStem(audioPath, options);
        var directory = Path.GetDirectoryName(stem)!;
        if (!Directory.Exists(directory)) return false;
        var pattern = "^" + Regex.Escape(Path.GetFileName(stem)) + @"(?:-\d+)?" + Regex.Escape(Extension(settings)) + "$";
        return Directory.EnumerateFiles(directory).Any(p => Regex.IsMatch(Path.GetFileName(p), pattern, RegexOptions.IgnoreCase));
    }

    public async Task<LocalWriteResult> WriteAsync(string audioPath, LyricVo lyrics, SettingBean settings,
        LocalSaveOptions options, CancellationToken ct, Func<IReadOnlyList<string>, bool>? canWrite = null)
    {
        ct.ThrowIfCancellationRequested();
        if (!options.Overwrite && HasExistingLyrics(audioPath, options, settings)) return new(true, []);
        var outputs = await LyricUtils.GetOutputContent(lyrics, settings);
        ct.ThrowIfCancellationRequested();
        if (outputs.Count == 0 || outputs.All(string.IsNullOrWhiteSpace))
            throw new MusicLyricException(LocalizationService.Get("LocalScan.NoOutput"));
        var stem = OutputStem(audioPath, options);
        var paths = outputs.Select((_, i) => stem + (outputs.Count > 1 ? $"-{i}" : "") + Extension(settings)).ToArray();
        if (canWrite != null && !canWrite(paths))
            throw new IOException(LocalizationService.Get("LocalScan.OutputCollision"));
        Directory.CreateDirectory(Path.GetDirectoryName(stem)!);
        var staged = new List<string>();
        try
        {
            // Stage complete files in the destination filesystem so rename never exposes partial contents.
            for (var i = 0; i < outputs.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var temporary = paths[i] + "." + Guid.NewGuid().ToString("N") + ".tmp";
                staged.Add(temporary);
                await File.WriteAllTextAsync(temporary, outputs[i], GlobalUtils.GetEncoding(settings.Param.Encoding), ct);
            }
            ct.ThrowIfCancellationRequested();
            if (!options.Overwrite && HasExistingLyrics(audioPath, options, settings)) return new(true, []);
            // No cancellation points inside this short commit phase. Each rename is atomic.
            for (var i = 0; i < paths.Length; i++) File.Move(staged[i], paths[i], options.Overwrite);
            return new(false, paths);
        }
        finally
        {
            foreach (var temporary in staged)
                if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
