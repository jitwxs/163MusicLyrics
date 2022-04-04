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
                return input;
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

        /**
         * 获取输出文件名
         */
        public static string GetOutputName(SongVo songVo, SearchInfo searchInfo)
        {
            if (songVo == null)
            {
                var ex = new ArgumentNullException(nameof(songVo));
                Logger.Error(ex);
                throw ex;
            }

            switch (searchInfo?.OutputFileNameType ?? throw new ArgumentNullException(nameof(searchInfo)))
            {
                case OutputFilenameTypeEnum.NAME_SINGER:
                    return songVo.Name + " - " + songVo.Singer;
                case OutputFilenameTypeEnum.SINGER_NAME:
                    return songVo.Singer + " - " + songVo.Name;
                case OutputFilenameTypeEnum.NAME:
                    return songVo.Name;
                default:
                    return string.Empty;
            }
        }

        /**
         * 拼接歌手名
         */
        public static string ContractSinger(List<Ar> arList)
        {
            if (arList == null)
            {
                var ex = new ArgumentNullException(nameof(arList));
                Logger.Error(ex);
                throw ex;
            }
            if (!arList.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var ar in arList)
            {
                sb.Append(ar.Name).Append(',');
            }

            return sb.Remove(sb.Length - 1, 1).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arbitraryString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetSafeFilename(string arbitraryString)
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
    }
}