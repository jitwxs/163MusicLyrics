using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute(
        [System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable("MUSICLYRIC_LIVE_TESTS") != "1")
            Skip = "External services: set MUSICLYRIC_LIVE_TESTS=1 to opt in.";
    }
}

// No user settings or credentials are read; these are anonymous external-service probes.
[Trait("Category", "Live")]
public class LiveServiceTests
{
    [LiveFact]
    public void NetEaseSongMetadataAndLyrics()
    {
        AssertSong(new NetEaseMusicApi(() => ""), "347230");
    }

    [LiveFact]
    public async Task Issue322NetEaseLyricsDecodeRealSongEntities()
    {
        const string songId = "2079814495";
        var api = new NetEaseMusicApi(() => "");

        var result = api.GetLyricVo(songId, songId, false);

        Assert.True(result.IsSuccess(), result.ErrorMsg);
        Assert.Contains("Now I'm feeling u", result.Data.Lyric);
        Assert.DoesNotContain("&apos;", result.Data.Lyric, StringComparison.OrdinalIgnoreCase);

        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        var output = string.Join(Environment.NewLine, await LyricUtils.GetOutputContent(result.Data, settings));
        Assert.Contains("Now I'm feeling u", output);
        Assert.DoesNotContain("&apos;", output, StringComparison.OrdinalIgnoreCase);
    }

    [LiveFact]
    public async Task Issue297QqSongRetainsLineEndCuesWhenEmptyLinesAreEnabled()
    {
        const string songId = "331449";
        var api = new QQMusicApi(() => "");
        var songs = api.GetSongVo([songId]);
        Assert.True(songs.TryGetValue(songId, out var song), "QQ Music did not return the issue's test song.");
        Assert.True(song!.IsSuccess(), song.ErrorMsg);
        Assert.Contains("The Winner Takes It All", song.Data.Name);

        var lyrics = api.GetLyricVo(song.Data.Id, song.Data.DisplayId, false);
        Assert.True(lyrics.IsSuccess(), lyrics.ErrorMsg);
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        settings.Config.IgnoreEmptyLyric = false;
        var lines = Assert.Single(await LyricUtils.GetOutputContent(lyrics.Data, settings)).Split(Environment.NewLine);
        Assert.Contains("[00:34.400]", lines);
        Assert.Contains("[00:39.648]", lines);
        Assert.Contains("[00:46.877]", lines);

        settings.Config.IgnoreEmptyLyric = true;
        lines = Assert.Single(await LyricUtils.GetOutputContent(lyrics.Data, settings)).Split(Environment.NewLine);
        Assert.DoesNotContain("[00:34.400]", lines);
        Assert.DoesNotContain("[00:39.648]", lines);
        Assert.DoesNotContain("[00:46.877]", lines);
    }

    [LiveFact]
    public void QQSongMetadataAndLyrics()
    {
        // Sample song MID documented by QQMusicNativeApi.GetSong.
        AssertSong(new QQMusicApi(() => ""), "001RaE0n4RrGX9");
    }

    [LiveFact]
    public void GitHubReleaseEndpoint()
    {
        var result = new UpdateCheckService().CheckLatestVersion("MusicLyricApp v7.3");
        Assert.False(string.IsNullOrWhiteSpace(result.Info.TagName));
    }

    [LiveFact]
    public void SodaSongMetadataAndLyrics()
    {
        AssertSong(new SodaMusicApi(SodaMusicNativeApi.NewDeviceId), "7293547695373273090");
    }

    [LiveFact]
    public async Task SodaEndToEndSearchAlbumPlaylistAndShortLink()
    {
        var api = new SodaMusicApi(SodaMusicNativeApi.NewDeviceId);

        var search = api.Search("八部众曲", SearchTypeEnum.SONG_ID);
        Assert.True(search.IsSuccess(), search.ErrorMsg);
        Assert.NotEmpty(search.Data.SongVos);
        Assert.All(search.Data.SongVos, s => Assert.Matches("^\\d+$", s.DisplayId));

        var album = api.GetAlbumVo("7144550895936604162");
        Assert.True(album.IsSuccess(), album.ErrorMsg);
        Assert.Equal(9, album.Data.SimpleSongVos.Length);

        var playlist = api.GetPlaylistVo("7031082140935145486");
        Assert.True(playlist.IsSuccess(), playlist.ErrorMsg);
        // Signed out, only the first page of a playlist is returned (count_tracks); later pages are empty.
        Assert.NotEmpty(playlist.Data.SimpleSongVos);

        var lyric = api.GetLyricVo("7144550907370276865", "7144550907370276865", false).Assert().Data;
        Assert.Contains("困在床上", lyric.TranslateLyric);
        var settings = new SettingBean();
        settings.Config.VerbatimLyricMode = VerbatimLyricModeEnum.STANDARD_MODE;
        var verbatim = api.GetLyricVo("7144550907370276865", "7144550907370276865", true).Assert().Data;
        Assert.NotEmpty(await LyricUtils.GetOutputContent(verbatim, settings));

        var shortLink = GlobalUtils.CheckInputId("https://qishui.douyin.com/s/69HXcAV/", SearchSourceEnum.NET_EASE_MUSIC,
            SearchTypeEnum.SONG_ID);
        Assert.Equal(SearchSourceEnum.SODA_MUSIC, shortLink.SearchSource);
        Assert.Equal(SearchTypeEnum.ALBUM_ID, shortLink.SearchType);
        Assert.Equal("7144550895936604162", shortLink.QueryId);
    }

    private static void AssertSong(IMusicApi api, string id)
    {
        var result = api.GetSongVo([id]);
        Assert.True(result.TryGetValue(id, out var entry), "Service did not return the requested song.");
        Assert.True(entry!.IsSuccess(), entry.ErrorMsg);
        Assert.False(string.IsNullOrWhiteSpace(entry.Data.Name));
        var lyrics = api.GetLyricVo(entry.Data.Id, entry.Data.DisplayId, false);
        Assert.True(lyrics.IsSuccess(), lyrics.ErrorMsg);
        Assert.False(lyrics.Data.IsEmpty(), "Service returned empty lyrics for the probe song.");
    }
}
