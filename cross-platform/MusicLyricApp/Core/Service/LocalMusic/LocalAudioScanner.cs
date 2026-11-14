using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MusicLyricApp.Core.Service.LocalMusic;

public sealed record LocalAudioFile(string Path, string Title, string Artist, string Album, long Duration,
    string? Error = null);

public interface IAudioMetadataReader
{
    LocalAudioFile Read(string path);
}

/// <summary>Reads tags without modifying the audio. Filename parsing only fills missing tags.</summary>
public sealed class AudioMetadataReader : IAudioMetadataReader
{
    public LocalAudioFile Read(string path)
    {
        using var file = TagLib.File.Create(path);
        var (title, artist) = ParseFileName(path);
        return new LocalAudioFile(path,
            string.IsNullOrWhiteSpace(file.Tag.Title) ? title : file.Tag.Title.Trim(),
            file.Tag.Performers?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                ? string.Join(" / ", file.Tag.Performers.Where(x => !string.IsNullOrWhiteSpace(x))) : artist,
            file.Tag.Album?.Trim() ?? "", (long)file.Properties.Duration.TotalMilliseconds);
    }

    public static (string Title, string Artist) ParseFileName(string path)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(path).Trim();
        // Preserve numeric song titles; only remove a track number when followed by a separator and text.
        stem = Regex.Replace(stem, @"^\d{1,3}\s*[-._]\s*(?=\S)", "");
        var parts = Regex.Split(stem, @"\s+[-–—]\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
        return parts.Length == 2 ? (parts[1].Trim(), parts[0].Trim()) : (stem, "");
    }
}

public sealed class LocalAudioScanner(IAudioMetadataReader? reader = null)
{
    private readonly IAudioMetadataReader _reader = reader ?? new AudioMetadataReader();
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".m4a", ".ogg", ".wav" };

    public void Scan(string root, bool recursive, IProgress<LocalAudioFile> files,
        IProgress<string> issues, CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(System.IO.Path.GetFullPath(root));
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] entries;
            try { entries = Directory.GetFileSystemEntries(directory); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Report($"{directory}: {ex.Message}");
                continue;
            }
            Array.Sort(entries, StringComparer.Ordinal);
            foreach (var path in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var attributes = File.GetAttributes(path);
                    // Do not follow symlinks: prevents loops and escaping the selected directory.
                    if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if (recursive) pending.Push(path);
                        continue;
                    }
                    if (!Extensions.Contains(System.IO.Path.GetExtension(path))) continue;
                    files.Report(_reader.Read(path));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (Extensions.Contains(System.IO.Path.GetExtension(path)))
                        files.Report(new LocalAudioFile(path, "", "", "", 0, ex.Message));
                    else issues.Report($"{path}: {ex.Message}");
                }
            }
        }
    }
}
