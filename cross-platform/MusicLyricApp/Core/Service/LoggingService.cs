using NLog;
using NLog.Config;

namespace MusicLyricApp.Core.Service;

public static class LoggingService
{
    private const string FileLoggingVariable = "fileLoggingEnabled";

    public static void Initialize(string configPath, bool enableFileLogging, LogFactory? factory = null)
    {
        factory ??= LogManager.LogFactory;
        factory.KeepVariablesOnReload = true;
        var configuration = new XmlLoggingConfiguration(configPath, factory);
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
}
