using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia;
using NLog;
using MusicLyricApp.Core.Localization;

namespace MusicLyricApp.Core;

public static class DialogHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static async Task ShowMessage(Exception ex)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Logger.Error(ex, "DialogHelper ShowMessage message: {ErrorMsg}, stackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            var box = MessageBoxManager.GetMessageBoxStandard(LocalizationService.Get("Dialog.Notice.Title"), ex.Message);
            box.ShowWindowAsync();
        });
    }
    
    public static async Task ShowMessage(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Logger.Info("DialogHelper ShowMessage message: {Message}", message);
            var box = MessageBoxManager.GetMessageBoxStandard(LocalizationService.Get("Dialog.Notice.Title"), message);
            box.ShowWindowAsync();
        });
    }
}
