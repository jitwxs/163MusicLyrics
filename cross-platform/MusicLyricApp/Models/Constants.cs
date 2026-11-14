using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Avalonia.Controls;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Models;

public static class Constants
{
    private const string AppFolderName = "MusicLyricApp";
    private const string ConfigFileName = "MusicLyricAppSetting.json";

    /// <summary>
    /// 获取用户配置文件的完整路径，确保目录存在。
    /// </summary>
    public static string GetConfigFilePath()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // macOS/Linux 会自动转换为 ~/Library/Application Support 或 ~/.config
        string configDir = Path.Combine(basePath, AppFolderName);

        // 确保目录存在
        Directory.CreateDirectory(configDir);

        return Path.Combine(configDir, ConfigFileName);
    }

    /// <summary>
    /// Where file logs go, following each platform's convention instead of the program folder, which may be
    /// read-only (Program Files, /opt) or a signed .app bundle:
    /// Windows <c>%LOCALAPPDATA%\MusicLyricApp\logs</c> (logs do not roam with the profile),
    /// macOS <c>~/Library/Logs/MusicLyricApp</c> (shown by Console.app),
    /// Linux <c>$XDG_STATE_HOME/MusicLyricApp/logs</c>, falling back to <c>~/.local/state</c>.
    /// The folder is created by the logger when it first writes.
    /// </summary>
    public static string GetLogFolderPath() => ResolveLogFolderPath(
        OperatingSystem.IsWindows() ? LogPlatform.Windows :
        OperatingSystem.IsMacOS() ? LogPlatform.MacOS : LogPlatform.Linux,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetEnvironmentVariable("XDG_STATE_HOME"));

    public enum LogPlatform { Windows, MacOS, Linux }

    public static string ResolveLogFolderPath(LogPlatform platform, string home, string localAppData,
        string? xdgStateHome)
    {
        switch (platform)
        {
            case LogPlatform.Windows:
                return Path.Combine(localAppData, AppFolderName, "logs");
            case LogPlatform.MacOS:
                return Path.Combine(home, "Library", "Logs", AppFolderName);
            default:
                // The XDG spec ignores relative values.
                var stateHome = !string.IsNullOrWhiteSpace(xdgStateHome) && Path.IsPathRooted(xdgStateHome)
                    ? xdgStateHome
                    : Path.Combine(home, ".local", "state");
                return Path.Combine(stateHome, AppFolderName, "logs");
        }
    }

    public static string GetDefaultSearchCacheFolderPath()
    {
        var configDir = Path.GetDirectoryName(GetConfigFilePath()) ?? Environment.CurrentDirectory;
        return Path.Combine(configDir, "cache");
    }

    public const int BatchQuerySize = 300;

    /// <summary>Default and upper bound for how many cached songs the download manager restores at startup.</summary>
    public const int DefaultStartupCacheLoadLimit = 500;
    public const int MaxStartupCacheLoadLimit = 5000;

    public const int SleepMsBetweenBatchQuery = 500;

    public static class HelpTips
    {
        public enum TypeEnum
        {
            /// <summary>
            /// 默认实例
            /// </summary>
            DEFAULT,

            /// <summary>
            /// 输出设置
            /// </summary>
            OUTPUT_SETTING
        }

        public static string GetContent(TypeEnum typeEnum)
        {
            var list = new List<string> { "Help.Prefix" };

            switch (typeEnum)
            {
                case TypeEnum.OUTPUT_SETTING:
                    list.Add("Help.Output.Intro");
                    list.Add("Help.Output.Id");
                    list.Add("Help.Output.Index");
                    list.Add("Help.Output.Name");
                    list.Add("Help.Output.Singer");
                    list.Add("Help.Output.Album");
                    list.Add("Help.Output.Separator");
                    list.Add("Help.Output.Functions");
                    list.Add("Help.Output.FillFunction");
                    list.Add("Help.Output.FillDescription");
                    break;
                case TypeEnum.DEFAULT:
                default:
                    break;
            }

            return string.Join(Environment.NewLine + Environment.NewLine, list.Select(LocalizationService.Get));
        }
    }
}
