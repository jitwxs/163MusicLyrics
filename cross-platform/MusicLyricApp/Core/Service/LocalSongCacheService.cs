using System;
using System.IO;
using System.Linq;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Core.Localization;
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
            var path = GetCacheFilePath(settingBean, source, songId);
            if (!File.Exists(path))
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

    public static bool TryLoadSongLink(SettingBean settingBean, SearchSourceEnum source, string songId, out string link)
    {
        link = string.Empty;
        try
        {
            var path = GetCacheFilePath(settingBean, source, songId);
            if (!File.Exists(path))
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

            LocalSongCacheEntry entry;
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path).ToEntity<LocalSongCacheEntry>();
                entry = existing ?? new LocalSongCacheEntry();
            }
            else
            {
                entry = new LocalSongCacheEntry();
            }

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
            var path = GetCacheFilePath(settingBean, source, songId);
            if (!File.Exists(path))
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

    private static string GetCacheFilePath(SettingBean settingBean, SearchSourceEnum source, string songId)
    {
        EnsureConfigDefaults(settingBean);
        var sourceName = source switch
        {
            SearchSourceEnum.NET_EASE_MUSIC => LocalizationService.Get("Status.Search.SourceNetEase"),
            SearchSourceEnum.QQ_MUSIC => LocalizationService.Get("Status.Search.SourceQQ"),
            _ => source.ToString()
        };
        var filename = $"{sourceName}_{songId}.json";
        return Path.Combine(settingBean.Config.SearchCacheFolderPath, filename);
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
