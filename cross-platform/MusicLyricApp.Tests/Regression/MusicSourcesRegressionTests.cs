using MusicLyricApp.Core;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

/// <summary>The source table every source-specific fact is read from.</summary>
public class MusicSourcesRegressionTests
{
    [Fact]
    public void EverySourceHasOneEntryWithAKeywordForEachSearchType()
    {
        Assert.Equal(Enum.GetValues<SearchSourceEnum>().Order(), MusicSources.All.Select(s => s.Source).Order());
        foreach (var source in MusicSources.All)
        {
            Assert.Equal([SearchTypeEnum.SONG_ID, SearchTypeEnum.ALBUM_ID, SearchTypeEnum.PLAYLIST_ID],
                source.LinkKeywords.Keys.Order());
        }
    }

    [Fact]
    public void OnlySourcesWithACookieSettingHaveAnAccount()
    {
        Assert.Equal([SearchSourceEnum.NET_EASE_MUSIC, SearchSourceEnum.QQ_MUSIC],
            MusicSources.WithAccount.Select(s => s.Source).Order());
        var config = new SettingBean().Config;
        config.NetEaseCookie = "netease";
        config.QQMusicCookie = "qq";
        Assert.Equal("netease", MusicSources.Get(SearchSourceEnum.NET_EASE_MUSIC).Cookie!(config));
        Assert.Equal("qq", MusicSources.Get(SearchSourceEnum.QQ_MUSIC).Cookie!(config));
    }

    /// <summary>A link built from the table is read back as the same source, type and id, whatever source is selected.</summary>
    [Theory]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID, "186016")]
    [InlineData(SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.ALBUM_ID, "18903")]
    [InlineData(SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID, "0039MnYb0qxYhV")]
    [InlineData(SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.PLAYLIST_ID, "7256912512")]
    [InlineData(SearchSourceEnum.SODA_MUSIC, SearchTypeEnum.SONG_ID, "7686917")]
    public void LinkPrefixesAreRecognisedByTheInputParser(SearchSourceEnum source, SearchTypeEnum type, string id)
    {
        var other = source == SearchSourceEnum.QQ_MUSIC ? SearchSourceEnum.NET_EASE_MUSIC : SearchSourceEnum.QQ_MUSIC;
        var link = "https://music." + MusicSources.Get(source).LinkPrefix(type) + id;

        var input = GlobalUtils.CheckInputId(link, other, SearchTypeEnum.SONG_ID);

        Assert.Equal((source, type, id), (input.SearchSource, input.SearchType, input.QueryId));
    }

    [Fact]
    public void SongPagesEscapeTheId()
    {
        Assert.Equal("https://music.163.com/#/song?id=1%262", MusicSources.Get(SearchSourceEnum.NET_EASE_MUSIC).SongPageUrl("1&2"));
        Assert.StartsWith("https://y.qq.com/", MusicSources.Get(SearchSourceEnum.QQ_MUSIC).SongPageUrl("x"));
        Assert.StartsWith("https://music.douyin.com/", MusicSources.Get(SearchSourceEnum.SODA_MUSIC).SongPageUrl("x"));
    }
}
