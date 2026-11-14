using System;
using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Core.Localization;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core;

/// <summary>What the app knows about one lyrics source, apart from its API client.</summary>
/// <param name="LinkHost">Text that marks a link as this source's (a share link contains it).</param>
/// <param name="LinkKeywords">The part of a link before the id, for each search type.</param>
/// <param name="SongPageUrl">The web page of a song, from its id.</param>
/// <param name="Cookie">The cookie setting the source signs in with; null when it is used signed out.</param>
/// <param name="WebUrl">The source's website, where people sign in to copy the cookie; null without an account.</param>
public sealed record MusicSourceInfo(
    SearchSourceEnum Source,
    string LinkHost,
    IReadOnlyDictionary<SearchTypeEnum, string> LinkKeywords,
    Func<string, string> SongPageUrl,
    Func<ConfigBean, string>? Cookie,
    string? WebUrl = null)
{
    /// <summary>Display name in the current UI language.</summary>
    public string Name => LocalizationService.Get($"Enum.{nameof(SearchSourceEnum)}.{Source}");

    public bool HasAccount => Cookie != null;

    /// <summary>The start of a link to an item of <paramref name="type"/>, such as <c>163.com/song?id=</c>.</summary>
    public string LinkPrefix(SearchTypeEnum type) => LinkHost + "/" + LinkKeywords[type];
}

/// <summary>
/// The lyrics sources in one place. Adding a source means adding an entry here (plus its API client in
/// <c>SearchService</c> and its name under <c>Enum.SearchSourceEnum.*</c> in the localization files).
/// </summary>
public static class MusicSources
{
    public static IReadOnlyList<MusicSourceInfo> All { get; } =
    [
        new(SearchSourceEnum.NET_EASE_MUSIC, "163.com",
            Keywords("song?id=", "album?id=", "playlist?id="),
            id => $"https://music.163.com/#/song?id={Uri.EscapeDataString(id)}",
            config => config.NetEaseCookie,
            "https://music.163.com/"),
        new(SearchSourceEnum.QQ_MUSIC, "qq.com",
            Keywords("songDetail/", "albumDetail/", "playlist/"),
            id => $"https://y.qq.com/n/ryqq/songDetail/{Uri.EscapeDataString(id)}",
            config => config.QQMusicCookie,
            "https://y.qq.com/"),
        // qishui.douyin.com/s/ short links and music.douyin.com/qishui/share/ pages both contain "qishui".
        new(SearchSourceEnum.SODA_MUSIC, "qishui",
            Keywords("track_id=", "album_id=", "playlist_id="),
            id => $"https://music.douyin.com/qishui/share/track?track_id={Uri.EscapeDataString(id)}",
            null),
    ];

    public static MusicSourceInfo Get(SearchSourceEnum source) => All.First(s => s.Source == source);

    /// <summary>The sources that sign in with a cookie.</summary>
    public static IEnumerable<MusicSourceInfo> WithAccount => All.Where(s => s.HasAccount);

    private static Dictionary<SearchTypeEnum, string> Keywords(string song, string album, string playlist) => new()
    {
        [SearchTypeEnum.SONG_ID] = song,
        [SearchTypeEnum.ALBUM_ID] = album,
        [SearchTypeEnum.PLAYLIST_ID] = playlist,
    };
}
