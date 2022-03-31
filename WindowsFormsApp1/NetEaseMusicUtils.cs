using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace 网易云歌词提取
{
    public static class NetEaseMusicUtils
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        /**
         * 输入参数校验
         *
         * @param input 输入参数
         * @param searchType 查询类型
         */
        public static long CheckInputId(string input, SEARCH_TYPE_ENUM searchType, out string errorMsg)
        {
            // 输入参数为空
            if (string.IsNullOrEmpty(input))
            {
                errorMsg = ErrorMsg.INPUT_ID_ILLEGAL;
                return -1;
            }
            
            // 输入是数字，直接返回
            if (CheckNum(input))
            {
                errorMsg = ErrorMsg.SUCCESS;
                return long.Parse(input);
            }
            
            // ID提取
            var keyword = string.Empty;
            switch (searchType)
            {
                case SEARCH_TYPE_ENUM.SONG_ID:
                    keyword = "song?id=";
                    break;
                case SEARCH_TYPE_ENUM.ALBUM_ID:
                    keyword = "album?id=";
                    break;
            }

            var index = input.IndexOf(keyword, StringComparison.Ordinal);
            if (index != -1)
            {
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

                if (sb.Length != 0)
                {
                    errorMsg = ErrorMsg.SUCCESS;
                    return long.Parse(sb.ToString());
                }
            }
            
            // 不合法类型
            errorMsg = ErrorMsg.INPUT_ID_ILLEGAL;
            return -1;
        }

        /*
         * 检查字符串是否为数字
         */
        private static bool CheckNum(string s)
        {
            return Regex.IsMatch(s, "^\\d+$");
        }

        /**
         * 获取歌词信息
         */
        public static LyricVo GetLyricVo(LyricResult lyricResult, long dt, SearchInfo searchInfo, out string errorMsg)
        {
            var vo = new LyricVo();

            if (searchInfo == null || lyricResult == null)
            {
                errorMsg = ErrorMsg.LRC_NOT_EXIST;
                return vo;
            }

            try
            {                
                if (lyricResult.Code != 200)
                {
                    errorMsg = $"网络错误, 错误码: {lyricResult.Code}";
                    _logger.Error($"获得歌曲信息时失败, 错误码: {lyricResult.Code}");
                    return vo;
                }

                errorMsg = ErrorMsg.SUCCESS;
                string originLyric = string.Empty, originTLyric = string.Empty;
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
                vo.DateTime = dt;
                vo.Output = GetOutputContent(vo, searchInfo);                
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取歌词信息失败");
                errorMsg = ex.Message;
            }

            return vo;
        }

        /**
         * 获取输出结果
         */
        public static string GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            if (lyricVo == null)
            {
                var ex = new ArgumentNullException(nameof(lyricVo));
                _logger.Error(ex);
                throw ex;
            }
            if (searchInfo == null)
            {
                var ex = new ArgumentNullException(nameof(searchInfo));
                _logger.Error(ex);
                throw ex;
            }

            switch (searchInfo.OutputFileFormat)
            {
                case OUTPUT_FORMAT_ENUM.LRC: return GetOutputLyric(lyricVo.Lyric, lyricVo.TLyric, searchInfo);
                case OUTPUT_FORMAT_ENUM.SRT: return ConvertLyricToSrt(
                    GetOutputLyric(lyricVo.Lyric, lyricVo.TLyric, searchInfo), lyricVo.DateTime);
                default: throw new ArgumentException(nameof(searchInfo.OutputFileFormat));
            }
        }

        /**
         * 获取输出歌词
         */
        private static string GetOutputLyric(string originLyric, string originTLyric, SearchInfo searchInfo)
        {
            // 歌词合并
            var formatLyrics = FormatLyric(originLyric, originTLyric, searchInfo);

            // 两位小数
            SetTimeStamp2Dot(ref formatLyrics, searchInfo.DotType);

            var result = new StringBuilder();
            foreach (var i in formatLyrics)
            {
                result.Append(i).Append(Environment.NewLine);
            }
            return result.ToString();
        }

        /**
         * 歌词格式化
         */
        private static string[] FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var showLrcType = searchInfo.ShowLrcType;

            // 如果不存在翻译歌词，或者选择返回原歌词
            var originLrcs = SplitLrc(originLrc);
            if (string.IsNullOrEmpty(translateLrc) || showLrcType == SHOW_LRC_TYPE_ENUM.ONLY_ORIGIN)
            {
                return originLrcs;
            }

            // 如果选择仅译文
            var translateLrcs = SplitLrc(translateLrc);
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
         * 设置时间戳小数位数
         */
        private static void SetTimeStamp2Dot(ref string[] lrcStr, DOT_TYPE_ENUM dotTypeEnum)
        {
            for (int i = 0; i < lrcStr.Length; i++)
            {
                int index = lrcStr[i].IndexOf("]");
                int dot = lrcStr[i].IndexOf(".");
                if (index == -1 || dot == -1)
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

        /**
         * lrc --> srt
         */
        private static string ConvertLyricToSrt(string input, long dt)
        {
            var output = new StringBuilder();

            var startTime = new TimeSpan();
            var baseTime = startTime.Add(new TimeSpan(0, 0, 0, 0, 0));
            var preTime = baseTime;
            var isFirstLine = true;
            var preStr = string.Empty;
            var timeReg = new Regex(@"(?<=^\[)(\d|\:|\.)+(?=])");
            var strReg = new Regex(@"(?<=]).+", RegexOptions.RightToLeft);

            var lines = input.Split(Environment.NewLine.ToCharArray());

            var index = 1;
            
            void AddSrtLine(TimeSpan curTime)
            {
                output
                    .Append(index++)
                    .Append(Environment.NewLine)
                    .Append($"{preTime.Hours:d2}:{preTime.Minutes:d2}:{preTime.Seconds:d2},{preTime.Milliseconds:d3}")
                    .Append(" --> ")
                    .Append($"{curTime.Hours:d2}:{curTime.Minutes:d2}:{curTime.Seconds:d2},{curTime.Milliseconds:d3}")
                    .Append(Environment.NewLine)
                    .Append(preStr)
                    .Append(Environment.NewLine)
                    .Append(Environment.NewLine);
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;

                var match = timeReg.Match(line);
                if (match.Success)
                {
                    if (isFirstLine)
                    {
                        preTime = baseTime.Add(TimeSpan.Parse("00:" + match.Value));
                        isFirstLine = false;
                    }
                    else
                    {
                        if (preStr != string.Empty)
                        {
                            var curTime = baseTime.Add(TimeSpan.Parse("00:" + match.Value));

                            AddSrtLine(curTime);

                            preTime = curTime;
                        }
                    }

                    var strMatch = strReg.Match(line);
                    preStr = strMatch.Success ? strMatch.Value.Trim() : string.Empty;
                }
                else
                {
                    var offsetReg = new Regex(@"(?<=^\[offset:)\d+(?=])");
                    match = offsetReg.Match(line);
                    if (match.Success)
                    {
                        var offset = Convert.ToInt32(match.Value);
                        baseTime = baseTime.Add(new TimeSpan(0, 0, 0, 0, offset));
                    }
                }
            }

            if (preStr != string.Empty)
            {
                AddSrtLine(TimeSpan.FromMilliseconds(dt));
            }

            return output.ToString();
        }

        /**
         * 获取输出文件名
         */
        public static string GetOutputName(SongVo songVo, SearchInfo searchInfo)
        {
            if (songVo == null)
            {
                var ex = new ArgumentNullException(nameof(songVo));
                _logger.Error(ex);
                throw ex;
            }

            switch (searchInfo?.OutputFileNameType ?? throw new ArgumentNullException(nameof(searchInfo)))
            {
                case OUTPUT_FILENAME_TYPE_ENUM.NAME_SINGER:
                    return songVo.Name + " - " + songVo.Singer;
                case OUTPUT_FILENAME_TYPE_ENUM.SINGER_NAME:
                    return songVo.Singer + " - " + songVo.Name;
                case OUTPUT_FILENAME_TYPE_ENUM.NAME:
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
                _logger.Error(ex);
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

        public static string GetSafeFilename(string arbitraryString)
        {
            if (arbitraryString == null)
            {
                var ex = new ArgumentNullException(nameof(arbitraryString));
                _logger.Error(ex);
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
                case OUTPUT_ENCODING_ENUM.UNICODE:
                    return Encoding.Unicode;
                default:
                    // utf-8 and others
                    return new UTF8Encoding(false);
            }
        }
    }
}