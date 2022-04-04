using System;
using System.Collections.Generic;
using Application.Bean;
using Application.Utils;

namespace Application.Api
{
    public class QQMusicNativeApi : BaseNativeApi
    {
        private static DateTime _dtFrom = new DateTime(1970, 1, 1, 8, 0, 0, 0, DateTimeKind.Local);

        protected override string HttpRefer()
        {
            return "https://c.y.qq.com/";
        }

        public QQMusicBean.AlbumResult GetAlbum(string albumMid)
        {
            var data = new Dictionary<string, string>
            {
                { "albummid", albumMid }
            };

            var resp = SendHttp("https://c.y.qq.com/v8/fcg-bin/fcg_v8_album_info_cp.fcg", data);
            
            return resp.ToEntity<QQMusicBean.AlbumResult>();
        }
        
        public QQMusicBean.SongResult GetSong(string songMid)
        {
            const string callBack = "getOneSongInfoCallback";

            var data = new Dictionary<string, string>
            {
                { "songmid", songMid },
                { "tpl", "yqq_song_detail" },
                { "format", "jsonp" },
                { "callback", callBack },
                { "g_tk", "5381" },
                { "jsonpCallback", callBack },
                { "loginUin", "0" },
                { "hostUin", "0" },
                { "outCharset", "utf8" },
                { "notice", "0" },
                { "platform", "yqq" },
                { "needNewCode", "0" },
            };

            var resp = SendHttp("https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg", data);

            return ResolveRespJson(callBack, resp).ToEntity<QQMusicBean.SongResult>();
        }

        public QQMusicBean.LyricResult GetLyric(string songMid)
        {
            var currentMillis = (DateTime.Now.ToLocalTime().Ticks - _dtFrom.Ticks) / 10000;

            const string callBack = "MusicJsonCallback_lrc";

            var data = new Dictionary<string, string>
            {
                { "callback", "MusicJsonCallback_lrc" },
                { "pcachetime", currentMillis + "" },
                { "songmid", songMid },
                { "g_tk", "5381" },
                { "jsonpCallback", callBack },
                { "loginUin", "0" },
                { "hostUin", "0" },
                { "format", "jsonp" },
                { "inCharset", "utf8" },
                { "outCharset", "utf8" },
                { "notice", "0" },
                { "platform", "yqq" },
                { "needNewCode", "0" },
            };

            var resp = SendHttp("https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg", data);

            var result = ResolveRespJson(callBack, resp).ToEntity<QQMusicBean.LyricResult>();

            return result.Decode();
        }

        private static string ResolveRespJson(string callBackSign, string val)
        {
            if (!val.StartsWith(callBackSign))
            {
                return "";
            }

            var jsonStr = val.Replace(callBackSign + "(", "");
            return jsonStr.Remove(jsonStr.Length - 1);
        }
    }
}