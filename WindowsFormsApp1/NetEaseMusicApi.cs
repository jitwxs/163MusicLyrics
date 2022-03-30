using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Numerics;

namespace 网易云歌词提取
{
    public class NetEaseMusicApi : INeteaseMusicApi
    {
        // General
        private const string _MODULUS =
            "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7";

        private const string _NONCE = "0CoJUm6Qyw8W8jud";
        private const string _PUBKEY = "010001";
        private const string _VI = "0102030405060708";

        public static string _USERAGENT =
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

        private const string _COOKIE =
            "os=pc;osver=Microsoft-Windows-10-Professional-build-16299.125-64bit;appver=2.0.3.131777;channel=netease;__remember_me=true";

        private const string _REFERER = "http://music.163.com/";

        // use keygen in c#
        private string _secretKey;
        private string _encSecKey;

        public NetEaseMusicApi()
        {
            _secretKey = CreateSecretKey(16);
            _encSecKey = RSAEncode(_secretKey);
        }

        public Dictionary<long, Datum> GetDatum(long[] songId, long bitrate = 999000)
        {
            var result = new Dictionary<long, Datum>();
            
            var urls = GetSongsUrl(songId, bitrate);
            if (urls.Code == 200)
            {
                foreach (var datum in urls.Data)
                {
                    result.Add(datum.Id, datum);
                }
            }

            return result;
        }

        public Dictionary<long, Song> GetSongs(long[] songIds)
        {
            var result = new Dictionary<long, Song>();

            if (songIds == null || songIds.Length < 1)
            {
                return result;
            }
            
            var detailResult = GetDetail(songIds);
            if (detailResult == null || detailResult.Code != 200)
            {
                return result;
            }
            
            foreach (var song in detailResult.Songs)
            {
                result[song.Id] = song;
            }
           
            return result;
        }

        public AlbumResult GetAlbum(long albumId)
        {
            var url = "https://music.163.com/weapi/v1/album/" + albumId + "?csrf_token=";
            
            var data = new Dictionary<string, string>
            {
                { "csrf_token", string.Empty },
            };

            var raw = CURL(url, Prepare(JsonConvert.SerializeObject(data)));
            
            return JsonConvert.DeserializeObject<AlbumResult>(raw);
        }

        public LyricResult GetLyric(long songId)
        {
            const string url = "https://music.163.com/weapi/song/lyric?csrf_token=";
            
            var data = new Dictionary<string, string>
            {
                { "id", songId.ToString() },
                { "os", "pc" },
                { "lv", "-1" },
                { "kv", "-1" },
                { "tv", "-1" },
                { "csrf_token", string.Empty }
            };

            var raw = CURL(url, Prepare(JsonConvert.SerializeObject(data)));
            
            return JsonConvert.DeserializeObject<LyricResult>(raw);
        }
        
        private SongUrls GetSongsUrl(long[] songId, long bitrate = 999000)
        {
            const string url = "https://music.163.com/weapi/song/enhance/player/url?csrf_token=";

            var data = new GetSongUrlJson
            {
                ids = songId,
                br = bitrate
            };

            var raw = CURL(url, Prepare(JsonConvert.SerializeObject(data)));

            return JsonConvert.DeserializeObject<SongUrls>(raw);
        }

        /**
         * 批量获取歌曲详情
         */
        private DetailResult GetDetail(IEnumerable<long> songIds)
        {
            const string url = "https://music.163.com/weapi/v3/song/detail?csrf_token=";

            var songRequests = new StringBuilder();
            foreach (var songId in songIds)
            {
                songRequests.Append("{'id':'").Append(songId).Append("'}").Append(',');
            }
            
            var data = new Dictionary<string, string>
            {
                {
                    "c",
                    "[" + songRequests.Remove(songRequests.Length - 1, 1) + "]"
                },
                { "csrf_token", string.Empty },
            };
            
            var raw = CURL(url, Prepare(JsonConvert.SerializeObject(data)));

            return JsonConvert.DeserializeObject<DetailResult>(raw);
        }

        private class GetSongUrlJson
        {
            public long[] ids;
            public long br;
            public string csrf_token = string.Empty;
        }

        private string CreateSecretKey(int length)
        {
            var str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var sb = new StringBuilder(length);
            var rnd = new Random();
            
            for (var i = 0; i < length; ++i)
            {
                sb.Append(str[rnd.Next(0, str.Length)]);
            }

            return sb.ToString();
        }

        private Dictionary<string, string> Prepare(string raw)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["params"] = AESEncode(raw, _NONCE);
            data["params"] = AESEncode(data["params"], _secretKey);
            data["encSecKey"] = _encSecKey;

            return data;
        }

        // encrypt mod
        private string RSAEncode(string text)
        {
            string srtext = new string(text.Reverse().ToArray());
            var a = BCHexDec(BitConverter.ToString(Encoding.Default.GetBytes(srtext)).Replace("-", string.Empty));
            var b = BCHexDec(_PUBKEY);
            var c = BCHexDec(_MODULUS);
            var key = BigInteger.ModPow(a, b, c).ToString("x");
            key = key.PadLeft(256, '0');
            if (key.Length > 256)
                return key.Substring(key.Length - 256, 256);
            else
                return key;
        }

        // fake curl
        private string CURL(string url, Dictionary<string, string> parms, string method = "POST")
        {
            string result;
            using (var wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                wc.Headers.Add(HttpRequestHeader.Referer, _REFERER);
                wc.Headers.Add(HttpRequestHeader.UserAgent, _USERAGENT);
                wc.Headers.Add(HttpRequestHeader.Cookie, _COOKIE);
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                foreach (var keyPair in parms)
                {
                    reqparm.Add(keyPair.Key, keyPair.Value);
                }

                var bytes = wc.UploadValues(url, method, reqparm);
                result = Encoding.UTF8.GetString(bytes);
            }
            System.Diagnostics.Debug.WriteLine(result);
            return result;
        }

        private BigInteger BCHexDec(string hex)
        {
            var dec = new BigInteger(0);
            var len = hex.Length;
            
            for (var i = 0; i < len; i++)
            {
                dec += BigInteger.Multiply(new BigInteger(Convert.ToInt32(hex[i].ToString(), 16)),
                    BigInteger.Pow(new BigInteger(16), len - i - 1));
            }

            return dec;
        }

        private string AESEncode(string secretData, string secret = "TA3YiYCfY2dDJQgg")
        {
            byte[] encrypted;
            byte[] IV = Encoding.UTF8.GetBytes(_VI);

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(secret);
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var stream = new MemoryStream())
                    {
                        using (var cstream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var sw = new StreamWriter(cstream))
                            {
                                sw.Write(secretData);
                            }

                            encrypted = stream.ToArray();
                        }
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }
    }
}