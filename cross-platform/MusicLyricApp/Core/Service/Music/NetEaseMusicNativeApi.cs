using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MusicLyricApp.Core.Utils;
using MusicLyricApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicLyricApp.Core.Service.Music;

public class NetEaseMusicNativeApi : BaseNativeApi
{
    // General
    private const string MODULUS =
        "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7";

    private const string NONCE = "0CoJUm6Qyw8W8jud";
    private const string PUBKEY = "010001";
    private const string VI = "0102030405060708";

    // use keygen in c#
    private readonly string _secretKey;
    private readonly string _encSecKey;

    public NetEaseMusicNativeApi(Func<string> cookieFunc) : base(cookieFunc)
    {
        _secretKey = CreateSecretKey(16);
        _encSecKey = RSAEncode(_secretKey);
    }

    protected override string HttpRefer()
    {
        return "https://music.163.com/";
    }

    public ResultVo<SearchResult> Search(string keyword, SearchTypeEnum searchType)
    {
        const string url = "https://music.163.com/weapi/cloudsearch/get/web";

        // 1: 单曲, 10: 专辑, 100: 歌手, 1000: 歌单, 1002: 用户, 1004: MV, 1006: 歌词, 1009: 电台, 1014: 视频, 1018:综合, 2000:声音
        string type;
        switch (searchType)
        {
            case SearchTypeEnum.SONG_ID:
                type = "1";
                break;
            case SearchTypeEnum.ALBUM_ID:
                type = "10";
                break;
            case SearchTypeEnum.PLAYLIST_ID:
                type = "1000";
                break;
            default:
                throw new MusicLyricException(ErrorMsgConst.SYSTEM_ERROR);
        }

        var data = new Dictionary<string, string>
        {
            { "csrf_token", string.Empty },
            { "s", keyword },
            { "type", type },
            { "limit", "20" },
            { "offset", "0" }
        };

        var res = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));

        var obj = (JObject)JsonConvert.DeserializeObject(res);
        if (obj == null)
        {
            return ResultVo<SearchResult>.Failure(ErrorMsgConst.SONG_NOT_EXIST);
        }

        var code = obj["code"].ToString();
        var result = obj["result"];

        if (code == "50000005")
        {
            var fallbackUrl = "https://music.163.com/api/search/get/web" +
                              $"?s={Uri.EscapeDataString(keyword)}&type={type}&limit=20&offset=0";
            obj = (JObject)JsonConvert.DeserializeObject(SendGet(fallbackUrl));
            code = obj?["code"]?.ToString();
            result = obj?["result"];
        }

        if (result == null || code != "200")
        {
            return ResultVo<SearchResult>.Failure(ErrorMsgConst.SONG_NOT_EXIST);
        }

        var resultStr = result.ToString();

        if (obj["abroad"] != null && bool.Parse(obj["abroad"].ToString()))
        {
            resultStr = NetEaseMusicSearchUtils.Decode(resultStr);
        }

        resultStr = NormalizeLegacySearchResult(resultStr);
        return new ResultVo<SearchResult>(JsonConvert.DeserializeObject<SearchResult>(resultStr));
    }

    private static string NormalizeLegacySearchResult(string resultStr)
    {
        var result = JObject.Parse(resultStr);
        NormalizeLegacySongs(result);
        return result.ToString(Formatting.None);
    }

    private static void NormalizeLegacySongs(JObject result)
    {
        if (result["songs"] is not JArray songs)
        {
            return;
        }

        foreach (var song in songs.OfType<JObject>())
        {
            song["ar"] ??= song["artists"];
            song["al"] ??= song["album"];
            song["dt"] ??= song["duration"];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="bitrate"></param>
    /// <exception cref="WebException"></exception>
    /// <returns></returns>
    public Dictionary<string, Datum> GetDatum(string[] songId, long bitrate = 999000)
    {
        var result = new Dictionary<string, Datum>();

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songIds"></param>
    /// <exception cref="WebException"></exception>
    /// <returns></returns>
    public Dictionary<string, Song> GetSongs(string[] songIds)
    {
        var result = new Dictionary<string, Song>();

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="albumId"></param>
    /// <returns></returns>
    /// <exception cref="WebException"></exception>
    public AlbumResult GetAlbum(string albumId)
    {
        var url = $"https://music.163.com/weapi/v1/album/{albumId}?csrf_token=";

        var data = new Dictionary<string, string>
        {
            { "csrf_token", string.Empty },
        };

        var raw = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));

        var albumResult = JsonConvert.DeserializeObject<AlbumResult>(raw);

        if (albumResult.Code == 200)
        {
            var blurPicUrl = albumResult.Album.BlurPicUrl;
            foreach (var song in albumResult.Songs)
            {
                // use blur to fault song pic url
                if (song.Al != null && string.IsNullOrEmpty(song.Al.PicUrl))
                {
                    song.Al.PicUrl = blurPicUrl;
                }
            }
        }

        return albumResult;
    }

    public PlaylistResult GetPlaylist(string playlistId)
    {
        var url = $"https://music.163.com/weapi/v6/playlist/detail?csrf_token=";

        var data = new Dictionary<string, string>
        {
            { "csrf_token", string.Empty },
            { "id", playlistId },
            { "offset", "0" },
            { "total", "true" },
            { "limit", "1000" },
            { "n", "1000" }
        };

        var raw = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));

        return JsonConvert.DeserializeObject<PlaylistResult>(raw);
    }

    /// <summary>
    /// 获得原始歌词结果
    /// </summary>
    /// <param name="songId">音乐ID</param>
    /// <exception cref="WebException"></exception>
    /// <returns>一个
    /// <see cref="LyricResult"/></returns>
    public LyricResult GetLyric(string songId)
    {
        const string url = "https://music.163.com/weapi/song/lyric?csrf_token=";

        var data = new Dictionary<string, string>
        {
            { "id", songId },
            { "os", "pc" },
            { "lv", "-1" }, // native lyrics
            { "kv", "-1" },
            { "tv", "-1" }, // translated lyrics
            { "rv", "-1" }, // transliteration lyrics
            { "yv", "-1" }, // word lyrics
            { "ytv", "-1" }, // word lyrics
            { "yrv", "-1" }, // word lyrics
            { "csrf_token", string.Empty }
        };

        var raw = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));

        return JsonConvert.DeserializeObject<LyricResult>(raw);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    /// <exception cref="WebException"></exception>
    private SongUrls GetSongsUrl(string[] songId, long bitrate = 999000)
    {
        const string url = "https://music.163.com/weapi/song/enhance/player/url?csrf_token=";

        var data = new Dictionary<string, string>
        {
            { "ids", $"[{string.Join(",", songId)}]" },
            { "br", bitrate.ToString() },
            { "csrf_token", string.Empty }
        };

        var raw = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));

        return JsonConvert.DeserializeObject<SongUrls>(raw);
    }

    /// <summary>
    /// 批量获得歌曲详情
    /// </summary>
    /// <param name="inputSongIds">歌曲ID</param>
    /// <exception cref="WebException"></exception>
    /// <returns></returns>
    private DetailResult GetDetail(IEnumerable<string> inputSongIds)
    {
        const string url = "https://music.163.com/weapi/v3/song/detail?csrf_token=";

        var requestedIds = inputSongIds.Distinct().ToList();
        var allResults = new List<Song>();
        var cnt = 1;
        
        foreach (var songIds in GlobalUtils.Batch(requestedIds, Constants.BatchQuerySize))
        {
            var songs = songIds.Select(id => new { id });
            var data = new Dictionary<string, string>
            {
                { "c", JsonConvert.SerializeObject(songs) },
                { "csrf_token", string.Empty }
            };

            var raw = SendPost(url, Prepare(JsonConvert.SerializeObject(data)));
            var partialResult = JsonConvert.DeserializeObject<DetailResult>(raw);
            if (partialResult?.Code == 200)
            {
                allResults.AddRange(partialResult.Songs);
            }

            if (cnt++ % 2 == 0)
            {
                Thread.Sleep(Constants.SleepMsBetweenBatchQuery); // sleep 500ms after every two batches
            }
        }

        var foundIds = allResults.Select(song => song.Id).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();
        foreach (var songIds in GlobalUtils.Batch(missingIds, Constants.BatchQuerySize))
        {
            var idList = $"[{string.Join(',', songIds)}]";
            var fallbackUrl = "https://music.163.com/api/song/detail/" +
                              $"?ids={Uri.EscapeDataString(idList)}";
            var fallbackObject = JObject.Parse(SendGet(fallbackUrl));
            NormalizeLegacySongs(fallbackObject);
            var fallbackResult = fallbackObject.ToObject<DetailResult>();
            if (fallbackResult?.Code == 200 && fallbackResult.Songs != null)
            {
                allResults.AddRange(fallbackResult.Songs.Where(song => foundIds.Add(song.Id)));
            }
        }
        
        return new DetailResult
        {
            Code = 200,
            Songs = allResults.ToArray()
        };
    }

    private Dictionary<string, string> Prepare(string raw)
    {
        var data = new Dictionary<string, string>();
        data["params"] = AESEncode(raw, NONCE);
        data["params"] = AESEncode(data["params"], _secretKey);
        data["encSecKey"] = _encSecKey;

        return data;
    }

    // encrypt mod
    private static string RSAEncode(string text)
    {
        var srtext = new string(text.Reverse().ToArray());
        var a = BCHexDec(BitConverter.ToString(Encoding.Default.GetBytes(srtext)).Replace("-", string.Empty));
        var b = BCHexDec(PUBKEY);
        var c = BCHexDec(MODULUS);
        var key = BigInteger.ModPow(a, b, c).ToString("x");
        key = key.PadLeft(256, '0');

        return key.Length > 256 ? key.Substring(key.Length - 256, 256) : key;
    }

    private static BigInteger BCHexDec(string hex)
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

    private static string AESEncode(string secretData, string secret = "TA3YiYCfY2dDJQgg")
    {
        byte[] encrypted;
        var IV = Encoding.UTF8.GetBytes(VI);

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

    private static string CreateSecretKey(int length)
    {
        const string str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sb = new StringBuilder(length);
        var rnd = new Random();

        for (var i = 0; i < length; ++i)
        {
            sb.Append(str[rnd.Next(0, str.Length)]);
        }

        return sb.ToString();
    }
}
