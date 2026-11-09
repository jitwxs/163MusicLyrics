using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TKCode.Arc;

namespace MusicLyricApp.Views;

/// <summary>
/// Moves an <see cref="ArcWindow"/> by its title bar on platforms where Arc's own drag does nothing.
/// </summary>
/// <remarks>
/// With TKCode.Arc 1.0.0 on Avalonia 12.1.4 / macOS, the title bar's native drag ends at once
/// ("Window move completed without beginning"), so the window cannot be moved. This follows the pointer and
/// sets <see cref="Window.Position"/> directly. The press is taken in the tunnel phase and marked handled, so
/// Arc's own handler never starts the failing native drag (which logs that warning on every move).
/// Double clicks are left to Arc to maximize or restore. Windows keeps Arc's native drag, which also provides Snap.
/// </remarks>
public static class TitleBarDrag
{
    public static bool IsNeeded => !OperatingSystem.IsWindows();

    public static void Attach(ArcWindow window)
    {
        if (!IsNeeded || window.TitleBar is not { } bar)
        {
            return;
        }

        PixelPoint? pointerStart = null;
        var windowStart = default(PixelPoint);

        bar.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            var point = e.GetCurrentPoint(window);
            if (!point.Properties.IsLeftButtonPressed || e.ClickCount > 1 ||
                window.WindowState != WindowState.Normal || IsOnControl(e.Source))
            {
                return;
            }

            pointerStart = window.PointToScreen(point.Position);
            windowStart = window.Position;
            e.Pointer.Capture(bar);
            e.Handled = true;
        }, RoutingStrategies.Tunnel);

        bar.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
        {
            if (pointerStart is not { } start)
            {
                return;
            }

            var now = window.PointToScreen(e.GetPosition(window));
            window.Position = new PixelPoint(windowStart.X + now.X - start.X, windowStart.Y + now.Y - start.Y);
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        void End(PointerEventArgs e)
        {
            if (pointerStart == null)
            {
                return;
            }

            pointerStart = null;
            e.Pointer.Capture(null);
        }

        bar.AddHandler(InputElement.PointerReleasedEvent, (_, e) => End(e), RoutingStrategies.Bubble, handledEventsToo: true);
        bar.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => pointerStart = null, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Buttons and other controls placed in the bar keep their own clicks.</summary>
    private static bool IsOnControl(object? source) =>
        source is Visual visual &&
        (visual.FindAncestorOfType<Avalonia.Controls.Button>(includeSelf: true) != null ||
         visual.FindAncestorOfType<TextBox>(includeSelf: true) != null);
}
