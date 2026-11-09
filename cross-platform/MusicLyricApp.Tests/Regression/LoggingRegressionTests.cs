using System.Xml.Linq;
using MusicLyricApp.Core.Service;
using MusicLyricApp.ViewModels;
using NLog;

using NLog.Targets;

namespace MusicLyricAppTest.Regression;

public class LoggingRegressionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PersistedSettingControlsStartupFileCreation(bool enabled)
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.EnableFileLogging = enabled;
        ws.Storage().SaveConfig(ws.Settings);
        var settings = ws.Storage().ReadAppConfig();
        Assert.Equal(enabled, settings.Config.EnableFileLogging);

        using var factory = new LogFactory { ThrowExceptions = true };
        LoggingService.Initialize(CreateConfiguration(ws), settings.Config.EnableFileLogging, factory);
        factory.GetLogger("startup").Info("Application starting...");
        factory.Flush();

        var files = Directory.GetFiles(ws.Root, "MusicLyricApp-*.log");
        if (enabled)
        {
            Assert.Contains("Application starting...", File.ReadAllText(Assert.Single(files)));
        }
        else
        {
            Assert.Empty(files);
        }
    }

    [Fact]
    public void DisabledFileLoggingPreservesOtherTargets()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        LoggingService.Initialize(CreateConfiguration(ws), false, factory);
        var memory = new MemoryTarget("test-memory") { Layout = "${message}" };
        factory.Configuration.AddRuleForAllLevels(memory);
        factory.ReconfigExistingLoggers();

        factory.GetLogger("test").Error("still available for diagnostics");
        factory.Flush();

        Assert.Equal("still available for diagnostics", Assert.Single(memory.Logs));
        Assert.Empty(Directory.GetFiles(ws.Root, "*.log"));
    }

    [Fact]
    public void RuntimeSwitchStopsWritesPreservesExistingFileAndCanResume()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        LoggingService.Initialize(CreateConfiguration(ws), true, factory);
        var logger = factory.GetLogger("test");
        logger.Info("before disabling");
        // ConfigureFileLogging drains the async queue before disabling file output.
        LoggingService.ConfigureFileLogging(false, factory);
        var path = Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log"));
        var before = File.ReadAllText(path);
        Assert.Contains("before disabling", before);

        logger.Error("must not be written");
        factory.Flush();
        Assert.Equal(before, File.ReadAllText(path));

        LoggingService.ConfigureFileLogging(true, factory);
        logger.Info("after enabling");
        factory.Flush();
        var after = File.ReadAllText(path);
        Assert.Contains("before disabling", after);
        Assert.Contains("after enabling", after);
        Assert.DoesNotContain("must not be written", after);
    }

    [Fact]
    public async Task DisabledSettingSurvivesConfigurationReload()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        var configPath = CreateConfiguration(ws);
        LoggingService.Initialize(configPath, true, factory);
        LoggingService.ConfigureFileLogging(false, factory);

        var originalConfiguration = factory.Configuration;
        var reloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ConfigurationChanged += (_, _) =>
        {
            if (factory.Configuration != originalConfiguration)
            {
                reloaded.TrySetResult();
            }
        };
        File.AppendAllText(configPath, Environment.NewLine);
        await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
        factory.GetLogger("test").Info("must remain disabled after reload");
        factory.Flush();

        Assert.Empty(Directory.GetFiles(ws.Root, "*.log"));
    }

    [Fact]
    public void SettingsViewModelBindsAndUpdatesPersistedPreference()
    {
        using var ws = new TestWorkspace();
        ws.Settings.Config.EnableFileLogging = false;
        var vm = new SettingParamViewModel();
        try
        {
            vm.Bind(ws.Settings);
            Assert.False(vm.EnableFileLogging);
            vm.EnableFileLogging = true;
            Assert.True(ws.Settings.Config.EnableFileLogging);
            vm.EnableFileLogging = false;
            Assert.False(ws.Settings.Config.EnableFileLogging);
            ws.Storage().SaveConfig(ws.Settings);
            Assert.False(ws.Storage().ReadAppConfig().Config.EnableFileLogging);
        }
        finally
        {
            LoggingService.ConfigureFileLogging(true);
        }
    }

    private static string CreateConfiguration(TestWorkspace ws)
    {
        // Exercise the shipped rules and async file target, but never write into the app directory.
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "NLog.config"));
        foreach (var attribute in document.Descendants().Attributes())
        {
            attribute.Value = attribute.Value.Replace("${basedir}", ws.Root);
        }
        var path = Path.Combine(ws.Root, "NLog.config");
        document.Save(path);
        return path;
    }
}
