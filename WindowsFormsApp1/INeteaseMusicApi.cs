using System.Collections.Generic;

namespace 网易云歌词提取
{
    public interface INeteaseMusicApi
    {
        Dictionary<long, Datum> GetDatum(long[] songId, long bitrate = 999000);

        DetailResult GetDetail(long songId);
        
        Song GetSong(long songId);

        AlbumResult GetAlbum(long albumId);

        LyricResult GetLyric(long songId);
    }
}