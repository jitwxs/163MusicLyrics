using System;

namespace MusicLyricApp.Core;

/// <summary>The app's name, version and project links.</summary>
public static class AppInfo
{
    /// <summary>"MusicLyricApp v{major}.{minor}", from the assembly version set in the project file.</summary>
    public static string Title { get; } = BuildTitle();

    public const string HomeUrl = "https://github.com/jitwxs/163MusicLyrics";
    public const string WikiUrl = HomeUrl + "/wiki";
    public const string IssuesUrl = HomeUrl + "/issues";
    public const string ReleasesUrl = HomeUrl + "/releases";
    public const string LatestReleaseApiUrl = "https://api.github.com/repos/jitwxs/163MusicLyrics/releases/latest";

    private static string BuildTitle()
    {
        var version = typeof(AppInfo).Assembly.GetName().Version ?? new Version(0, 0);
        return $"MusicLyricApp v{version.Major}.{version.Minor}";
    }
}
