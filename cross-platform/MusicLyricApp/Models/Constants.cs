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

    public static string GetDefaultSearchCacheFolderPath()
    {
        var configDir = Path.GetDirectoryName(GetConfigFilePath()) ?? Environment.CurrentDirectory;
        return Path.Combine(configDir, "cache");
    }

    public static WindowIcon GetIcon(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MusicLyricApp.Resources." + name + ".ico";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        return new WindowIcon(stream);
    }

    public const int BatchQuerySize = 300;

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
            /// 时间戳设置
            /// </summary>
            TIME_STAMP_SETTING,

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
                case TypeEnum.TIME_STAMP_SETTING:
                    list.Add("Help.Timestamp.Intro");
                    list.Add("Help.Timestamp.Hours");
                    list.Add("Help.Timestamp.Minutes");
                    list.Add("Help.Timestamp.Seconds");
                    list.Add("Help.Timestamp.MillisecondsOne");
                    list.Add("Help.Timestamp.MillisecondsTwo");
                    list.Add("Help.Timestamp.MillisecondsThree");
                    list.Add("Help.Timestamp.Rounding");
                    break;
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
