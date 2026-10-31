using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class NetEaseMusicApiTests
{
    [Fact]
    public async Task NetEaseLyricsDecodeHtmlEntitiesBeforeOutput()
    {
        var nativeApi = new StubNetEaseMusicNativeApi(new LyricResult
        {
            Code = 200,
            Lrc = new Lrc { Lyric = "[01:08.960]Now I&apos;m feeling u &amp; you &#39;til dawn" },
            Tlyric = new Lrc { Lyric = "[01:08.960]It&apos;s translated" },
            Romalrc = new Lrc { Lyric = "[01:08.960]It&apos;s transliterated" }
        });
        var api = new NetEaseMusicApi(() => "", nativeApi);

        var result = api.GetLyricVo("322-test", "322-test", false);
        Assert.True(result.IsSuccess(), result.ErrorMsg);
        Assert.Equal("[01:08.960]Now I'm feeling u & you 'til dawn", result.Data.Lyric);
        Assert.Equal("[01:08.960]It's translated", result.Data.TranslateLyric);
        Assert.Equal("[01:08.960]It's transliterated", result.Data.TransliterationLyric);

        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        Assert.Equal("[01:08.960]Now I'm feeling u & you 'til dawn",
            Assert.Single(await LyricUtils.GetOutputContent(result.Data, settings)));
    }

    private sealed class StubNetEaseMusicNativeApi(LyricResult lyricResult) : NetEaseMusicNativeApi(() => "")
    {
        public override LyricResult GetLyric(string songId) => lyricResult;
    }
}
