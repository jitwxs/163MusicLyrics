using System;
using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicLyricApp.Api
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

        public string GetSongLink(string songMid)
        {
            var guid = GetGuid();

            var subData =
                "{\"req\": {\"module\": \"CDN.SrfCdnDispatchServer\",\"method\": \"GetCdnDispatch\"," +
                "\"param\": {\"guid\": \"" + guid + "\",\"calltype\": 0,\"userip\": \"\"}}," +
                "\"req_0\": {\"module\": \"vkey.GetVkeyServer\",\"method\": \"CgiGetVkey\",\"param\": " +
                "{\"guid\": \"8348972662\",\"songmid\": [\"" + songMid +
                "\"],\"songtype\": [1],\"uin\": \"0\",\"loginflag\": 1," +
                "\"platform\": \"20\"}},\"comm\": {\"uin\": 0,\"format\": \"json\",\"ct\": 24,\"cv\": 0}}";

            var requestUrl =
                "https://u.y.qq.com/cgi-bin/musicu.fcg?g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&data=" +
                subData;

            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "User-Agent", Useragent }
            };

            var resp = HttpUtils.HttpGet(requestUrl, "application/json", headers);
            var obj = (JObject)JsonConvert.DeserializeObject(resp);

            if (obj["code"].ToString() != "0")
            {
                return null;
            }

            return obj["req"]["data"]["sip"][0].ToString() + obj["req_0"]["data"]["midurlinfo"][0]["purl"] + ToString();
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

        protected virtual string GetGuid()
        {
            var guid = "";
            var r = new Random();
            for (var i = 0; i < 10; i++)
            {
                guid += Convert.ToString(r.Next(10));
            }

            return guid;
        }
    }
}