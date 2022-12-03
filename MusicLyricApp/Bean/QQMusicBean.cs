using System;
using System.Text;

namespace MusicLyricApp.Bean
{
    public class QQMusicBean
    {
        /// <summary>
        /// 专辑查询接口返回结果
        /// </summary>
        public class AlbumResult
        {
            public long Code { get; set; }

            public AlbumInfo Data { get; set; }

            public string Message { get; set; }
        }

        /// <summary>
        /// 歌单查询接口返回结果
        /// </summary>
        public class PlaylistResult
        {
            public long Code { get; set; }
            
            public Playlist[] Cdlist { get; set; }
        }

        /// <summary>
        /// 歌曲查询接口返回结果
        /// </summary>
        public class SongResult
        {
            public long Code { get; set; }

            public Song[] Data { get; set; }
        }

        /// <summary>
        /// 歌词查询接口返回结果
        /// </summary>
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
        
        public class Playlist
        {
            /// <summary>
            /// 歌单 ID
            /// </summary>
            public string Disstid { get; set; }
            
            /// <summary>
            /// 歌单名
            /// </summary>
            public string Dissname { get; set; }
            
            /// <summary>
            /// 作者名
            /// </summary>
            public string Nickname { get; set; }
            
            /// <summary>
            /// 歌单封面
            /// </summary>
            public string Logo { get; set; }
            
            /// <summary>
            /// 歌单描述
            /// </summary>
            public string Desc { get; set; }

            /// <summary>
            /// 标签
            /// </summary>
            public Tag[] Tags { get; set; }
            
            /// <summary>
            /// 歌曲数量
            /// </summary>
            public long Songnum { get; set; }
            
            /// <summary>
            /// 歌曲 ID 列表，使用英文逗号分割
            /// </summary>
            public string Songids { get; set; }
            
            /// <summary>
            /// 歌曲列表
            /// </summary>
            public Song[] SongList { get; set; }
            
            /// <summary>
            /// 播放量
            /// </summary>
            public long Visitnum { get; set; }
            
            /// <summary>
            /// 创建时间
            /// </summary>
            public long CTime { get; set; }
        }
        
        public class Tag
        {
            public long Id { get; set; }
            
            public string Name { get; set; }
            
            public long Pid { get; set; }
        }

        public class Song
        {
            public Album Album { get; set; }

            public long Id { get; set; }

            public int Interval { get; set; }

            public string Mid { get; set; }

            public string Name { get; set; }

            public Singer[] Singer { get; set; }

            public string Title { get; set; }
            
            /// <summary>
            /// 发布时间，eg: 2005-07-08
            /// </summary>
            public string TimePublic { get; set; }
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