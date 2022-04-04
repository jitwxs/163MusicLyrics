using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Application.Bean;

namespace Application.Utils
{
    /// <summary>
    /// 歌词处理基类
    /// </summary>
    public abstract class LyricUtils
    {
        /// <summary>
        /// 填充歌词信息属性
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="songVo"></param>
        /// <param name="searchInfo"></param>
        public static void FillingLyricVo(LyricVo lyricVo, SongVo songVo, SearchInfo searchInfo)
        {
            lyricVo.Duration = songVo.Duration;
            lyricVo.Output = GetOutputContent(lyricVo, searchInfo);
        }
        
        /// <summary>
        /// 获取输出结果
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        public static string GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            var output = GenerateOutput(lyricVo.Lyric, lyricVo.TranslateLyric, searchInfo);

            if (searchInfo.OutputFileFormat == OutputFormatEnum.SRT)
            {
                output = SrtUtils.LrcToSrt(output, lyricVo.Duration);
            }

            return output;
        }

        /// <summary>
        /// 生成输出结果
        /// </summary>
        /// <param name="originLyric">原始的歌词内容</param>
        /// <param name="originTLyric">原始的译文内容</param>
        /// <param name="searchInfo">处理参数</param>
        /// <returns></returns>
        private static string GenerateOutput(string originLyric, string originTLyric, SearchInfo searchInfo)
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

        /// <summary>
        /// 歌词格式化
        /// </summary>
        /// <param name="originLrc">原始的歌词内容</param>
        /// <param name="translateLrc">原始的译文内容</param>
        /// <param name="searchInfo">处理参数</param>
        /// <returns></returns>
        private static string[] FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var showLrcType = searchInfo.ShowLrcType;

            // 如果不存在翻译歌词，或者选择返回原歌词
            var originLrcs = SplitLrc(originLrc);
            if (string.IsNullOrEmpty(translateLrc) || showLrcType == ShowLrcTypeEnum.ONLY_ORIGIN)
            {
                return originLrcs;
            }

            // 如果选择仅译文
            var translateLrcs = SplitLrc(translateLrc);
            if (showLrcType == ShowLrcTypeEnum.ONLY_TRANSLATE)
            {
                return translateLrcs;
            }

            string[] res = null;
            switch (showLrcType)
            {
                case ShowLrcTypeEnum.ORIGIN_PRIOR:
                    res = SortLrc(originLrcs, translateLrcs, true);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR:
                    res = SortLrc(originLrcs, translateLrcs, false);
                    break;
                case ShowLrcTypeEnum.MERGE_ORIGIN:
                    res = MergeLrc(originLrcs, translateLrcs, searchInfo.LrcMergeSeparator, true);
                    break;
                case ShowLrcTypeEnum.MERGE_TRANSLATE:
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
        private static string[] MergeLrc(string[] originLrcs, string[] translateLrcs, string splitStr, bool hasOriginLrcPrior)
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

                    // Fix: https://github.com/jitwxs/Application/issues/7
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

            // Fix: https://github.com/jitwxs/Application/issues/8
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
        private static void SetTimeStamp2Dot(ref string[] lrcStr, DotTypeEnum dotTypeEnum)
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
                    if (dotTypeEnum == DotTypeEnum.DOWN)
                    {
                        ms = ms.Substring(0, 2);
                    }
                    else if (dotTypeEnum == DotTypeEnum.HALF_UP)
                    {
                        ms = Convert.ToDouble("0." + ms).ToString("0.00").Substring(2);
                    }
                }
                lrcStr[i] = lrcStr[i].Substring(0, dot) + "." + ms + lrcStr[i].Substring(index);
            }
        }
    }
}