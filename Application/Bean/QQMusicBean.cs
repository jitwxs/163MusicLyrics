using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Bean
{
    public class QQMusicBean
    {
        public class SongResult
        {
            public long Code { get; set; }
            
            public Song[] Data { get; set; }
        }

        public class LyricResult
        {
            public long Code { get; set; }

            public string lyric { get; set; }
            
            public string trans { get; set; }

            public LyricResult Decode()
            {
                var decode = Encoding.UTF8.GetString(Convert.FromBase64String(lyric));
                lyric = Regex.Replace(decode, "\n", "\r\n");
                
                decode = Encoding.UTF8.GetString(Convert.FromBase64String(trans));
                trans = Regex.Replace(decode, "\n", "\r\n");

                return this;
            }
        }
        
        public class Song
        {
            public Album Album { get; set; }
            
            public int DataType { get; set; }
            
            public long Id { get; set; }
            
            public int Interval { get; set; }
            
            public string Mid  { get; set; }
            
            public string Name { get; set; }
            
            public Singer[] Singer { get; set; }
            
            public string Subtitle { get; set; }
            
            public string Title { get; set; }
        }

        public class Album
        {
            public long Id { get; set; }
            
            public string Mid  { get; set; }
            
            public string Name  { get; set; }
            
            public string Pmid  { get; set; }
            
            public string Subtitle { get; set; }
            
            public string TimePublic { get; set; }
            
            public string Title { get; set; }
        }

        public class Singer
        {
            public long Id { get; set; }
            
            public string Mid  { get; set; }
            
            public string Name  { get; set; }
            
            public string Pmid  { get; set; }
            
            public string Title { get; set; }
            
            public int Type { get; set; }
            
            public int Uin { get; set; }
        }
    }
}