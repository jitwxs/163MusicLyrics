using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Service;

public partial class UpdateCheckService
{
    private readonly Func<GitHubInfo?> _fetchLatest;

    public UpdateCheckService(Func<GitHubInfo?>? fetchLatest = null)
    {
        _fetchLatest = fetchLatest ?? FetchLatest;
    }
    public const string ReleasePageUrl = AppInfo.ReleasesUrl;

    /// <summary>
    /// Checks for a newer release of the running version; when there is one, shows its notes and opens the release
    /// page if the user asks to. Throws when the check fails.
    /// </summary>
    /// <returns>False when the running version is the latest.</returns>
    public async Task<bool> OfferUpdateAsync(Func<string, Task<bool>>? showReleaseNotes = null, Action<string>? open = null)
    {
        var result = CheckLatestVersion(AppInfo.Title);
        if (!result.HasUpdate) return false;
        if (await (showReleaseNotes ?? DialogHelper.ShowReleaseNotesAsync)(BuildReleaseDescription(result.Info)))
        {
            (open ?? ShellLauncher.Open)(ReleasePageUrl);
        }

        return true;
    }

    public UpdateCheckResult CheckLatestVersion(string currentVersionText)
    {
        var info = _fetchLatest();

        return EvaluateLatestVersion(currentVersionText, info);
    }

    private static GitHubInfo? FetchLatest() => HttpUtils.HttpGet<GitHubInfo>(
            AppInfo.LatestReleaseApiUrl,
            "application/json",
            new Dictionary<string, string>
            {
                { "Accept", "application/vnd.github.v3+json" },
                { "User-Agent", BaseNativeApi.Useragent }
            });

    private static UpdateCheckResult EvaluateLatestVersion(string currentVersionText, GitHubInfo? info)
    {

        if (info == null)
        {
            throw new MusicLyricException(ErrorMsgConst.GET_LATEST_VERSION_FAILED);
        }

        if (!string.IsNullOrWhiteSpace(info.Message) && info.Message.Contains("API rate limit"))
        {
            throw new MusicLyricException(ErrorMsgConst.API_RATE_LIMIT);
        }

        var currentVersion = ParseVersion(currentVersionText);
        var latestVersion = ParseVersion(info.TagName);

        var hasUpdate = latestVersion.Major > currentVersion.Major ||
                        (latestVersion.Major == currentVersion.Major && latestVersion.Minor > currentVersion.Minor);

        return new UpdateCheckResult(hasUpdate, info);
    }

    public static string BuildReleaseDescription(GitHubInfo info)
    {
        var sb = new StringBuilder();
        sb
            .Append($"Tag: {info.TagName}").Append('\t')
            .Append($"UpdateTime: {info.PublishedAt.DateTime.AddHours(8)}").Append('\t')
            .Append($"DownloadCount: {info.Assets?.FirstOrDefault()?.DownloadCount ?? 0}").Append('\t')
            .Append($"Author: {info.Author?.Login ?? string.Empty}")
            .Append(Environment.NewLine)
            .Append(Environment.NewLine)
            .Append(info.Body);
        return sb.ToString();
    }

    private static (int Major, int Minor) ParseVersion(string text)
    {
        var match = VersionRegex().Match(text ?? string.Empty);
        if (!match.Success)
        {
            return (0, 0);
        }

        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
    }

    [GeneratedRegex(@"v(\d+)\.(\d+)")]
    private static partial Regex VersionRegex();
}

public record UpdateCheckResult(bool HasUpdate, GitHubInfo Info);

