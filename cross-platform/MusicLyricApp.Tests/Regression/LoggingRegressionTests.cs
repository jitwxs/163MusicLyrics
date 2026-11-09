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

    [Fact]
    public void RepeatedSwitchesDrainAcceptedEventsAndNeverChangeFileWhileDisabled()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        LoggingService.Initialize(CreateConfiguration(ws), true, factory);
        var logger = factory.GetLogger("repeated-switch");
        for (var cycle = 0; cycle < 3; cycle++)
        {
            logger.Info("enabled-cycle-{0}", cycle);
            LoggingService.ConfigureFileLogging(false, factory);
            var path = Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log"));
            var before = File.ReadAllBytes(path);
            var lastWrite = File.GetLastWriteTimeUtc(path);
            Assert.Contains($"enabled-cycle-{cycle}", File.ReadAllText(path));

            LoggingService.ConfigureFileLogging(false, factory);
            logger.Fatal("disabled-cycle-{0}", cycle);
            factory.Flush();
            Assert.Equal(before, File.ReadAllBytes(path));
            Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(path));

            LoggingService.ConfigureFileLogging(true, factory);
            LoggingService.ConfigureFileLogging(true, factory);
        }

        logger.Info("final-resume");
        factory.Flush();
        var content = File.ReadAllText(Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log")));
        Assert.Contains("final-resume", content);
        Assert.DoesNotContain("disabled-cycle", content);
        for (var cycle = 0; cycle < 3; cycle++)
            Assert.Equal(1, content.Split($"enabled-cycle-{cycle}").Length - 1);
    }

    [Fact]
    public void RestartWithPersistedDisabledSettingLeavesPreviousLogUntouched()
    {
        using var ws = new TestWorkspace();
        var configPath = CreateConfiguration(ws);
        using (var firstFactory = new LogFactory { ThrowExceptions = true })
        {
            LoggingService.Initialize(configPath, true, firstFactory);
            firstFactory.GetLogger("first-run").Info("previous session");
            firstFactory.Flush();
            LoggingService.ConfigureFileLogging(false, firstFactory);
            ws.Settings.Config.EnableFileLogging = false;
            ws.Storage().SaveConfig(ws.Settings);
        }

        var path = Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log"));
        var before = File.ReadAllBytes(path);
        var lastWrite = File.GetLastWriteTimeUtc(path);
        var settings = ws.Storage().ReadAppConfig();
        Assert.False(settings.Config.EnableFileLogging);
        using (var restartedFactory = new LogFactory { ThrowExceptions = true })
        {
            LoggingService.Initialize(configPath, settings.Config.EnableFileLogging, restartedFactory);
            restartedFactory.GetLogger("restart").Error("disabled startup");
            restartedFactory.Flush();
        }

        Assert.Equal(new[] { path }, Directory.GetFiles(ws.Root, "MusicLyricApp-*.log"));
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task EnabledRuntimeSettingSurvivesReloadAndReloadStillAppliesLayoutChanges()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        var configPath = CreateConfiguration(ws);
        LoggingService.Initialize(configPath, false, factory);
        var logger = factory.GetLogger("enabled-reload");
        logger.Info("disabled-before-enable");
        factory.Flush();
        Assert.Empty(Directory.GetFiles(ws.Root, "*.log"));
        LoggingService.ConfigureFileLogging(true, factory);
        logger.Info("enabled-before-reload");
        factory.Flush();

        var original = factory.Configuration;
        var reloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ConfigurationChanged += (_, _) =>
        {
            if (factory.Configuration != original)
                reloaded.TrySetResult();
        };
        var document = XDocument.Load(configPath);
        var fileTarget = Assert.Single(document.Descendants(), element =>
            element.Attribute("fileName") != null);
        fileTarget.SetAttributeValue("layout", "reloaded-layout | ${message}");
        document.Save(configPath);
        await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
        logger.Info("enabled-after-reload");
        factory.Flush();

        var content = File.ReadAllText(Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log")));
        Assert.Contains("enabled-before-reload", content);
        Assert.Contains("reloaded-layout | enabled-after-reload", content);
        Assert.DoesNotContain("disabled-before-enable", content);
    }

    [Fact]
    public void DefaultInfoFileThresholdDoesNotFilterOtherTargetsAcrossSwitches()
    {
        using var ws = new TestWorkspace();
        using var factory = new LogFactory { ThrowExceptions = true };
        LoggingService.Initialize(CreateConfiguration(ws), true, factory);
        var memory = new MemoryTarget("all-levels") { Layout = "${message}" };
        factory.Configuration.AddRuleForAllLevels(memory);
        factory.ReconfigExistingLoggers();
        var logger = factory.GetLogger("threshold");
        logger.Trace("trace-before");
        logger.Debug("debug-before");
        logger.Info("info-before");
        LoggingService.ConfigureFileLogging(false, factory);
        logger.Error("error-disabled");
        LoggingService.ConfigureFileLogging(true, factory);
        logger.Debug("debug-after");
        logger.Warn("warn-after");
        factory.Flush();

        Assert.Equal(new[] { "trace-before", "debug-before", "info-before", "error-disabled", "debug-after", "warn-after" }, memory.Logs);
        var content = File.ReadAllText(Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log")));
        Assert.Contains("info-before", content);
        Assert.Contains("warn-after", content);
        Assert.DoesNotContain("trace-before", content);
        Assert.DoesNotContain("debug-before", content);
        Assert.DoesNotContain("debug-after", content);
        Assert.DoesNotContain("error-disabled", content);
    }

    [Fact]
    public void SwitchingBeforeInitializationIsNoOpAndDoesNotOverrideLaterInitialization()
    {
        using var ws = new TestWorkspace();
        // Suppress NLog's automatic discovery of the shipped config to exercise the no-config branch.
        using var factory = new LogFactory { ThrowExceptions = true };
#pragma warning disable CS0618 // The replacement loader still falls back to auto-discovery for missing files.
        factory.SetCandidateConfigFilePaths(Array.Empty<string>());
#pragma warning restore CS0618
        var logger = factory.GetLogger("before-init");
        Assert.Null(factory.Configuration);
        LoggingService.ConfigureFileLogging(true, factory);
        LoggingService.ConfigureFileLogging(false, factory);
        Assert.Null(factory.Configuration);
        Assert.Empty(Directory.GetFiles(ws.Root));

        LoggingService.Initialize(CreateConfiguration(ws), true, factory);
        logger.Info("explicit-startup-setting-wins");
        factory.Flush();
        Assert.Contains("explicit-startup-setting-wins",
            File.ReadAllText(Assert.Single(Directory.GetFiles(ws.Root, "MusicLyricApp-*.log"))));
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
