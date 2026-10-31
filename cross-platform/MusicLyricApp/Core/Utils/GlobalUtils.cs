using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Models;
using NLog;

namespace MusicLyricApp.Core.Utils;

public static partial class GlobalUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string GetSongKey(string displayId, bool verbatimLyric)
    {
        return displayId + "_" + verbatimLyric;
    }

    public static string FormatDate(long millisecond)
    {
        var date = (new DateTime(1970, 1, 1))
                .AddMilliseconds(double.Parse(millisecond.ToString()))
                .AddHours(8) // +8 时区
            ;

        return date.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static readonly Dictionary<SearchSourceEnum, string> SearchSourceKeywordDict = new()
    {
        { SearchSourceEnum.NET_EASE_MUSIC, "163.com" },
        { SearchSourceEnum.QQ_MUSIC, "qq.com" },
    };

    public static readonly Dictionary<SearchSourceEnum, Dictionary<SearchTypeEnum, string>> SearchTypeKeywordDict =
        new()
        {
            {
                SearchSourceEnum.NET_EASE_MUSIC, new Dictionary<SearchTypeEnum, string>
                {
                    { SearchTypeEnum.SONG_ID, "song?id=" },
                    { SearchTypeEnum.ALBUM_ID, "album?id=" },
                    { SearchTypeEnum.PLAYLIST_ID, "playlist?id=" },
                }
            },
            {
                SearchSourceEnum.QQ_MUSIC, new Dictionary<SearchTypeEnum, string>
                {
                    { SearchTypeEnum.SONG_ID, "songDetail/" },
                    { SearchTypeEnum.ALBUM_ID, "albumDetail/" },
                    { SearchTypeEnum.PLAYLIST_ID, "playlist/" },
                }
            }
        };

    /// <summary>
    /// 输入参数校验
    /// </summary>
    /// <param name="input">输入参数</param>
    /// <param name="searchSource"></param>
    /// <param name="searchType"></param>
    /// <returns></returns>
    /// <exception cref="MusicLyricException"></exception>
    public static InputSongId CheckInputId(string input, SearchSourceEnum searchSource,
        SearchTypeEnum searchType)
    {
        // 输入参数为空
        if (string.IsNullOrEmpty(input))
        {
            throw new MusicLyricException(ErrorMsgConst.INPUT_ID_ILLEGAL);
        }

        // 自动识别音乐提供商
        foreach (var pair in SearchSourceKeywordDict.Where(pair => input.Contains(pair.Value)))
        {
            searchSource = pair.Key;
        }

        input = ConvertSearchWithShareLink(searchSource, input);

        // 自动识别搜索类型
        foreach (var pair in SearchTypeKeywordDict[searchSource].Where(pair => input.Contains(pair.Value)))
        {
            searchType = pair.Key;
        }

        // 网易云，纯数字，直接通过
        if (searchSource == SearchSourceEnum.NET_EASE_MUSIC && CheckNum(input))
        {
            return new InputSongId(input, searchSource, searchType);
        }

        // QQ 音乐，数字+字母，直接通过
        if (searchSource == SearchSourceEnum.QQ_MUSIC && LettersAndNumbersRegex().IsMatch(input))
        {
            return new InputSongId(input, searchSource, searchType);
        }

        // URL 关键字提取
        var urlKeyword = SearchTypeKeywordDict[searchSource][searchType];
        var index = input.IndexOf(urlKeyword, StringComparison.Ordinal);
        if (index != -1)
        {
            var sb = new StringBuilder();
            foreach (var c in input[(index + urlKeyword.Length)..])
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    break;
                }
            }

            var extractedId = sb.ToString();
            if (string.IsNullOrEmpty(extractedId) ||
                (searchSource == SearchSourceEnum.NET_EASE_MUSIC && !CheckNum(extractedId)))
            {
                throw new MusicLyricException(ErrorMsgConst.INPUT_ID_ILLEGAL);
            }

            return new InputSongId(extractedId, searchSource, searchType);
        }

        // QQ 音乐，歌曲短链接
        if (searchSource == SearchSourceEnum.QQ_MUSIC && input.Contains("fcgi-bin/u"))
        {
            var response = HttpUtils.HttpGet0(input);
            if (response is { IsSuccessStatusCode: true, RequestMessage: not null } && response.RequestMessage.RequestUri != null)
            {
                var redirectUrl = response.RequestMessage.RequestUri.AbsoluteUri;
                return CheckInputId(redirectUrl, searchSource, searchType);
            }
        }
        
        Logger.Warn("GlobalUtils#CheckInputId INPUT_ID_ILLEGAL, input: " + input);

        throw new MusicLyricException(ErrorMsgConst.INPUT_ID_ILLEGAL);
    }

    public static string ConvertSearchWithShareLink(SearchSourceEnum searchSource, string input)
    {
        // 只处理QQ音乐
        if (searchSource == SearchSourceEnum.QQ_MUSIC)
        {
            // 处理歌曲ID链接
            var songIdMatch = Regex.Match(input, @"playsong\.html\?songid=([^&]*)(&.*)?$");
            if (songIdMatch.Success)
            {
                var songId = songIdMatch.Groups[1].Value;
                var replacement = SearchTypeKeywordDict[searchSource][SearchTypeEnum.SONG_ID];
                return Regex.Replace(input, @"playsong\.html\?songid=[^&]*(&.*)?$", replacement + songId);
            }

            // 处理专辑ID链接 (albummid格式)
            var albumIdMatch1 = Regex.Match(input, @"album\.html\?albummid=([^&]*)(&.*)?$");
            if (albumIdMatch1.Success)
            {
                var albumId = albumIdMatch1.Groups[1].Value;
                var replacement = SearchTypeKeywordDict[searchSource][SearchTypeEnum.ALBUM_ID];
                return Regex.Replace(input, @"album\.html\?albummid=[^&]*(&.*)?$", replacement + albumId);
            }
            
            // 处理专辑ID链接 (albumId格式)
            var albumIdMatch2 = Regex.Match(input, @"album\.html\?(.*&)?albumId=([^&]*)(&.*)?$");
            if (albumIdMatch2.Success)
            {
                var albumId = albumIdMatch2.Groups[2].Value;
                var replacement = SearchTypeKeywordDict[searchSource][SearchTypeEnum.ALBUM_ID];
                // 构建替换部分，保留?后albumId前的参数，然后替换为albumDetail页面
                return Regex.Replace(input, @"album\.html\?.*albumId=[^&]*(&.*)?$", replacement + albumId + "$1");
            }

            // 处理播放列表ID链接
            var playlistIdMatch = Regex.Match(input, @"taoge\.html\?id=([^&]*)(&.*)?$");
            if (playlistIdMatch.Success)
            {
                var playlistId = playlistIdMatch.Groups[1].Value;
                var replacement = SearchTypeKeywordDict[searchSource][SearchTypeEnum.PLAYLIST_ID];
                return Regex.Replace(input, @"taoge\.html\?id=[^&]*(&.*)?$", replacement + playlistId);
            }
        }

        return input;
    }

    /**
     * 检查字符串是否为数字
     */
    public static bool CheckNum(string s)
    {
        return NumberRegex().IsMatch(s);
    }

    /**
     * 获取输出文件名
     */
    public static string GetOutputName(SaveVo saveVo, string format, string singerSeparator, int singerCountLimit = -1)
    {
        if (saveVo == null)
        {
            throw new MusicLyricException("GetOutputName but saveVo is null");
        }

        var songVo = saveVo.SongVo;

        if (songVo == null)
        {
            throw new MusicLyricException("GetOutputName but songVo is null");
        }

        var singers = songVo.Singer;
        if (singerCountLimit > 0 && singers != null && singers.Length > singerCountLimit)
        {
            singers = singers.Take(singerCountLimit).ToArray();
        }

        var outputName = format
            .Replace("${index}", saveVo.Index.ToString())
            .Replace("${id}", songVo.DisplayId)
            .Replace("${name}", ControlLength(songVo.Name))
            .Replace("${singer}", ControlLength(string.Join(singerSeparator, singers)))
            .Replace("${album}", ControlLength(songVo.Album));

        outputName = ResolveCustomFunction(outputName);

        return GetSafeFilename(outputName);
    }

    private static string ResolveCustomFunction(string content)
    {
        var sourceContent = content;

        try
        {
            foreach (Match match in FillLengthRegex().Matches(content))
            {
                var raw = match.Value;

                var leftQuote = raw.IndexOf('(') + 1;
                var rightQuote = raw.IndexOf(')');

                var split = raw.Substring(leftQuote, rightQuote - leftQuote).Split(',');
                // 三个参数
                if (split.Length != 3)
                {
                    continue;
                }

                string res = split[0], keyword = split[1];

                // 重复长度
                if (!int.TryParse(split[2], out var targetLength))
                {
                    continue;
                }

                while (res.Length < targetLength)
                {
                    var diff = targetLength - res.Length;

                    res = (diff < keyword.Length ? keyword[..diff] : keyword) + res;
                }

                content = content.Replace(raw, res);
            }

            return content;
        }
        catch (Exception e)
        {
            Logger.Error(e, "ResolveCustomFunction error, content: " + sourceContent);
            return sourceContent;
        }
    }

    private static string GetSafeFilename(string arbitraryString)
    {
        if (arbitraryString == null)
        {
            var ex = new ArgumentNullException(nameof(arbitraryString));
            Logger.Error(ex);
            throw ex;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var replaceIndex = arbitraryString.IndexOfAny(invalidChars, 0);
        if (replaceIndex == -1)
            return arbitraryString;

        var r = new StringBuilder();
        var i = 0;

        do
        {
            r.Append(arbitraryString, i, replaceIndex - i);

            switch (arbitraryString[replaceIndex])
            {
                case '"':
                    r.Append("''");
                    break;
                case '<':
                    r.Append('\u02c2'); // '˂' (modifier letter left arrowhead)
                    break;
                case '>':
                    r.Append('\u02c3'); // '˃' (modifier letter right arrowhead)
                    break;
                case '|':
                    r.Append('\u2223'); // '∣' (divides)
                    break;
                case ':':
                    r.Append('-');
                    break;
                case '*':
                    r.Append('\u2217'); // '∗' (asterisk operator)
                    break;
                case '\\':
                case '/':
                    r.Append('\u2044'); // '⁄' (fraction slash)
                    break;
                case '\0':
                case '\f':
                case '?':
                    break;
                case '\t':
                case '\n':
                case '\r':
                case '\v':
                    r.Append(' ');
                    break;
                default:
                    r.Append('_');
                    break;
            }

            i = replaceIndex + 1;
            replaceIndex = arbitraryString.IndexOfAny(invalidChars, i);
        } while (replaceIndex != -1);

        r.Append(arbitraryString, i, arbitraryString.Length - i);

        return r.ToString();
    }

    public static Encoding GetEncoding(OutputEncodingEnum encodingEnum)
    {
        return encodingEnum switch
        {
            OutputEncodingEnum.UTF_32 => Encoding.UTF32,
            OutputEncodingEnum.UTF_8_BOM => new UTF8Encoding(true),
            OutputEncodingEnum.UNICODE => Encoding.Unicode,
            _ => new UTF8Encoding(false)
        };
    }

    public static int ToInt(string str, int defaultValue)
    {
        return int.TryParse(str, out var result) ? result : defaultValue;
    }

    public static string GetOrDefault(string v, string defaultValue)
    {
        return string.IsNullOrEmpty(v) ? defaultValue : v;
    }

    public static string MergeStr(IEnumerable<string> strList)
    {
        return string.Join(Environment.NewLine, strList);
    }
    
    /// <summary>
    /// 将序列拆分成指定大小的批次。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">要拆分的源序列</param>
    /// <param name="size">每批的大小（必须 > 0）</param>
    /// <returns>批次列表，每批为一个 List&lt;T&gt;</returns>
    public static IEnumerable<List<T>> Batch<T>(IEnumerable<T> source, int size)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Batch size must be greater than 0.");

        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == size)
            {
                yield return batch;
                batch = new List<T>(size);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    private static string ControlLength(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        
        if (str.Length > 128)
        {
            return str[..125] + "...";
        }
        else
        {
            return str;
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]*$")]
    private static partial Regex LettersAndNumbersRegex();
    
    [GeneratedRegex("^\\d+$", RegexOptions.Compiled)]
    private static partial Regex NumberRegex();
    
    [GeneratedRegex(@"\$fillLength\([^\)]*\)")]
    private static partial Regex FillLengthRegex();
}
