using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using MusicLyricApp.Core.Utils;

namespace MusicLyricApp.Core.Service.Music;

public abstract class BaseNativeApi(Func<string> cookieFunc, Func<HttpClient>? httpClientFactory = null)
{
    public const string Useragent =
        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

    private readonly string _defaultCookie = "NMTID=" + Guid.NewGuid();

    protected abstract string HttpRefer();

    protected string SendPost(string url, Dictionary<string, string> paramDict)
    {
        using var content = new FormUrlEncodedContent(paramDict);
        return Send(url, content);
    }

    protected virtual string SendJsonPost(string url, Dictionary<string, object> paramDict)
    {
        using var content = new StringContent(paramDict.ToJson(), Encoding.UTF8, "application/json");
        return Send(url, content);
    }

    private string Send(string url, HttpContent content)
    {
        using var client = httpClientFactory?.Invoke() ?? NetworkClientFactory.CreateHttpClient();
        var cookie = cookieFunc.Invoke();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie",
            string.IsNullOrWhiteSpace(cookie) ? _defaultCookie : cookie);
        client.DefaultRequestHeaders.Referrer = new Uri(HttpRefer());
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Useragent);
        using var response = client.PostAsync(url, content).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
