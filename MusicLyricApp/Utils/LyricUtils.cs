using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Utils
{
    /// <summary>
    /// 歌词处理基类
    /// </summary>
    public abstract class LyricUtils
    {
        /// <summary>
        /// 获取输出结果
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        public static async Task<string> GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            var output = await GenerateOutput(lyricVo.Lyric, lyricVo.TranslateLyric, searchInfo);

            if (searchInfo.SettingBean.Param.OutputFileFormat == OutputFormatEnum.SRT)
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
        private static async Task<string> GenerateOutput(string originLyric, string originTLyric, SearchInfo searchInfo)
        {
            // 歌词合并
            var formatLyrics = await FormatLyric(originLyric, originTLyric, searchInfo);

            // 两位小数
            SetTimeStamp2Dot(ref formatLyrics, searchInfo.SettingBean.Param.DotType);

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
        private static async Task<string[]> FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var showLrcType = searchInfo.SettingBean.Param.ShowLrcType;
            var searchSource = searchInfo.SettingBean.Param.SearchSource;

            var originLyrics = SplitLrc(originLrc, searchSource);
            if (originLrc.Length == 0)
            {
                return Array.Empty<string>();
            }

            // 如果不存在翻译歌词，或者选择返回原歌词
            if (string.IsNullOrEmpty(translateLrc) || showLrcType == ShowLrcTypeEnum.ONLY_ORIGIN)
            {
                return originLyrics;
            }
            
            // 译文处理，启用罗马音进行转换，否则使用原始的译文
            string[] translateLyrics;
            if (searchInfo.SettingBean.Config.RomajiConfig.Enable)
            {
                translateLyrics = await RomajiUtils.ToRomaji(originLyrics, searchInfo.SettingBean.Config.RomajiConfig);
            }
            else
            {
                translateLyrics = SplitLrc(translateLrc, searchSource);
            }

            if (translateLyrics.Length == 0)
            {
                return Array.Empty<string>();
            }

            // 如果选择仅译文
            if (showLrcType == ShowLrcTypeEnum.ONLY_TRANSLATE)
            {
                return translateLyrics;
            }

            string[] res = null;
            switch (showLrcType)
            {
                case ShowLrcTypeEnum.ORIGIN_PRIOR:
                    res = SortLrc(originLyrics, translateLyrics, true);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR:
                    res = SortLrc(originLyrics, translateLyrics, false);
                    break;
                case ShowLrcTypeEnum.MERGE_ORIGIN:
                    res = MergeLrc(originLyrics, translateLyrics, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    break;
                case ShowLrcTypeEnum.MERGE_TRANSLATE:
                    res = MergeLrc(originLyrics, translateLyrics, searchInfo.SettingBean.Param.LrcMergeSeparator, false);
                    break;
            }
            return res;
        }
        
        /**
         * 将歌词切割为数组
         */
        private static string[] SplitLrc(string lrc, SearchSourceEnum searchSource)
        {
            // 换行符统一
            var temp = lrc 
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');

            var resultList = new List<string>();
            
            foreach (var line in temp)
            {
                // 跳过空行
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                // QQ 音乐歌词正式开始标识符
                if (searchSource == SearchSourceEnum.QQ_MUSIC && "[offset:0]".Equals(line))
                {
                    resultList.Clear();
                    continue;
                }
                
                resultList.Add(line);
            }
            
            return resultList.ToArray();
        }

        /**
         * 双语歌词排序
         */
        private static string[] SortLrc(string[] originLyrics, string[] translateLyrics, bool hasOriginLrcPrior)
        {
            int lenA = originLyrics.Length, lenB = translateLyrics.Length;
            var c = new string[lenA + lenB];
            
            //分别代表数组a ,b , c 的索引
            int i = 0, j = 0, k = 0;

            while (i < lenA && j < lenB)
            {
                if (Compare(originLyrics[i], translateLyrics[j], hasOriginLrcPrior) == 1)
                {
                    c[k++] = translateLyrics[j++];
                }
                else if (Compare(originLyrics[i], translateLyrics[j], hasOriginLrcPrior) == -1)
                {
                    c[k++] = originLyrics[i++];
                }
                else
                {
                    c[k++] = hasOriginLrcPrior ? originLyrics[i++] : translateLyrics[j++];
                }
            }

            while (i < lenA)
                c[k++] = originLyrics[i++];
            while (j < lenB)
                c[k++] = translateLyrics[j++];
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