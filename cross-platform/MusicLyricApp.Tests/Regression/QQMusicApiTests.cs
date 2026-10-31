using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;

namespace MusicLyricAppTest.Regression;

public class QQMusicApiTests
{
    [Fact]
    public void MissingPlaylistPayloadReturnsFailureInsteadOfThrowing()
    {
        QQMusicBean.PlaylistResult?[] malformedResponses =
        [
            null,
            new() { Code = 0, Cdlist = null! },
            new() { Code = 0, Cdlist = [] },
            new() { Code = 0, Cdlist = [null!] }
        ];

        foreach (var response in malformedResponses)
        {
            var api = CreateApi(response);

            var result = api.GetPlaylistVo(Guid.NewGuid().ToString("N"));

            Assert.False(result.IsSuccess());
            Assert.Equal(ErrorMsgConst.PLAYLIST_NOT_EXIST, result.ErrorMsg);
        }
    }

    [Fact]
    public void NullSongListIsHandledAsAnEmptyPlaylist()
    {
        var api = CreateApi(new QQMusicBean.PlaylistResult
        {
            Code = 0,
            Cdlist = [new QQMusicBean.Playlist { Dissname = "Empty playlist", SongList = null! }]
        });

        var result = api.GetPlaylistVo(Guid.NewGuid().ToString("N"));

        Assert.True(result.IsSuccess(), result.ErrorMsg);
        Assert.Empty(result.Data.SimpleSongVos);
    }

    [Fact]
    public void NullOrIncompleteSongsAreSkippedAndMissingMetadataDoesNotThrow()
    {
        const string songId = "qq-null-safe-id-297";
        const string songMid = "qq-null-safe-mid-297";
        var api = CreateApi(new QQMusicBean.PlaylistResult
        {
            Code = 0,
            Cdlist =
            [
                new QQMusicBean.Playlist
                {
                    SongList =
                    [
                        null!,
                        new QQMusicBean.Song { Id = "", Mid = "incomplete-mid" },
                        new QQMusicBean.Song
                        {
                            Id = songId,
                            Mid = songMid,
                            Name = "Valid playlist entry",
                            Singer = null!,
                            Album = null!
                        }
                    ]
                }
            ]
        });

        var playlist = api.GetPlaylistVo(Guid.NewGuid().ToString("N"));
        Assert.True(playlist.IsSuccess(), playlist.ErrorMsg);
        var entry = Assert.Single(playlist.Data.SimpleSongVos);
        Assert.Equal(songId, entry.Id);
        Assert.Equal(songMid, entry.DisplayId);
        Assert.Empty(entry.Singer);

        var songResult = api.GetSongVo([songMid]);
        Assert.True(songResult[songMid].IsSuccess(), songResult[songMid].ErrorMsg);
        Assert.Empty(songResult[songMid].Data.Singer);
        Assert.Equal(string.Empty, songResult[songMid].Data.Pics);
        Assert.Equal(string.Empty, songResult[songMid].Data.Album);
    }

    private static QQMusicApi CreateApi(QQMusicBean.PlaylistResult? result) =>
        new(() => "", new StubQQMusicNativeApi(result));

    private sealed class StubQQMusicNativeApi(QQMusicBean.PlaylistResult? result) : QQMusicNativeApi(() => "")
    {
        public override QQMusicBean.PlaylistResult GetPlaylist(string playlistId) => result!;
    }
}
