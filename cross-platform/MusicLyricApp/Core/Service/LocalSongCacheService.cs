using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using NLog;

namespace MusicLyricApp.Core.Service;

public static class LocalSongCacheService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void EnsureConfigDefaults(SettingBean settingBean)
    {
        if (string.IsNullOrWhiteSpace(settingBean.Config.SearchCacheFolderPath))
        {
            settingBean.Config.SearchCacheFolderPath = Constants.GetDefaultSearchCacheFolderPath();
        }

        if (settingBean.Config.SearchCacheMaxSizeMb <= 0)
        {
            settingBean.Config.SearchCacheMaxSizeMb = 128;
        }
    }

    public static bool TryLoadSaveVo(SettingBean settingBean, SearchSourceEnum source, string songId, out SaveVo saveVo)
    {
        saveVo = null!;
        try
        {
            var path = FindCacheFile(settingBean, source, songId);
            if (path == null)
            {
                return false;
            }

            var text = File.ReadAllText(path);
            var entry = text.ToEntity<LocalSongCacheEntry>();
            if (entry?.SongVo == null || entry.LyricVo == null ||
                entry.IsVerbatim != (settingBean.Config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE))
            {
                return false;
            }

            saveVo = new SaveVo(entry.Index, entry.SongVo, entry.LyricVo);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Load local song cache failed: {Source}/{SongId}", source, songId);
            return false;
        }
    }

    /// <summary>A song read back from the local cache.</summary>
    public sealed record CachedSong(SearchSourceEnum Source, string SongId, SaveVo SaveVo, string SongLink, DateTime UpdatedAt);

    /// <summary>
    /// The most recently cached songs, newest first, at most <paramref name="limit"/> of them. Entries are read by
    /// content rather than file name, since older names depend on the UI language at the time they were written;
    /// unreadable entries and entries cached for the other verbatim-lyric mode are left out.
    /// </summary>
    public static List<CachedSong> LoadRecent(SettingBean settingBean, int limit)
    {
        var songs = new Dictionary<(SearchSourceEnum, string), CachedSong>();
        try
        {
            EnsureConfigDefaults(settingBean);
            var folder = settingBean.Config.SearchCacheFolderPath;
            if (!Directory.Exists(folder))
            {
                return [];
            }

            var isVerbatim = settingBean.Config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE;
            foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
            {
                try
                {
                    var entry = File.ReadAllText(file).ToEntity<LocalSongCacheEntry>();
                    if (entry?.SongVo == null || entry.LyricVo == null || string.IsNullOrWhiteSpace(entry.SongId) ||
                        entry.IsVerbatim != isVerbatim)
                    {
                        continue;
                    }

                    var song = new CachedSong(entry.Source, entry.SongId, new SaveVo(entry.Index, entry.SongVo, entry.LyricVo),
                        entry.SongLink ?? "", entry.UpdatedAt);
                    var key = (entry.Source, entry.SongId);
                    if (!songs.TryGetValue(key, out var known) || known.UpdatedAt < song.UpdatedAt)
                    {
                        songs[key] = song;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Skip unreadable local song cache: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Load local song cache folder failed");
        }

        return songs.Values
            .OrderByDescending(s => s.UpdatedAt)
            .Take(Math.Max(0, limit))
            .ToList();
    }

    public static bool TryLoadSongLink(SettingBean settingBean, SearchSourceEnum source, string songId, out string link)
    {
        link = string.Empty;
        try
        {
            var path = FindCacheFile(settingBean, source, songId);
            if (path == null)
            {
                return false;
            }

            var text = File.ReadAllText(path);
            var entry = text.ToEntity<LocalSongCacheEntry>();
            if (entry == null || string.IsNullOrWhiteSpace(entry.SongLink))
            {
                return false;
            }

            link = entry.SongLink;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Load local song link cache failed: {Source}/{SongId}", source, songId);
            return false;
        }
    }

    public static void SaveCache(SettingBean settingBean, SearchSourceEnum source, string songId, SaveVo saveVo, string? songLink = null)
    {
        try
        {
            var folder = EnsureCacheFolder(settingBean);
            var path = GetCacheFilePath(settingBean, source, songId);

            // Start from whatever is cached under any name, so a stored song link is kept.
            var existingPath = FindCacheFile(settingBean, source, songId);
            var entry = existingPath == null
                ? new LocalSongCacheEntry()
                : File.ReadAllText(existingPath).ToEntity<LocalSongCacheEntry>() ?? new LocalSongCacheEntry();

            entry.Source = source;
            entry.SongId = songId;
            entry.Index = saveVo.Index;
            entry.SongVo = saveVo.SongVo;
            entry.LyricVo = saveVo.LyricVo;
            entry.IsVerbatim = settingBean.Config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE;
            if (!string.IsNullOrWhiteSpace(songLink))
            {
                entry.SongLink = songLink;
            }
            entry.UpdatedAt = DateTime.Now;

            File.WriteAllText(path, entry.ToJson(), System.Text.Encoding.UTF8);
            DeleteLegacyFiles(settingBean, source, songId);

            TrimIfNeeded(folder, settingBean.Config.SearchCacheMaxSizeMb);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Save local song cache failed: {Source}/{SongId}", source, songId);
        }
    }

    public static void UpdateSongLink(SettingBean settingBean, SearchSourceEnum source, string songId, string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        try
        {
            var path = FindCacheFile(settingBean, source, songId);
            if (path == null)
            {
                return;
            }

            var entry = File.ReadAllText(path).ToEntity<LocalSongCacheEntry>();
            if (entry == null)
            {
                return;
            }

            entry.SongLink = link;
            entry.UpdatedAt = DateTime.Now;
            File.WriteAllText(path, entry.ToJson(), System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Update local song link cache failed: {Source}/{SongId}", source, songId);
        }
    }

    private static string EnsureCacheFolder(SettingBean settingBean)
    {
        EnsureConfigDefaults(settingBean);
        var folder = settingBean.Config.SearchCacheFolderPath;
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Removes a song from the cache, whichever name it was stored under. True when a file was deleted.</summary>
    public static bool Delete(SettingBean settingBean, SearchSourceEnum source, string songId)
    {
        var deleted = false;
        foreach (var path in CandidatePaths(settingBean, source, songId))
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Delete local song cache failed: {Path}", path);
            }
        }

        return deleted;
    }

    /// <summary>
    /// Where a song is cached. The name uses the source's enum name so it does not change with the UI language.
    /// </summary>
    private static string GetCacheFilePath(SettingBean settingBean, SearchSourceEnum source, string songId)
    {
        EnsureConfigDefaults(settingBean);
        return Path.Combine(settingBean.Config.SearchCacheFolderPath, $"{source}_{songId}.json");
    }

    /// <summary>
    /// Versions before 8.0 named files after the source's display name in the UI language of the moment
    /// (for example "网易云_1.json" or "NetEase Music_1.json"); those names are still read and cleaned up.
    /// </summary>
    private static readonly Dictionary<SearchSourceEnum, string[]> LegacySourceNames = new()
    {
        [SearchSourceEnum.NET_EASE_MUSIC] = ["网易云", "NetEase Music"],
        [SearchSourceEnum.QQ_MUSIC] = ["QQ音乐", "QQ Music"]
    };

    private static IEnumerable<string> CandidatePaths(SettingBean settingBean, SearchSourceEnum source, string songId)
    {
        yield return GetCacheFilePath(settingBean, source, songId);
        foreach (var path in LegacyPaths(settingBean, source, songId))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> LegacyPaths(SettingBean settingBean, SearchSourceEnum source, string songId) =>
        LegacySourceNames.TryGetValue(source, out var names)
            ? names.Select(name => Path.Combine(settingBean.Config.SearchCacheFolderPath, $"{name}_{songId}.json"))
            : [];

    private static string? FindCacheFile(SettingBean settingBean, SearchSourceEnum source, string songId) =>
        CandidatePaths(settingBean, source, songId).FirstOrDefault(File.Exists);

    private static void DeleteLegacyFiles(SettingBean settingBean, SearchSourceEnum source, string songId)
    {
        foreach (var path in LegacyPaths(settingBean, source, songId))
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Delete legacy song cache failed: {Path}", path);
            }
        }
    }

    private static void TrimIfNeeded(string folder, int maxSizeMb)
    {
        var limitBytes = Math.Max(1, maxSizeMb) * 1024L * 1024L;
        var files = new DirectoryInfo(folder).GetFiles("*.json");
        long total = files.Sum(f => f.Length);
        if (total <= limitBytes)
        {
            return;
        }

        foreach (var file in files.OrderBy(f => f.LastWriteTimeUtc))
        {
            try
            {
                total -= file.Length;
                file.Delete();
                if (total <= limitBytes)
                {
                    break;
                }
            }
            catch
            {
                // ignore single file cleanup failure
            }
        }
    }

    private class LocalSongCacheEntry
    {
        public SearchSourceEnum Source { get; set; }
        public string SongId { get; set; } = "";
        public int Index { get; set; }
        public SongVo? SongVo { get; set; }
        public LyricVo? LyricVo { get; set; }
        // Older cache entries have no mode information and must be fetched again.
        public bool? IsVerbatim { get; set; }
        public string SongLink { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
