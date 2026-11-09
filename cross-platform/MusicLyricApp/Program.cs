using Avalonia;
using System;
using System.IO;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;
using NLog;
using TKCode.Arc;
using TKCode.Arc.Fonts;

namespace MusicLyricApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var settings = new StorageService().ReadAppConfig();
            LoggingService.Initialize(Path.Combine(AppContext.BaseDirectory, "NLog.config"),
                settings.Config.EnableFileLogging);
            LogManager.GetCurrentClassLogger().Info("Application starting...");
            
            BuildAvaloniaApp(settings.Config.ThemeMode).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex, "App will be crash, message: {ErrorMsg}", ex.Message);
        }
        finally
        {
            LogManager.Shutdown(); // flush and close
        }
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(ThemeModeEnum.FOLLOW_SYSTEM);

    public static AppBuilder BuildAvaloniaApp(ThemeModeEnum themeMode)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .WithArcFonts()
            .UseArc(arc => arc
                .UseArcControls()
                .UseArcBlocks()
                .WithTheme(t => t.Appearance(ThemeService.ToAppearance(themeMode)).Accent(ThemeService.Accent)))
            .LogToTrace();
}