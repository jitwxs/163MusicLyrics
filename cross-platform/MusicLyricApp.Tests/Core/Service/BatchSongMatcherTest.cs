using MusicLyricApp.Core.Service;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Core.Service;

public class BatchSongMatcherTest
{
    [Fact]
    public void ParseQueries_HandlesPlainLinesAndDeduplicates()
    {
        var queries = BatchSongMatcher.ParseQueries("迷星叫\n音一会\n迷星叫\n");

        Assert.Collection(
            queries,
            query => Assert.Equal("迷星叫", query.Title),
            query => Assert.Equal("音一会", query.Title));
    }

    [Fact]
    public void ParseQueries_HandlesMarkdownRowsAndSkipsHeader()
    {
        const string input = """
                             | 序号 | 歌曲 | 歌手 |
                             | ---: | ---- | ---- |
                             | 1 | 迷星叫 | MyGO!!!!! |
                             | 2 | ホーミー・タイッ!! | 一家Dumb Rock!（开场嘉宾） |
                             """;

        var queries = BatchSongMatcher.ParseQueries(input);

        Assert.Equal(2, queries.Count);
        Assert.Equal(new BatchSongQuery("迷星叫", "MyGO!!!!!"), queries[0]);
        Assert.Equal(new BatchSongQuery("ホーミー・タイッ!!", "一家Dumb Rock!（开场嘉宾）"), queries[1]);
    }

    [Fact]
    public void ParseQueries_HandlesHashHeaderAndNetEaseLinks()
    {
        const string input = """
                             | # | 曲目 | 艺术家 | 网易云直达链接（Top1） | 候选数 |
                             |---:|---|---|---|---:|
                             | 1 | 迷星叫 | MyGO!!!!! | https://music.163.com/song?id=2131509441 | 3 |
                             """;

        var query = Assert.Single(BatchSongMatcher.ParseQueries(input));

        Assert.Equal("迷星叫", query.Title);
        Assert.Equal("MyGO!!!!!", query.Artist);
        Assert.Equal("2131509441", query.DirectSongId);
        Assert.Equal(SearchSourceEnum.NET_EASE_MUSIC, query.DirectSource);
    }

    [Fact]
    public void ParseQueries_IgnoresMarkdownProseAroundTable()
    {
        const string input = """
                             # MyGO 9th LIVE DAY2 曲目

                             下面是已经确认的直达链接。

                             | # | 曲目 | 艺术家 | 网易云直达链接（Top1） |
                             |---:|---|---|---|
                             | 1 | 音一会 | MyGO!!!!! | https://music.163.com/song?id=2123214058 |

                             来源：现场歌单。
                             """;

        var query = Assert.Single(BatchSongMatcher.ParseQueries(input));

        Assert.Equal("音一会", query.Title);
        Assert.Equal("2123214058", query.DirectSongId);
    }

    [Fact]
    public void Normalize_IgnoresWidthWhitespaceAndPunctuation()
    {
        Assert.Equal(
            BatchSongMatcher.Normalize("ホーミー・タイッ!!"),
            BatchSongMatcher.Normalize("ホーミー タイッ！！"));
    }

    [Fact]
    public void FindExactCandidates_RejectsFuzzyTitles()
    {
        var result = CreateResult(
            SearchSourceEnum.NET_EASE_MUSIC,
            ("1", "迷星叫", new[] { "MyGO!!!!!" }),
            ("2", "迷星叫 - Live", new[] { "MyGO!!!!!" }));

        var candidates = BatchSongMatcher.FindExactCandidates(
            new BatchSongQuery("迷星叫", null),
            [result]);

        var candidate = Assert.Single(candidates);
        Assert.Equal("1", candidate.SongId);
    }

    [Fact]
    public void RankCandidates_PrefersArtistThenNetEase()
    {
        var candidates = new[]
        {
            new BatchSongCandidate(SearchSourceEnum.NET_EASE_MUSIC, "1", "迷星叫", ["翻唱歌手"], "A", 1),
            new BatchSongCandidate(SearchSourceEnum.QQ_MUSIC, "2", "迷星叫", ["MyGO!!!!!"], "B", 1),
            new BatchSongCandidate(SearchSourceEnum.NET_EASE_MUSIC, "3", "迷星叫", ["MyGO!!!!!"], "C", 1)
        };

        var ranked = BatchSongMatcher.RankCandidates(candidates, "MyGO!!!!!");

        Assert.Equal(2, ranked.Count);
        Assert.Equal("3", ranked[0].SongId);
        Assert.Equal("2", ranked[1].SongId);
    }

    [Theory]
    [InlineData("影色舞", "MyGO!!!!!", "影色舞", "MyGO!!!!!", true)]
    [InlineData("ホーミー・タイッ!!", "一家Dumb Rock!", "ホーミー・タイッ！！", "一家Dumb Rock!", true)]
    [InlineData("影色舞", "MyGO!!!!!", "影色舞 (instrumental)", "MyGO!!!!!", false)]
    [InlineData("壱雫空", "MyGO!!!!!", "焚音打", "MyGO!!!!!", false)]
    [InlineData("歌いましょう鳴らしましょう", "MyGO!!!!!", "夢で逢いましょう", "SARD UNDERGROUND", false)]
    public void DirectSongMatchesQuery_RejectsWrongSongsAndInstrumentals(
        string requestedTitle,
        string requestedArtist,
        string actualTitle,
        string actualArtist,
        bool expected)
    {
        var query = new BatchSongQuery(requestedTitle, requestedArtist);

        Assert.Equal(expected, BatchSongMatcher.DirectSongMatchesQuery(query, actualTitle, [actualArtist]));
    }

    private static SearchResultVo CreateResult(
        SearchSourceEnum source,
        params (string id, string title, string[] artists)[] songs)
    {
        var result = new SearchResultVo
        {
            SearchSource = source,
            SearchType = SearchTypeEnum.SONG_ID
        };

        foreach (var song in songs)
        {
            result.SongVos.Add(new SearchResultVo.SongSearchResultVo
            {
                DisplayId = song.id,
                Title = song.title,
                AuthorName = song.artists,
                AlbumName = "album",
                Duration = 1
            });
        }

        return result;
    }
}
