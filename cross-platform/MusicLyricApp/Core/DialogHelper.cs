using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using NLog;
using MusicLyricApp.Core.Localization;
using TKCode.Arc;

namespace MusicLyricApp.Core;

/// <summary>
/// App-wide dialogs on top of Arc's <see cref="Dialog"/>. Every dialog is anchored to the main window;
/// when there is none (headless tests, early start-up) the message is only logged.
/// </summary>
public static class DialogHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Overrides the owner lookup; tests set this to a window of their own.</summary>
    public static Func<Visual?>? OwnerResolver { get; set; }

    private static Visual? Owner => OwnerResolver != null
        ? OwnerResolver()
        : (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static Task ShowMessage(Exception ex)
    {
        Logger.Error(ex, "DialogHelper ShowMessage message: {ErrorMsg}, stackTrace: {StackTrace}", ex.Message, ex.StackTrace);
        return ShowNotice(ex.Message);
    }

    public static Task ShowMessage(string message)
    {
        Logger.Info("DialogHelper ShowMessage message: {Message}", message);
        return ShowNotice(message);
    }

    /// <summary>Shows a notice without blocking the caller, matching the previous fire-and-forget behaviour.</summary>
    private static async Task ShowNotice(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Owner is not { } owner)
            {
                return;
            }

            _ = Dialog.ShowAsync(owner, LocalizationService.Get("Dialog.Notice.Title"), BuildBody(message), null,
                new DialogAction(LocalizationService.Get("Dialog.Action.Close"), null, ButtonVariant.Primary));
        });
    }

    /// <summary>Asks a yes/no question; returns true only when the confirm button is pressed.</summary>
    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText, bool isDanger = false)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Owner is not { } owner)
            {
                return false;
            }

            return await Dialog.ConfirmAsync(owner, title, message, confirmText,
                LocalizationService.Get("Dialog.Action.Cancel"), isDanger);
        });
    }

    /// <summary>Shows release notes; returns true when the user chooses to open the download page.</summary>
    public static async Task<bool> ShowReleaseNotesAsync(string content)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Owner is not { } owner)
            {
                return false;
            }

            var result = await Dialog.ShowAsync(owner, LocalizationService.Get("Dialog.ReleaseNotes.Title"),
                BuildBody(content), null,
                new DialogAction(LocalizationService.Get("Dialog.ReleaseNotes.Later"), false, ButtonVariant.Secondary),
                new DialogAction(LocalizationService.Get("Dialog.ReleaseNotes.Open"), true, ButtonVariant.Primary));
            return result is true;
        });
    }

    /// <summary>Long, multi-line text (release notes, shortcut lists) scrolls inside the dialog.</summary>
    private static Control BuildBody(string text) => new ScrollViewer
    {
        MaxHeight = 360,
        Content = new SelectableTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        }
    };
}
