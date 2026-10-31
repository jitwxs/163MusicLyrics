using MusicLyricApp.Core;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class LyricsRegressionTests
{
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    [Theory]
    [InlineData("", "")]
    [InlineData(" \r\n ", "")]
    [InlineData("1\n00:00:01,500 --> 00:00:02,000\nhello", "[00:01.500]hello")]
    [InlineData("00:00:01.5 --> 00:00:02.0\nhello", "[00:01.500]hello")]
    [InlineData("1\r\n00:00:01,05 --> 00:00:02,000\r\nhello\r\nworld", "[00:01.050]hello world")]
    [InlineData("1\r00:00:01,005 --> 00:00:02,000\r你好", "[00:01.005]你好")]
    [InlineData("malformed block", "")]
    public void SrtImportHandlesNewlinesFractionsAndEmptyInput(string source, string expected) =>
        Assert.Equal(expected, SrtUtils.SrtToLrc(source, "[mm:ss.SSS]", DotTypeEnum.DOWN));

    [Fact]
    public void SrtImportSortsCues()
    {
        var input = "2\n00:00:03,000 --> 00:00:04,000\nlater\n\n1\n00:00:01,000 --> 00:00:02,000\nearlier";
        Assert.Equal(Lines("[00:01.000]earlier", "[00:03.000]later"), SrtUtils.SrtToLrc(input, "[mm:ss.SSS]", DotTypeEnum.DOWN));
    }

    [Fact]
    public void SrtImportRejectsInvalidStart() => Assert.Throws<MusicLyricException>(() =>
        SrtUtils.SrtToLrc("1\ninvalid --> 00:00:02,000\nhello", "[mm:ss.SSS]", DotTypeEnum.DOWN));

    [Fact]
    public void LrcExportUsesNextCueAndSongDuration()
    {
        var cues = new List<LyricLineVo> { new("first", new(1000)), new("second", new(3000)) };
        var result = SrtUtils.LrcToSrt(cues, "HH:mm:ss,SSS", DotTypeEnum.DOWN, 5000);
        Assert.Equal(Lines("1", "00:00:01,000 --> 00:00:03,000", "first", "", "2", "00:00:03,000 --> 00:00:05,000", "second", "", ""), result);
        Assert.Equal(Lines("[00:01.000]first", "[00:03.000]second"), SrtUtils.SrtToLrc(result, "[mm:ss.SSS]", DotTypeEnum.DOWN));
    }

    [Fact]
    public void SimultaneousBilingualCuesHaveSameEnd()
    {
        var cues = new List<LyricLineVo> { new("原文", new(1000)), new("translation", new(1000)), new("next", new(3000)) };
        var output = SrtUtils.LrcToSrt(cues, "HH:mm:ss,SSS", DotTypeEnum.DOWN, 5000);
        Assert.Equal(2, output.Split("00:00:01,000 --> 00:00:03,000").Length - 1);
        Assert.Contains("00:00:03,000 --> 00:00:05,000", output);
    }

    [Fact]
    public void EmptyLrcExportsEmptySrt() => Assert.Empty(SrtUtils.LrcToSrt([], "HH:mm:ss,SSS", DotTypeEnum.DOWN, 5000));

    [Theory]
    [InlineData(ShowLrcTypeEnum.STAGGER, "[00:01.000]hello\n[00:01.000]你好\n[00:02.000]world\n[00:02.000]世界")]
    [InlineData(ShowLrcTypeEnum.MERGE, "[00:01.000]hello / 你好\n[00:02.000]world / 世界")]
    [InlineData(ShowLrcTypeEnum.ISOLATED, "[00:01.000]hello\n[00:02.000]world\n[00:01.000]你好\n[00:02.000]世界")]
    public async Task BilingualLayoutsPreserveOrdering(ShowLrcTypeEnum layout, string expected)
    {
        var settings = new SettingBean();
        settings.Param.ShowLrcType = layout;
        settings.Param.LrcMergeSeparator = " / ";
        var lyric = new LyricVo { Lyric = "[00:01.000]hello\n[00:02.000]world", TranslateLyric = "[00:01.000]你好\n[00:02.000]世界" };
        Assert.Equal(expected.Replace("\n", Environment.NewLine), Assert.Single(await LyricUtils.GetOutputContent(lyric, settings)));
    }

    [Fact]
    public async Task IsolatedOutputCreatesOneDocumentPerLanguage()
    {
        var settings = new SettingBean();
        settings.Param.ShowLrcType = ShowLrcTypeEnum.ISOLATED;
        settings.Config.SeparateFileForIsolated = true;
        var output = await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]hello", TranslateLyric = "[00:01.000]你好" }, settings);
        Assert.Equal(new[] { "[00:01.000]hello", "[00:01.000]你好" }, output);
    }

    [Fact]
    public async Task MissingConfiguredTranslationDoesNotMakeOriginInsertionIndexInvalid()
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "2,0"; // Chinese translation, then original.
        var lyric = new LyricVo { Lyric = "[00:01.000]这是中文歌词" };

        var output = await LyricUtils.GetOutputContent(lyric, settings);

        Assert.Equal("[00:01.000]这是中文歌词", Assert.Single(output));
    }

    [Theory]
    [InlineData(TransLyricLostRuleEnum.IGNORE, "")]
    [InlineData(TransLyricLostRuleEnum.EMPTY_LINE, "[00:01.000]")]
    [InlineData(TransLyricLostRuleEnum.FILL_ORIGIN, "[00:01.000]hello")]
    public async Task MissingTranslationHonorsPolicy(TransLyricLostRuleEnum rule, string expected)
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "1";
        settings.Config.TransConfig.LostRule = rule;
        var output = await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]hello" }, settings);
        Assert.Equal(expected, string.Join("", output));
    }

    [Theory]
    [InlineData(0, "[00:01.020]你好")]
    [InlineData(20, "[00:01.000]你好")]
    public async Task TranslationToleranceAlignsTimestamps(int tolerance, string expected)
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "1";
        settings.Config.TransConfig.MatchPrecisionDeviation = tolerance;
        var output = await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]hello", TranslateLyric = "[00:01.020]你好" }, settings);
        Assert.Equal(expected, Assert.Single(output));
    }

    [Theory]
    [InlineData(ChineseProcessRuleEnum.SIMPLIFIED_CHINESE, "音樂", "音乐")]
    [InlineData(ChineseProcessRuleEnum.TRADITIONAL_CHINESE, "音乐", "音樂")]
    [InlineData(ChineseProcessRuleEnum.IGNORE, "音樂", "音樂")]
    public async Task ChineseConversion(ChineseProcessRuleEnum rule, string text, string expected)
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        settings.Config.ChineseProcessRule = rule;
        Assert.Equal("[00:01.000]" + expected, Assert.Single(await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]" + text }, settings)));
    }

    [Fact]
    public async Task TransliterationUsesItsOwnText()
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "4";
        Assert.Equal("[00:01.000]konnichiwa", Assert.Single(await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]こんにちは", TransliterationLyric = "[00:01.000]konnichiwa" }, settings)));
    }

    [Fact]
    public async Task EmptyOutputSelectionProducesNoFiles()
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "";
        Assert.Empty(await LyricUtils.GetOutputContent(TestWorkspace.Song().LyricVo, settings));
    }

    [Theory]
    [InlineData(true, "[00:02.000]hello")]
    [InlineData(false, "[00:01.000]\n[00:02.000]hello")]
    public async Task EmptyLinePolicy(bool ignore, string expected)
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        settings.Config.IgnoreEmptyLyric = ignore;
        Assert.Equal(expected.Replace("\n", Environment.NewLine), Assert.Single(await LyricUtils.GetOutputContent(new LyricVo { Lyric = "[00:01.000]\n[00:02.000]hello" }, settings)));
    }

    [Theory]
    [InlineData(false, "[00:32.775]I don't wanna talk\n[00:34.400]\n[00:36.707]About things we've gone through\n[00:39.648]\n[00:40.626]Though it's hurting me now it's history\n[00:46.877]")]
    [InlineData(true, "[00:32.775]I don't wanna talk\n[00:36.707]About things we've gone through\n[00:40.626]Though it's hurting me now it's history")]
    public async Task QqCommonLyricsHonorEmptyLineSettingForLineEndCues(bool ignore, string expected)
    {
        const string qqLyrics = "[32775,1625]I (32775,268)don't (33043,397)wanna (33440,216)talk(33656,744)\n" +
                                "[36707,2941]About (36707,677)things (37384,552)we've (37936,224)gone (38160,344)through(38504,1144)\n" +
                                "[40626,6251]Though (40626,272)it's (40898,225)hurting (41123,504)me (41627,705)now (44229,639)it's (44868,264)history(45132,1745)";
        var commonLyrics = VerbatimLyricUtils.ConvertVerbatimLyricToCommonLyric4QQMusic(qqLyrics);
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        settings.Config.IgnoreEmptyLyric = ignore;

        var output = await LyricUtils.GetOutputContent(new LyricVo
        {
            SearchSource = SearchSourceEnum.QQ_MUSIC,
            Lyric = commonLyrics
        }, settings);

        Assert.Equal(expected.Replace("\n", Environment.NewLine), Assert.Single(output));
    }

    [Fact]
    public async Task QQHeaderIsRemoved()
    {
        var settings = new SettingBean();
        settings.Config.OutputLyricTypes = "0";
        Assert.Equal("[00:01.000]hello", Assert.Single(await LyricUtils.GetOutputContent(new LyricVo { SearchSource = SearchSourceEnum.QQ_MUSIC, Lyric = "[ti:header]\n[offset:0]\n[00:01.000]hello" }, settings)));
    }

    [Theory]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC, "[00:00.000]纯音乐，请欣赏")]
    [InlineData(SearchSourceEnum.QQ_MUSIC, "[00:00.000]此歌曲为没有填词的纯音乐，请您欣赏")]
    public void PureMusicDetection(SearchSourceEnum source, string lyric)
    {
        var vo = new LyricVo { SearchSource = source, Lyric = lyric };
        Assert.True(vo.IsPureMusic());
        vo.TranslateLyric = "translation";
        Assert.False(vo.IsPureMusic());
    }
}
