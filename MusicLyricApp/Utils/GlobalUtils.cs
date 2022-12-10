using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NLog;

namespace MusicLyricApp.Utils
{
    public static class GlobalUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static Dictionary<string, string> BatchGetSongKey(IEnumerable<string> displayId, bool verbatimLyric)
        {
            return displayId.ToDictionary(id => id, id => GetSongKey(id, verbatimLyric));
        }
        
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

        /// <summary>
        /// 输入参数校验
        /// </summary>
        /// <param name="input">输入参数</param>
        /// <param name="searchSource">搜索类型</param>
        /// <param name="searchType">查询类型</param>
        /// <returns></returns>
        public static string CheckInputId(string input, SearchSourceEnum searchSource, SearchTypeEnum searchType)
        {
            // 输入参数为空
            if (string.IsNullOrEmpty(input))
            {
                throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
            }

            // 网易云，纯数字，直接通过
            if (searchSource == SearchSourceEnum.NET_EASE_MUSIC && CheckNum(input))
            {
                return input;
            }

            // QQ 音乐，数字+字母，直接通过
            if (searchSource == SearchSourceEnum.QQ_MUSIC && Regex.IsMatch(input, @"^[a-zA-Z0-9]*$"))
            {
                return input;
            }

            // URL 截取
            string urlKeyword;
            switch (searchSource)
            {
                case SearchSourceEnum.NET_EASE_MUSIC:
                    switch (searchType)
                    {
                        case SearchTypeEnum.SONG_ID:
                            urlKeyword = "song?id=";
                            break;
                        case SearchTypeEnum.ALBUM_ID:
                            urlKeyword = "album?id=";
                            break;
                        case SearchTypeEnum.PLAYLIST_ID:
                            urlKeyword = "playlist?id=";
                            break;
                        default:
                            throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
                    }
                    break;
                case SearchSourceEnum.QQ_MUSIC:
                    switch (searchType)
                    {
                        case SearchTypeEnum.SONG_ID:
                            urlKeyword = "songDetail/";
                            break;
                        case SearchTypeEnum.ALBUM_ID:
                            urlKeyword = "albumDetail/";
                            break;
                        case SearchTypeEnum.PLAYLIST_ID:
                            urlKeyword = "playlist/";
                            break;
                        default:
                            throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
                    }
                    break;
                default:
                    throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
            }
            
            var index = input.IndexOf(urlKeyword, StringComparison.Ordinal);
            if (index == -1)
            {
                throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
            }
            
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

            return sb.ToString();
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
        public static string GetOutputName(SongVo songVo, OutputFilenameTypeEnum typeEnum)
        {
            if (songVo == null)
            {
                var ex = new ArgumentNullException(nameof(songVo));
                Logger.Error(ex);
                throw ex;
            }

            string outputName;
            switch (typeEnum)
            {
                case OutputFilenameTypeEnum.NAME_SINGER:
                    outputName = $"{songVo.Name} - {songVo.Singer}";
                    break;
                case OutputFilenameTypeEnum.SINGER_NAME:
                    outputName = $"{songVo.Singer} - {songVo.Name}";
                    break;
                case OutputFilenameTypeEnum.NAME:
                    outputName = songVo.Name;
                    break;
                default:
                    throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }
            return GetSafeFilename(outputName);
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

        public static int toInt(string str, int defaultValue)
        {
            var result = defaultValue;

            int.TryParse(str, out result);

            return result;
        }
        
        public static List<T> GetEnumList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).OfType<T>().ToList();
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
    }
}