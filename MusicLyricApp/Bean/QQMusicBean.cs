using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicLyricApp.Bean
{
    public class QQMusicBean
    {
        public class AlbumResult
        {
            public long Code { get; set; }

            public AlbumInfo Data { get; set; }

            public string Message { get; set; }
        }

        public class SongResult
        {
            public long Code { get; set; }

            public Song[] Data { get; set; }
        }

        public class LyricResult
        {
            public long Code { get; set; }

            public string Lyric { get; set; }

            public string Trans { get; set; }

            public LyricResult Decode()
            {
                if (Lyric != null)
                {
                    Lyric = Encoding.UTF8.GetString(Convert.FromBase64String(Lyric));
                }

                if (Trans != null)
                {
                    Trans = Encoding.UTF8.GetString(Convert.FromBase64String(Trans));
                }

                return this;
            }
        }

        public class Song
        {
            public Album Album { get; set; }

            public int DataType { get; set; }

            public long Id { get; set; }

            public int Interval { get; set; }

            public string Mid { get; set; }

            public string Name { get; set; }

            public Singer[] Singer { get; set; }

            public string Subtitle { get; set; }

            public string Title { get; set; }
        }

        public class AlbumInfo
        {
            public string ADate { get; set; }
            
            public string Company { get; set; }

            public long Id { get; set; }

            public string Lan { get; set; }

            public AlbumSong[] List { get; set; }

            public string Mid { get; set; }

            public string Name { get; set; }

            public long Singerid { get; set; }

            public string Singermid { get; set; }

            public string Singername { get; set; }

            public int Total { get; set; }
        }

        public class AlbumSong
        {
            public Singer[] singer { get; set; }
            
            public long Songid { get; set; }

            public string Songmid { get; set; }

            public string Songname { get; set; }
        }

        public class Album
        {
            public long Id { get; set; }

            public string Mid { get; set; }

            public string Name { get; set; }

            public string Pmid { get; set; }

            public string Subtitle { get; set; }

            public string TimePublic { get; set; }

            public string Title { get; set; }
        }

        public class Singer
        {
            public long Id { get; set; }

            public string Mid { get; set; }

            public string Name { get; set; }

            public string Pmid { get; set; }

            public string Title { get; set; }

            public int Type { get; set; }

            public long Uin { get; set; }
        }
    }
}