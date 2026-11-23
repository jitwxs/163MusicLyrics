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
        var (fileTitle, fileArtist) = ParseFileName(path);
        string? title = null, artist = null, album = null;
        long duration = 0;

        try
        {
            // Formats with native TagLibSharp support (MP3/FLAC/M4A/OGG/WAV/WMA/APE/DSF ...).
            using var file = TagLib.File.Create(path);
            title = file.Tag.Title?.Trim();
            artist = file.Tag.Performers?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                ? string.Join(" / ", file.Tag.Performers.Where(x => !string.IsNullOrWhiteSpace(x))) : null;
            album = file.Tag.Album?.Trim();
            duration = (long)file.Properties.Duration.TotalMilliseconds;
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or ArgumentException)
        {
            // TagLibSharp cannot parse this container (e.g. DFF / DSDIFF). Fall back to an
            // embedded ID3v2 tag if present, otherwise rely on the filename below.
            ReadEmbeddedId3v2(path, ref title, ref artist, ref album);
        }

        return new LocalAudioFile(path,
            string.IsNullOrWhiteSpace(title) ? fileTitle : title,
            string.IsNullOrWhiteSpace(artist) ? fileArtist : artist,
            album ?? "", duration);
    }

    /// <summary>
    /// Best-effort scan for an embedded ID3v2 tag (used for DFF/DSDIFF, which TagLibSharp cannot parse).
    /// Pure byte-level parsing avoids depending on TagLib's container-specific APIs.
    /// </summary>
    private static void ReadEmbeddedId3v2(string path, ref string? title, ref string? artist, ref string? album)
    {
        try
        {
            const int window = 256 * 1024;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[window];
            var read = fs.Read(buffer, 0, buffer.Length);

            for (var i = 0; i + 10 <= read; i++)
            {
                if (buffer[i] != (byte)'I' || buffer[i + 1] != (byte)'D' || buffer[i + 2] != (byte)'3') continue;
                var major = buffer[i + 3];
                if (major is not (2 or 3 or 4)) continue;
                // Sync-safe tag size (7 bits per byte).
                var size = ((buffer[i + 6] & 0x7F) << 21) | ((buffer[i + 7] & 0x7F) << 14)
                           | ((buffer[i + 8] & 0x7F) << 7) | (buffer[i + 9] & 0x7F);
                var pos = i + 10;
                var end = Math.Min(read, pos + size);
                if (major == 2) { pos += 6; } // v2.2 header has no flags byte after size handling here

                while (pos + 10 <= end)
                {
                    var fid = System.Text.Encoding.ASCII.GetString(buffer, pos, 4);
                    if (fid[0] == 0) break; // padding
                    var frameSize = major == 2
                        ? (buffer[pos + 4] << 16) | (buffer[pos + 5] << 8) | buffer[pos + 6]
                        : (buffer[pos + 4] << 24) | (buffer[pos + 5] << 16) | (buffer[pos + 6] << 8) | buffer[pos + 7];
                    var dataStart = pos + 10;
                    if (frameSize <= 0 || dataStart + frameSize > end) break;
                    var enc = buffer[dataStart];
                    var text = DecodeId3Text(buffer, dataStart + 1, frameSize - 1, enc);
                    switch (fid)
                    {
                        case "TIT2": case "TT2": title = string.IsNullOrWhiteSpace(title) ? text : title; break;
                        case "TPE1": case "TP1": artist = string.IsNullOrWhiteSpace(artist) ? text : artist; break;
                        case "TALB": case "TAL": album = string.IsNullOrWhiteSpace(album) ? text : album; break;
                    }
                    pos = dataStart + frameSize;
                }
                break;
            }
        }
        catch
        {
            // Ignore: filename fallback still applies.
        }
    }

    private static string DecodeId3Text(byte[] buffer, int offset, int count, byte encoding)
    {
        if (count <= 0) return "";
        System.Text.Encoding enc = encoding switch
        {
            0 => System.Text.Encoding.Latin1,
            1 => System.Text.Encoding.Unicode, // UTF-16 with BOM
            2 => System.Text.Encoding.BigEndianUnicode,
            _ => System.Text.Encoding.UTF8,     // 3 = UTF-8
        };
        var text = enc.GetString(buffer, offset, count).Trim('\0', ' ', '\r', '\n');
        // UTF-16 frames start with a BOM; strip it.
        if (text.Length > 0 && (text[0] == '\uFEFF' || text[0] == '\uFFFE')) text = text[1..];
        return text.Trim();
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
        { ".mp3", ".flac", ".m4a", ".ogg", ".wav", ".wma", ".ape", ".dsf", ".dff" };

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
