using System;
using System.IO;
using System.Linq;
using NLog;
using NLog.Config;

namespace MusicLyricApp.Core.Service;

/// <summary>How much the log folder holds; "old" means every log file but today's, i.e. what can be deleted.</summary>
public readonly record struct LogUsage(int Files, long Bytes, int OldFiles, long OldBytes)
{
    public static readonly LogUsage Empty = default;
}

public static class LoggingService
{
    private const string FileLoggingVariable = "fileLoggingEnabled";
    private const string LogDirectoryVariable = "logDir";

    /// <summary>Daily files are <c>MusicLyricApp-yyyy-MM-dd.log</c>, matching the file target in NLog.config.</summary>
    public const string LogFilePattern = "MusicLyricApp-*.log";

    public static string TodayLogFileName(DateTime today) => $"MusicLyricApp-{today:yyyy-MM-dd}.log";

    public static void Initialize(string configPath, string logDirectory, bool enableFileLogging,
        LogFactory? factory = null)
    {
        factory ??= LogManager.LogFactory;
        factory.KeepVariablesOnReload = true;
        var configuration = new XmlLoggingConfiguration(configPath, factory);
        configuration.Variables[LogDirectoryVariable] = logDirectory;
        configuration.Variables[FileLoggingVariable] = enableFileLogging ? "true" : "false";
        factory.Configuration = configuration;
    }

    public static void ConfigureFileLogging(bool enabled, LogFactory? factory = null)
    {
        factory ??= LogManager.LogFactory;
        var configuration = factory.Configuration;
        if (configuration == null)
        {
            return;
        }

        // Drain events accepted before the switch; disabled logging never deletes existing files.
        factory.Flush();
        factory.KeepVariablesOnReload = true;
        configuration.Variables[FileLoggingVariable] = enabled ? "true" : "false";
        factory.ReconfigExistingLoggers();
    }

    /// <summary>Counts the log files in <paramref name="logDirectory"/>; a missing folder holds none.</summary>
    public static LogUsage GetLogUsage(string logDirectory, DateTime today)
    {
        if (!Directory.Exists(logDirectory))
        {
            return LogUsage.Empty;
        }

        var usage = LogUsage.Empty;
        var keep = TodayLogFileName(today);
        foreach (var file in new DirectoryInfo(logDirectory).EnumerateFiles(LogFilePattern))
        {
            long length;
            try
            {
                length = file.Length;
            }
            catch (IOException)
            {
                continue; // Deleted while enumerating.
            }

            var old = !string.Equals(file.Name, keep, StringComparison.OrdinalIgnoreCase);
            usage = new LogUsage(usage.Files + 1, usage.Bytes + length,
                usage.OldFiles + (old ? 1 : 0), usage.OldBytes + (old ? length : 0));
        }

        return usage;
    }

    /// <summary>
    /// Deletes the log files in <paramref name="logDirectory"/> except today's, which the logger may hold open
    /// (Windows locks it; on macOS and Linux deleting it would send the rest of today's log to an unlinked file).
    /// Files that cannot be deleted are skipped. Returns how many were deleted and how many bytes that freed.
    /// </summary>
    public static (int Files, long Bytes) DeleteOldLogs(string logDirectory, DateTime today)
    {
        if (!Directory.Exists(logDirectory))
        {
            return (0, 0);
        }

        var keep = TodayLogFileName(today);
        var deleted = 0;
        long freed = 0;
        foreach (var file in new DirectoryInfo(logDirectory).EnumerateFiles(LogFilePattern)
                     .Where(f => !string.Equals(f.Name, keep, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var length = file.Length;
                file.Delete();
                deleted++;
                freed += length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // In use or protected: leave it.
            }
        }

        return (deleted, freed);
    }

    /// <summary>Byte count as people read it: 512 B, 3.4 KB, 12.5 MB, 1.2 GB (1 KB = 1024 B).</summary>
    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} B"
            : value.ToString(value >= 100 ? "0" : "0.#", System.Globalization.CultureInfo.InvariantCulture) + " " + units[unit];
    }
}
