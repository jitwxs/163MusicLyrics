using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
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
