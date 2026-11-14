using System.Diagnostics;

namespace MusicLyricApp.Core.Service;

/// <summary>Opens a web page, folder or file with the system's default handler.</summary>
public static class ShellLauncher
{
    /// <summary>Opens <paramref name="target"/>; throws when the system cannot. Callers decide how to report it.</summary>
    public static void Open(string target)
    {
        using var _ = Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
    }
}
