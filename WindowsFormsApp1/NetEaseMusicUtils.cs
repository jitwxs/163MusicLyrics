using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace 网易云歌词提取
{
    static class NetEaseMusicUtils
    {
        /**
         * 输入参数校验
         */
        public static long CheckInputId(string input, out string errorMsg)
        {
            long result = 0;

            if (string.IsNullOrEmpty(input) || !CheckNum(input))
            {
                errorMsg = ErrorMsg.INPUT_ID_ILLEGAG;
            }
            else
            {
                errorMsg = ErrorMsg.SUCCESS;
                result = long.Parse(input);
            }

            return result;
        }

        /**
         * 从 DetailResult 获取 Song 对象
         */
        public static Song GetSongFromDetailResult(DetailResult detailResult)
        {
            if (detailResult != null && detailResult.Code == 200)
            {
                var songArray = detailResult.Songs;
                if (songArray != null && songArray.Length > 0)
                {
                    return songArray[0];
                }
            }

            return null;
        }

        /**
         * 获取歌曲基本信息
         */
        public static SongVo GetSongVo(Datum datum, Song song, out string errorMsg)
        {
            var vo = new SongVo();

            if (datum == null)
            {
                errorMsg = ErrorMsg.SONG_NOT_EXIST;
                return vo;
            }

            if (song == null)
            {
                errorMsg = ErrorMsg.SONG_NOT_EXIST;
                return vo;
            }

            vo.Links = datum.Url;
            vo.Name = song.Name;
            vo.Singer = ContractSinger(song.Ar);
            vo.Album = song.Al.Name;

            errorMsg = ErrorMsg.SUCCESS;

            return vo;
        }

        /**
         * 获取歌词信息
         */
        public static LyricVo GetLyricVo(LyricResult lyricResult, SearchInfo searchInfo, out string errorMsg)
        {
            var vo = new LyricVo();

            if (searchInfo == null)
            {
                errorMsg = ErrorMsg.LRC_NOT_EXIST;
                return vo;
            }

            if (lyricResult == null)
            {
                errorMsg = ErrorMsg.LRC_NOT_EXIST;
                return vo;
            }

            try
            {
                if (lyricResult.Code == 200)
                {
                    string originLyric = "", originTLyric = "";
                    if (lyricResult.Lrc != null)
                    {
                        originLyric = lyricResult.Lrc.Lyric;
                    }

                    if (lyricResult.Tlyric != null)
                    {
                        originTLyric = lyricResult.Tlyric.Lyric;
                    }

                    vo.Lyric = originLyric;
                    vo.TLyric = originTLyric;
                    vo.Output = GetOutputLyric(originLyric, originTLyric, searchInfo);
                }

                errorMsg = ErrorMsg.SUCCESS;
            }
            catch (Exception ew)
            {
                errorMsg = ew.Message;
                Console.WriteLine(ew);
            }

            return vo;
        }

        public static string GetOutputLyric(string originLyric, string originTLyric, SearchInfo searchInfo)
        {
            // 歌词合并
            var formatLyrics = FormatLyric(originLyric, originTLyric, searchInfo);

            // 两位小数
            SetTimeStamp2Dot(ref formatLyrics, searchInfo.DotType);

            string result = "";
            foreach (string i in formatLyrics)
            {
                result += i + "\r\n";
            }

            return result;
        }


        /**
         * 获取输出文件名
         */
        public static string GetOutputName(SongVo songVo, SearchInfo searchInfo)
        {
            switch (searchInfo.OutputFileNameType)
            {
                case OUTPUT_FILENAME_TYPE_ENUM.NAME_SINGER:
                    return songVo.Name + " - " + songVo.Singer;
                case OUTPUT_FILENAME_TYPE_ENUM.SINGER_NAME:
                    return songVo.Singer + " - " + songVo.Name;
                case OUTPUT_FILENAME_TYPE_ENUM.NAME:
                    return songVo.Name;
                default:
                    return "";
            }
        }

        /*
         * 检查字符串是否为数字
         */
        private static bool CheckNum(string s)
        {
            return Regex.Match(s, "^\\d+$").Success;
        }

        /**
         * 拼接歌手名
         */
        private static string ContractSinger(List<Ar> arList)
        {
            if (!arList.Any())
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (Ar ar in arList)
            {
                sb.Append(ar.Name).Append(",");
            }

            return sb.Remove(sb.Length - 1, 1).ToString();
        }

        /**
         * 歌词格式化
         */
        private static string[] FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            SHOW_LRC_TYPE_ENUM showLrcType = searchInfo.ShowLrcType;

            // 如果不存在翻译歌词，或者选择返回原歌词
            string[] originLrcs = SplitLrc(originLrc);
            if (string.IsNullOrEmpty(translateLrc) || showLrcType == SHOW_LRC_TYPE_ENUM.ONLY_ORIGIN)
            {
                return originLrcs;
            }

            // 如果选择仅译文
            string[] translateLrcs = SplitLrc(translateLrc);
            if (showLrcType == SHOW_LRC_TYPE_ENUM.ONLY_TRANSLATE)
            {
                return translateLrcs;
            }

            string[] res = null;
            switch (showLrcType)
            {
                case SHOW_LRC_TYPE_ENUM.ORIGIN_PRIOR:
                    res = SortLrc(originLrcs, translateLrcs, true);
                    break;
                case SHOW_LRC_TYPE_ENUM.TRANSLATE_PRIOR:
                    res = SortLrc(originLrcs, translateLrcs, false);
                    break;
                case SHOW_LRC_TYPE_ENUM.MERGE_ORIGIN:
                    res = MergeLrc(originLrcs, translateLrcs, searchInfo.LrcMergeSeparator, true);
                    break;
                case SHOW_LRC_TYPE_ENUM.MERGE_TRANSLATE:
                    res = MergeLrc(originLrcs, translateLrcs, searchInfo.LrcMergeSeparator, false);
                    break;
            }

            return res;
        }

        /**
         * 将歌词切割为数组
         */
        private static string[] SplitLrc(string lrc)
        {
            string[] ss = lrc.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            return ss;
        }

        /**
         * 双语歌词排序
         */
        private static string[] SortLrc(string[] originLrcs, string[] translateLrcs, bool hasOriginLrcPrior)
        {
            int lena = originLrcs.Length;
            int lenb = translateLrcs.Length;
            string[] c = new string[lena + lenb];
            //分别代表数组a ,b , c 的索引
            int i = 0, j = 0, k = 0;

            while (i < lena && j < lenb)
            {
                if (Compare(originLrcs[i], translateLrcs[j], hasOriginLrcPrior) == 1)
                {
                    c[k++] = translateLrcs[j++];
                }
                else if (Compare(originLrcs[i], translateLrcs[j], hasOriginLrcPrior) == -1)
                {
                    c[k++] = originLrcs[i++];
                }
                else
                {
                    c[k++] = hasOriginLrcPrior ? originLrcs[i++] : translateLrcs[j++];
                }
            }

            while (i < lena)
                c[k++] = originLrcs[i++];
            while (j < lenb)
                c[k++] = translateLrcs[j++];
            return c;
        }

        /**
         * 双语歌词合并
         */
        private static string[] MergeLrc(string[] originLrcs, string[] translateLrcs, string splitStr,
            bool hasOriginLrcPrior)
        {
            string[] c = SortLrc(originLrcs, translateLrcs, hasOriginLrcPrior);
            List<string> list = new List<string>
            {
                c[0]
            };

            for (int i = 1; i < c.Length; i++)
            {
                int str1Index = c[i - 1].IndexOf("]") + 1;
                int str2Index = c[i].IndexOf("]") + 1;
                string str1Timestamp = c[i - 1].Substring(0, str1Index);
                string str2Timestamp = c[i].Substring(0, str2Index);
                if (str1Timestamp != str2Timestamp)
                {
                    list.Add(c[i]);
                }
                else
                {
                    int index = list.Count - 1;
                    string subStr1 = list[index];
                    string subStr2 = c[i].Substring(str2Index);

                    // Fix: https://github.com/jitwxs/163MusicLyrics/issues/7
                    if (string.IsNullOrEmpty(subStr1) || string.IsNullOrEmpty(subStr2))
                    {
                        list[index] = subStr1 + subStr2;
                    }
                    else
                    {
                        list[index] = subStr1 + splitStr + subStr2;
                    }
                }
            }

            return list.ToArray();
        }

        /**
         * 歌词排序函数
         */
        private static int Compare(string originLrc, string translateLrc, bool hasOriginLrcPrior)
        {
            int str1Index = originLrc.IndexOf("]");
            string str1Timestamp = originLrc.Substring(0, str1Index + 1);
            int str2Index = translateLrc.IndexOf("]");
            string str2Timestamp = translateLrc.Substring(0, str2Index + 1);

            // Fix: https://github.com/jitwxs/163MusicLyrics/issues/8
            if (string.IsNullOrEmpty(str1Timestamp) || string.IsNullOrEmpty(str2Timestamp))
            {
                return 1;
            }

            if (str1Timestamp != str2Timestamp)
            {
                str1Timestamp = str1Timestamp.Substring(1, str1Timestamp.Length - 2);
                str2Timestamp = str2Timestamp.Substring(1, str2Timestamp.Length - 2);
                string[] t1s = str1Timestamp.Split(':');
                string[] t2s = str2Timestamp.Split(':');
                for (int i = 0; i < t1s.Length; i++)
                {
                    if (double.TryParse(t1s[i], out double t1))
                    {
                        if (double.TryParse(t2s[i], out double t2))
                        {
                            if (t1 > t2)
                                return 1;
                            else if (t1 < t2)
                                return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }

                return 0;
            }
            else
            {
                return hasOriginLrcPrior ? -1 : 1;
            }
        }

        /**
         * 设置时间戳小数位数
         */
        private static void SetTimeStamp2Dot(ref string[] lrcStr, DOT_TYPE_ENUM dotTypeEnum)
        {
            for (int i = 0; i < lrcStr.Length; i++)
            {
                int index = lrcStr[i].IndexOf("]");
                int dot = lrcStr[i].IndexOf(".");
                if (dot == -1)
                {
                    continue;
                }

                string ms = lrcStr[i].Substring(dot + 1, index - dot - 1);
                if (ms.Length == 3)
                {
                    if (dotTypeEnum == DOT_TYPE_ENUM.DOWN)
                    {
                        ms = ms.Substring(0, 2);
                    }
                    else if (dotTypeEnum == DOT_TYPE_ENUM.HALF_UP)
                    {
                        ms = Convert.ToDouble("0." + ms).ToString("0.00").Substring(2);
                    }
                }

                lrcStr[i] = lrcStr[i].Substring(0, dot) + "." + ms + lrcStr[i].Substring(index);
            }
        }

        public static string GetSafeFilename(string arbitraryString)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var replaceIndex = arbitraryString.IndexOfAny(invalidChars, 0);
            if (replaceIndex == -1) return arbitraryString;

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

        public static Encoding GetEncoding(OUTPUT_ENCODING_ENUM encodingEnum)
        {
            switch (encodingEnum)
            {
                case OUTPUT_ENCODING_ENUM.GB_2312:
                    return Encoding.GetEncoding("GB2312");
                case OUTPUT_ENCODING_ENUM.GBK:
                    return Encoding.GetEncoding("GBK");
                case OUTPUT_ENCODING_ENUM.UTF_8_BOM:
                    return new UTF8Encoding(true);
                default:
                    // utf-8 and others
                    return new UTF8Encoding(false);
            }
        }
    }
}