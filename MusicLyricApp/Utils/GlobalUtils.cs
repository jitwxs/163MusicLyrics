using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace MusicLyricApp.Utils
{
    public static class GlobalUtils
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

        public static readonly Dictionary<SearchSourceEnum, string> SearchSourceKeywordDict =
            new Dictionary<SearchSourceEnum, string>
            {
                { SearchSourceEnum.NET_EASE_MUSIC, "163.com" },
                { SearchSourceEnum.QQ_MUSIC, "qq.com" },
            };

        public static readonly Dictionary<SearchSourceEnum, Dictionary<SearchTypeEnum, string>> SearchTypeKeywordDict =
            new Dictionary<SearchSourceEnum, Dictionary<SearchTypeEnum, string>>
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
        public static SearchInfo.InputSongId CheckInputId(string input, SearchSourceEnum searchSource, SearchTypeEnum searchType)
        {
            // 输入参数为空
            if (string.IsNullOrEmpty(input))
            {
                throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
            }

            // 自动识别音乐提供商
            foreach (var pair in SearchSourceKeywordDict.Where(pair => input.Contains(pair.Value)))
            {
                searchSource = pair.Key;
            }

            // 自动识别搜索类型
            foreach (var pair in SearchTypeKeywordDict[searchSource].Where(pair => input.Contains(pair.Value)))
            {
                searchType = pair.Key;
            }

            // 网易云，纯数字，直接通过
            if (searchSource == SearchSourceEnum.NET_EASE_MUSIC && CheckNum(input))
            {
                return new SearchInfo.InputSongId(input, searchSource, searchType);
            }

            // QQ 音乐，数字+字母，直接通过
            if (searchSource == SearchSourceEnum.QQ_MUSIC && Regex.IsMatch(input, @"^[a-zA-Z0-9]*$"))
            {
                return new SearchInfo.InputSongId(input, searchSource, searchType);
            }

            // URL 关键字提取
            var urlKeyword = SearchTypeKeywordDict[searchSource][searchType];
            var index = input.IndexOf(urlKeyword, StringComparison.Ordinal);
            if (index != -1)
            {
                var sb = new StringBuilder();
                foreach (var c in input.Substring(index + urlKeyword.Length).ToCharArray())
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

                return new SearchInfo.InputSongId(sb.ToString(), searchSource, searchType);
            }

            // QQ 音乐，歌曲短链接
            if (searchSource == SearchSourceEnum.QQ_MUSIC && input.Contains("fcgi-bin/u"))
            {
                const string keyword = "window.__ssrFirstPageData__";
                var html = HttpUtils.HttpGet(input);

                var indexOf = html.IndexOf(keyword);

                if (indexOf != -1)
                {
                    var endIndexOf = html.IndexOf("</script>", indexOf);
                    if (endIndexOf != -1)
                    {
                        var data = html.Substring(indexOf + keyword.Length, endIndexOf - indexOf - keyword.Length);

                        data = data.Trim().Substring(1);

                        var obj = (JObject)JsonConvert.DeserializeObject(data);

                        var songs = obj["songList"].ToObject<QQMusicBean.Song[]>();

                        if (songs.Length > 0)
                        {
                            return new SearchInfo.InputSongId(songs[0].Id, searchSource, searchType);
                        }
                    }
                }
            }

            throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
        }

        /**
         * 检查字符串是否为数字
         */
        public static bool CheckNum(string s)
        {
            return Regex.IsMatch(s, "^\\d+$", RegexOptions.Compiled);
        }

        /**
         * 获取输出文件名
         */
        public static string GetOutputName(SaveVo saveVo, string format, string singerSeparator)
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

            var outputName = format
                .Replace("${index}", saveVo.Index.ToString())
                .Replace("${id}", songVo.DisplayId)
                .Replace("${name}", ControlLength(songVo.Name))
                .Replace("${singer}", ControlLength(string.Join(singerSeparator, songVo.Singer)))
                .Replace("${album}", ControlLength(songVo.Album));

            outputName = ResolveCustomFunction(outputName);

            return GetSafeFilename(outputName);
        }

        public static string ResolveCustomFunction(string content)
        {
            var sourceContent = content;
            
            try
            {
                foreach (Match match in new Regex(@"\$fillLength\([^\)]*\)").Matches(content))
                {
                    var raw = match.Value;

                    var leftQuote = raw.IndexOf("(", StringComparison.Ordinal) + 1;
                    var rightQuote = raw.IndexOf(")", StringComparison.Ordinal);

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

                        res = (diff < keyword.Length ? keyword.Substring(0, diff) : keyword) + res;
                    }

                    content = content.Replace(raw, res);
                }
                
                return content;
            }
            catch (System.Exception e)
            {
                Logger.Error("ResolveCustomFunction error, content: " + sourceContent + ", stack: " + e);
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

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
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
            switch (encodingEnum)
            {
                case OutputEncodingEnum.GB_2312:
                    return Encoding.GetEncoding("GB2312");
                case OutputEncodingEnum.GBK:
                    return Encoding.GetEncoding("GBK");
                case OutputEncodingEnum.UTF_8_BOM:
                    return new UTF8Encoding(true);
                case OutputEncodingEnum.UNICODE:
                    return Encoding.Unicode;
                default:
                    // utf-8 and others
                    return new UTF8Encoding(false);
            }
        }

        public static string GetSuffix(string str)
        {
            var a = str.LastIndexOf("/", StringComparison.Ordinal);
            if (a != -1)
            {
                str = str.Substring(a + 1);
            }

            var b = str.IndexOf("?", StringComparison.Ordinal);
            if (b != -1)
            {
                str = str.Substring(0, b);
            }

            var c = str.LastIndexOf(".", StringComparison.Ordinal);
            if (c == -1)
            {
                return "";
            }
            else
            {
                return str.Substring(c);
            }
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

        public static List<T> GetEnumList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).OfType<T>().ToList();
        }
        
        public static Dictionary<string, T> GetEnumDict<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).OfType<T>().ToDictionary(e => e.ToDescription(), e => e);
        }

        public static string[] GetEnumDescArray<T>() where T : Enum
        {
            var list = GetEnumList<T>();
            var result = new string[list.Count];

            for (var i = 0; i < list.Count; i++)
            {
                result[i] = list[i].ToDescription();
            }

            return result;
        }

        private static string ControlLength(string str)
        {
            if (str.Length > 128)
            {
                return str.Substring(0, 125) + "...";
            }
            else
            {
                return str;
            }
        }
    }
}