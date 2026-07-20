using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using MusicLyricApp.Core.Service;
using MusicLyricApp.Core.Utils;

namespace MusicLyricApp.Core.Service.Music;

public abstract class BaseNativeApi(Func<string> cookieFunc)
{
    public const string Useragent =
        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

    private readonly string _defaultCookie = "NMTID=" + Guid.NewGuid();

    protected abstract string HttpRefer();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="url">链接</param>
    /// <param name="paramDict">参数</param>
    /// <param name="method">模式</param>
    /// <exception cref="WebException"></exception>
    /// <returns></returns>
    protected string SendPost(string url, Dictionary<string, string> paramDict)
    {
        string result;
        using (var wc = new WebClient())
        {
            NetworkClientFactory.ConfigureWebClient(wc);
            var cookie = cookieFunc.Invoke();
            if (string.IsNullOrWhiteSpace(cookie))
            {
                cookie = _defaultCookie;
            }

            wc.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            wc.Headers.Add(HttpRequestHeader.Referer, HttpRefer());
            wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent);
            wc.Headers.Add(HttpRequestHeader.Cookie, cookie);

            var request = new NameValueCollection();
            foreach (var keyPair in @paramDict)
            {
                request.Add(keyPair.Key, keyPair.Value);
            }

            var bytes = wc.UploadValues(url, "POST", request);
            result = Encoding.UTF8.GetString(bytes);
        }

        return result;
    }

    protected string SendGet(string url)
    {
        using var wc = new WebClient();
        NetworkClientFactory.ConfigureWebClient(wc);
        var cookie = cookieFunc.Invoke();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            cookie = _defaultCookie;
        }

        wc.Encoding = Encoding.UTF8;
        wc.Headers.Add(HttpRequestHeader.Referer, HttpRefer());
        wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent);
        wc.Headers.Add(HttpRequestHeader.Cookie, cookie);
        return wc.DownloadString(url);
    }

    protected string SendJsonPost(string url, Dictionary<string, object> paramDict)
    {
        using (var wc = new WebClient())
        {
            NetworkClientFactory.ConfigureWebClient(wc);
            var cookie = cookieFunc.Invoke();
            if (string.IsNullOrWhiteSpace(cookie))
            {
                cookie = _defaultCookie;
            }

            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            wc.Headers.Add(HttpRequestHeader.Referer, HttpRefer());
            wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent);
            wc.Headers.Add(HttpRequestHeader.Cookie, cookie);

            return wc.UploadString(url, paramDict.ToJson());
        }
    }
}
