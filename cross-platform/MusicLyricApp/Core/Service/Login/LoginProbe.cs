using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MusicLyricApp.Core.Service.Music;
using MusicLyricApp.Models;
using Newtonsoft.Json.Linq;

namespace MusicLyricApp.Core.Service.Login;

/// <summary>What is known about one platform's sign-in cookie.</summary>
public enum LoginState
{
    /// <summary>No cookie: requests go out as a guest.</summary>
    NotConfigured,
    /// <summary>A cookie is filled in but lacks the sign-in token, so it cannot work.</summary>
    Malformed,
    Checking,
    Valid,
    /// <summary>The platform answered that the cookie is not signed in (wrong or expired).</summary>
    Invalid,
    /// <summary>The check itself failed (network, proxy, unexpected answer); the cookie may still work.</summary>
    Unknown
}

public sealed record LoginProbeResult(LoginState State, string Nickname = "", string Detail = "");

public interface ILoginProbe
{
    /// <summary>Asks the platform whether <paramref name="cookie"/> is signed in. Blocking; call off the UI thread.</summary>
    LoginProbeResult Probe(SearchSourceEnum source, string cookie);
}

/// <summary>Local checks and clean-up of pasted cookies, without any network request.</summary>
public static class CookieInspector
{
    private static readonly string[] QqUinKeys = ["uin", "wxuin", "qqmusic_uin"];
    private static readonly string[] QqTokenKeys = ["qm_keyst", "qqmusic_key"];

    /// <summary>
    /// Trims what usually comes along when a cookie is copied from the browser: surrounding quotes and spaces,
    /// a leading "Cookie:" header name and line breaks.
    /// </summary>
    public static string Normalize(string? cookie)
    {
        if (string.IsNullOrWhiteSpace(cookie)) return "";
        var value = cookie.Trim().Trim('"', '\'').Trim();
        if (value.StartsWith("cookie:", StringComparison.OrdinalIgnoreCase)) value = value[7..].Trim();
        value = Regex.Replace(value, @"\s*[\r\n]+\s*", " ");
        return value.Trim();
    }

    public static Dictionary<string, string> Parse(string? cookie)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in Normalize(cookie).Split(';'))
        {
            var index = part.IndexOf('=');
            if (index <= 0) continue;
            var key = part[..index].Trim();
            var value = part[(index + 1)..].Trim();
            if (key.Length > 0 && value.Length > 0) result[key] = value;
        }
        return result;
    }

    /// <summary>True when the cookie carries the token that platform uses to recognise a signed-in user.</summary>
    public static bool HasLoginToken(SearchSourceEnum source, string? cookie)
    {
        var values = Parse(cookie);
        return source == SearchSourceEnum.NET_EASE_MUSIC
            ? values.ContainsKey("MUSIC_U")
            : QqUin(cookie).Length > 0 && QqTokenKeys.Any(values.ContainsKey);
    }

    /// <summary>The QQ number from the cookie, without the "o" prefix and leading zeros the web client adds.</summary>
    public static string QqUin(string? cookie)
    {
        var values = Parse(cookie);
        foreach (var key in QqUinKeys)
        {
            if (!values.TryGetValue(key, out var raw)) continue;
            var digits = raw.TrimStart('o', 'O').TrimStart('0');
            if (digits.Length > 0 && digits.All(char.IsDigit)) return digits;
        }
        return "";
    }
}

/// <summary>Checks cookies against the platforms' own account endpoints.</summary>
public sealed class NativeLoginProbe : ILoginProbe
{
    public LoginProbeResult Probe(SearchSourceEnum source, string cookie)
    {
        cookie = CookieInspector.Normalize(cookie);
        if (cookie.Length == 0) return new(LoginState.NotConfigured);
        if (!CookieInspector.HasLoginToken(source, cookie)) return new(LoginState.Malformed);
        try
        {
            return source == SearchSourceEnum.NET_EASE_MUSIC
                ? ParseNetEase(new NetEaseMusicNativeApi(() => cookie).GetLoginAccount())
                : ParseQq(new QQMusicNativeApi(() => cookie).GetLoginProfile(CookieInspector.QqUin(cookie)));
        }
        catch (Exception ex)
        {
            return new(LoginState.Unknown, Detail: ex.Message);
        }
    }

    /// <summary><c>{"code":200,"profile":{"nickname":...}}</c> when signed in; <c>profile</c> is null otherwise.</summary>
    public static LoginProbeResult ParseNetEase(string json)
    {
        var obj = JObject.Parse(json);
        if (obj.Value<int?>("code") != 200) return new(LoginState.Unknown, Detail: obj.Value<string>("message") ?? json);
        return obj["profile"] is JObject profile
            ? new(LoginState.Valid, profile.Value<string>("nickname") ?? "")
            : new(LoginState.Invalid);
    }

    /// <summary><c>{"code":0,"data":{"creator":{"nick":...}}}</c> when signed in; code 1000 means not signed in.</summary>
    public static LoginProbeResult ParseQq(string json)
    {
        var obj = JObject.Parse(json);
        return obj.Value<int?>("code") switch
        {
            0 => new(LoginState.Valid, obj.SelectToken("data.creator.nick")?.ToString() ?? ""),
            1000 => new(LoginState.Invalid),
            var code => new(LoginState.Unknown, Detail: $"code {code}")
        };
    }
}
