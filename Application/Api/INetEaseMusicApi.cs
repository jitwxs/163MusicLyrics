using System.Collections.Generic;
using Application.Bean;

namespace Application.Api
{
    public interface INetEaseMusicApi
    {
        Dictionary<long, Datum> GetDatum(long[] songId, long bitrate = 999000);

        Dictionary<long, Song> GetSongs(long[] songIds);

        AlbumResult GetAlbum(long albumId);

        LyricResult GetLyric(long songId);
    }
}