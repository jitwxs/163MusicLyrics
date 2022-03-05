using System.Collections.Generic;

namespace 网易云歌词提取
{
    public interface INeteaseMusicApi
    {
        Dictionary<long, Datum> GetDatum(long[] songId, long bitrate = 999000);

        Dictionary<long, Song> GetSongs(long[] songIds);

        AlbumResult GetAlbum(long albumId);

        LyricResult GetLyric(long songId);
    }
}