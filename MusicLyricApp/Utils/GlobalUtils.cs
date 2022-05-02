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

            if (searchSource == SearchSourceEnum.NET_EASE_MUSIC)
            {
                // 输入是数字，直接返回
                if (CheckNum(input))
                {
                    return input;
                }
                
                // ID 提取
                var keyword = searchType == SearchTypeEnum.SONG_ID ? "song?id=" : "album?id=";
                var index = input.IndexOf(keyword, StringComparison.Ordinal);
                if (index == -1)
                {
                    throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
                }
                
                var sb = new StringBuilder();
                foreach (var c in input.Substring(index + keyword.Length).ToCharArray())
                {
                    if (char.IsNumber(c))
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        break;
                    }
                }

                if (sb.Length == 0 || !CheckNum(sb.ToString())) 
                {
                    throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
                }

                return sb.ToString();
            }

            if (searchSource == SearchSourceEnum.QQ_MUSIC)
            {
                if (input.Contains("/"))
                {
                    // ID 提取
                    var keyword = searchType == SearchTypeEnum.SONG_ID ? "songDetail/" : "albumDetail/";
                    var index = input.IndexOf(keyword, StringComparison.Ordinal);
                    if (index != -1)
                    {
                        var sb = new StringBuilder();
                        foreach (var c in input.Substring(index + keyword.Length).ToCharArray())
                        {
                            if (c == '/')
                            {
                                break;
                            }
                            sb.Append(c);
                        }

                        if (sb.Length > 0)
                        {
                            return sb.ToString();
                        }
                    }
                }
                else
                {
                    return input;
                }
            }

            throw new MusicLyricException(ErrorMsg.INPUT_ID_ILLEGAL);
        }

        /**
         * 检查字符串是否为数字
         */
        private static bool CheckNum(string s)
        {
            return Regex.IsMatch(s, "^\\d+$");
        }

        public static long TimestampStrToLong(string timestamp)
        {
            // 不支持的格式
            if (string.IsNullOrWhiteSpace(timestamp) || timestamp[0] != '[' || timestamp[timestamp.Length - 1] != ']')
            {
                return -1;
            }

            timestamp = timestamp.Substring(1, timestamp.Length - 2);

            var split = timestamp.Split(':');

            var min = toInt(split[0], 0);

            split = split[1].Split('.');

            var second = toInt(split[0], 0);

            var ms = 0;

            if (split.Length > 1)
            {
                ms = toInt(split[1], 0);
            }

            return (min * 60 + second) * 1000 + ms;
        }

        public static string TimestampLongToStr(long timestamp, string msScale)
        {
            if (timestamp < 0)
            {
                throw new MusicLyricException(ErrorMsg.SYSTEM_ERROR);
            }
            
            var ms = timestamp % 1000;

            timestamp /= 1000;

            var seconds = timestamp % 60;

            var min = timestamp / 60;

            return "[" + min.ToString("00") + ":" + seconds.ToString("00") + "." + ms.ToString(msScale) + "]";
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
                    outputName = songVo.Name + " - " + songVo.Singer;
                    break;
                case OutputFilenameTypeEnum.SINGER_NAME:
                    outputName = songVo.Singer + " - " + songVo.Name;
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