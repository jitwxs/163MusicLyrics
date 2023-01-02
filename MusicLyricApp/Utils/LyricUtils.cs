using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MusicLyricApp.Api.Translate;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NTextCat;

namespace MusicLyricApp.Utils
{
    /// <summary>
    /// 歌词处理基类
    /// </summary>
    public abstract class LyricUtils
    {
        private static readonly Regex VerbatimRegex = new Regex(@"\(\d+,\d+\)");
        
        /// <summary>
        /// 获取输出结果
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        public static async Task<string> GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            var param = searchInfo.SettingBean.Param;
            
            var dotType = param.DotType;
            var timestampFormat = param.OutputFileFormat == OutputFormatEnum.SRT ? param.SrtTimestampFormat : param.LrcTimestampFormat;
            
            var voList = await FormatLyric(lyricVo.Lyric, lyricVo.TranslateLyric, searchInfo);

            if (searchInfo.SettingBean.Param.EnableVerbatimLyric)
            {
                voList = FormatSubLineLyric(voList, timestampFormat, dotType);
            }
            
            if (param.OutputFileFormat == OutputFormatEnum.SRT)
            {
                return SrtUtils.LrcToSrt(voList, timestampFormat, dotType, lyricVo.Duration);
            }
            else
            {
                return string.Join(Environment.NewLine,  from o in voList select o.Print(timestampFormat, dotType));
            }
        }

        /// <summary>
        /// 处理逐字歌词格式为常规格式
        /// </summary>
        public static string DealVerbatimLyric(string originLrc, SearchSourceEnum searchSource)
        {
            var defaultParam = new PersistParamBean();
            var sb = new StringBuilder();
            
            foreach (var line in SplitLrc(originLrc))
            {
                // skip illegal verbatim line, eg: https://y.qq.com/n/ryqq/songDetail/000sNzbP2nHGs2
                if (!line.EndsWith(")"))
                {
                    continue;
                }
                
                var matches = VerbatimRegex.Matches(line);
                if (matches.Count > 0)
                {
                    int contentStartIndex = 0, i = 0;

                    do
                    {
                        var curMatch = matches[i];
                        var group = curMatch.Groups[0];
                        int leftParenthesesIndex = group.Index, parenthesesLength = group.Length;

                        // (404,202)
                        var timestamp = line.Substring(leftParenthesesIndex, parenthesesLength);
                        // 404
                        timestamp = timestamp.Split(',')[0].Trim().Substring(1);
                        // format
                        timestamp = new LyricTimestamp(long.Parse(timestamp))
                            .PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType);
                        
                        var content = line.Substring(contentStartIndex, leftParenthesesIndex - contentStartIndex);
                        // 去除全行时间戳
                        if (i == 0)
                        {
                            content = new LyricLineVo(content).Content;
                        }
                        
                        contentStartIndex = leftParenthesesIndex + parenthesesLength;

                        sb.Append(timestamp).Append(content);
                    } while (++i < matches.Count);
                }
                else
                {
                    sb.Append(line);
                }

                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// need try split sub lyricLineVO, resolve verbatim lyric mode
        /// </summary>
        /// <returns></returns>
        private static List<LyricLineVo> FormatSubLineLyric(List<LyricLineVo> vos, string timestampFormat, DotTypeEnum dotType)
        {
            var res = new List<LyricLineVo>();
            foreach (var vo in vos)
            {
                var sb = new StringBuilder();
                foreach (var subVo in LyricLineVo.Split(vo))
                {
                    sb.Append(subVo.Timestamp.PrintTimestamp(timestampFormat, dotType) + subVo.Content);
                }
                
                res.Add(new LyricLineVo(sb.ToString()));
            }
            return res;
        }
        
        /// <summary>
        /// 歌词格式化
        /// </summary>
        /// <param name="originLrc">原始的歌词内容</param>
        /// <param name="translateLrc">原始的译文内容</param>
        /// <param name="searchInfo">处理参数</param>
        /// <returns></returns>
        private static async Task<List<LyricLineVo>> FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var showLrcType = searchInfo.SettingBean.Param.ShowLrcType;
            var searchSource = searchInfo.SettingBean.Param.SearchSource;
            var ignoreEmptyLyric = searchInfo.SettingBean.Param.IgnoreEmptyLyric;

            var originLyrics = SplitLrc(originLrc, searchSource, ignoreEmptyLyric);
            if (showLrcType == ShowLrcTypeEnum.ONLY_ORIGIN)
            {
                return originLyrics;
            }

            // 原始译文歌词的空行没有意义，指定 true 不走配置
            var basicTransLyrics = SplitLrc(translateLrc, searchSource, true);
            var transLyricsList = await DealTranslateLyric(originLyrics, basicTransLyrics, searchInfo.SettingBean.Config.TransConfig);

            var res = new List<LyricLineVo>();

            switch (showLrcType)
            {
                case ShowLrcTypeEnum.ONLY_TRANS_STAGGER:
                    foreach (var each in transLyricsList)
                    {
                        res = SortLrc(res, each, true);
                    }
                    break;
                case ShowLrcTypeEnum.ONLY_TRANS_ISOLATED:
                    foreach (var each in transLyricsList)
                    {
                        res.AddRange(each);
                    }
                    break;
                case ShowLrcTypeEnum.ONLY_TRANS_MERGE:
                    foreach (var each in transLyricsList)
                    {
                        res = MergeLrc(res, each, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    }
                    break;
                case ShowLrcTypeEnum.ORIGIN_PRIOR_STAGGER:
                    res.AddRange(originLyrics);
                    for (var i = 0; i < transLyricsList.Count; i++)
                    {
                        res = SortLrc(res, transLyricsList[i], true);
                    }
                    break;
                case ShowLrcTypeEnum.ORIGIN_PRIOR_ISOLATED:
                    res.AddRange(originLyrics);
                    foreach (var each in transLyricsList)
                    {
                        res.AddRange(each);
                    }
                    break;
                case ShowLrcTypeEnum.ORIGIN_PRIOR_MERGE:
                    res.AddRange(originLyrics);
                    foreach (var each in transLyricsList)
                    {
                        res = MergeLrc(res, each, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    }
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_STAGGER:
                    foreach (var each in transLyricsList)
                    {
                        res = SortLrc(res, each, true);
                    }
                    res = SortLrc(res, originLyrics, true);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_ISOLATED:
                    foreach (var each in transLyricsList)
                    {
                        res.AddRange(each);
                    }
                    res.AddRange(originLyrics);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_MERGE:
                    foreach (var each in transLyricsList)
                    {
                        res = MergeLrc(res, each, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    }
                    res = MergeLrc(res, originLyrics, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    break;
                default:
                    throw new NotSupportedException("not support showLrcType: " + showLrcType);
            }

            return res;
        }

        private static string[] SplitLrc(string lrc)
        {
            // 换行符统一
            return lrc
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');
        }

        /**
         * 切割歌词
         */
        private static List<LyricLineVo> SplitLrc(string lrc, SearchSourceEnum searchSource, bool ignoreEmptyLine)
        {

            var temp = SplitLrc(lrc);

            var resultList = new List<LyricLineVo>();

            foreach (var line in temp)
            {
                // QQ 音乐歌词正式开始标识符
                if (searchSource == SearchSourceEnum.QQ_MUSIC && "[offset:0]".Equals(line))
                {
                    resultList.Clear();
                    continue;
                }

                var lyricLineVo = new LyricLineVo(line);
                
                // 无效内容处理
                if (lyricLineVo.IsIllegalContent())
                {
                    if (ignoreEmptyLine)
                    {
                        continue;
                    }

                    // 重置空行内容
                    lyricLineVo.Content = string.Empty;
                }

                resultList.Add(lyricLineVo);
            }

            return resultList;
        }


        /// <summary>
        /// 歌词排序
        /// </summary>
        private static List<LyricLineVo> SortLrc(List<LyricLineVo> listA, List<LyricLineVo> listB, bool aFirst)
        {
            int lenA = listA.Count, lenB = listB.Count;
            var c = new List<LyricLineVo>();

            int i = 0, j = 0;

            while (i < lenA && j < lenB)
            {
                var compare = Compare(listA[i], listB[j], aFirst);
                
                if (compare > 0)
                {
                    c.Add(listB[j++]);
                }
                else if (compare < 0)
                {
                    c.Add(listA[i++]);
                }
                else
                {
                    c.Add(aFirst ? listA[i++] : listB[j++]);
                }
            }

            while (i < lenA)
                c.Add(listA[i++]);
            while (j < lenB)
                c.Add(listB[j++]);
            return c;
        }

        /// <summary>
        /// 歌词合并
        /// </summary>
        private static List<LyricLineVo> MergeLrc(List<LyricLineVo> listA, List<LyricLineVo> listB, string splitStr, bool aFirst)
        {
            var c = SortLrc(listA, listB, aFirst);
            
            var list = new List<LyricLineVo>
            {
                c[0]
            };

            for (var i = 1; i < c.Count; i++)
            {
                if (c[i - 1].Timestamp.TimeOffset == c[i].Timestamp.TimeOffset)
                {
                    var index = list.Count - 1;

                    list[index].Content = list[index].Content + splitStr + c[i].Content;
                }
                else
                {
                    list.Add(c[i]);
                }
            }

            return list;
        }

        /// <summary>
        /// 译文逻辑处理: 译文精度误差, 译文缺省规则, 译文类型填充
        /// </summary>
        /// <param name="originList">原文歌词</param>
        /// <param name="baseTransList">初始的译文歌词</param>
        /// <param name="transConfig">译文配置</param>
        /// <returns></returns>
        public static async Task<List<List<LyricLineVo>>> DealTranslateLyric(List<LyricLineVo> originList, List<LyricLineVo> baseTransList, TransConfigBean transConfig)
        {
            var result = new List<List<LyricLineVo>>();
            
            // 不存在原文歌词
            if (originList == null || originList.Count == 0)
            {
                return result;
            }

            // 处理译文精度误差, 译文缺省规则
            var transList = ResolveTransLyricDigitDeviationAndLost(originList, baseTransList, transConfig.MatchPrecisionDeviation, transConfig.LostRule);

            // 处理译文类型填充
            LanguageEnum originLanguage = CertainLanguage(originList), baseTransLanguage = CertainLanguage(transList);
            
            var configTransTypes = transConfig.TransType.Split(',')
                .Select(e => (TransTypeEnum) Convert.ToInt32(e)).ToHashSet();
            
            foreach (var transTypeEnum in configTransTypes)
            {
                switch (transTypeEnum)
                {
                    case TransTypeEnum.ORIGIN_TRANS:
                        result.Add(transList);
                        break;
                    case TransTypeEnum.ROMAJI:
                        if (originLanguage == LanguageEnum.JAPANESE)
                        {
                            result.Add(await RomajiUtils.ToRomaji(originList, transConfig.RomajiModeEnum, transConfig.RomajiSystemEnum));
                        }
                        break;
                    case TransTypeEnum.CHINESE:
                    case TransTypeEnum.ENGLISH:
                        // 输出语言和原始歌词语言只有不同时，才翻译
                        if (CastTransType(originLanguage) != transTypeEnum)
                        {
                            // 输出语言和已有译文语言相同
                            if (CastTransType(baseTransLanguage) == transTypeEnum)
                            {
                                // 仅当已有译文未输出时，才输出
                                if (!configTransTypes.Contains(TransTypeEnum.ORIGIN_TRANS))
                                {
                                    result.Add(transList);
                                }
                            }
                            else
                            {
                                // 调用翻译 API
                                var translateApi = GetTranslateApi(transConfig);
                                if (translateApi != null)
                                {
                                    var inputs = transList.Select(e => e.Content).ToArray();
                                    var outputs = translateApi.Translate(inputs, baseTransLanguage, CastLanguageEnum(transTypeEnum));

                                    var outputList = new List<LyricLineVo>();
                                    for (var i = 0; i < inputs.Length; i++)
                                    {
                                        outputList.Add(new LyricLineVo(outputs[i], transList[i].Timestamp));
                                    }

                                    result.Add(outputList);
                                }
                            }
                        }
                        break;
                }
            }

            return result;
        }
        
        public static ITranslateApi GetTranslateApi(TransConfigBean transConfig)
        {
            try
            {
                return new CaiYunTranslateApi(transConfig.CaiYunToken);
            }
            catch (MusicLyricException caiYunEx)
            {
                if (ErrorMsg.CAIYUN_TRANSLATE_AUTH_FAILED.Equals(caiYunEx.Message))
                {
                    try
                    {
                        return new BaiduTranslateApi(transConfig.BaiduTranslateAppId, transConfig.BaiduTranslateSecret);
                    }
                    catch (MusicLyricException baiduEx)
                    {
                        
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 解决译文歌词的精度误差和丢失问题
        /// </summary>
        /// <param name="originList">原文歌词</param>
        /// <param name="baseTransList">初始译文歌词</param>
        /// <param name="precisionDigitDeviation">译文匹配精度误差</param>
        /// <param name="lostRule">译文缺失规则</param>
        /// <returns></returns>
        private static List<LyricLineVo> ResolveTransLyricDigitDeviationAndLost(List<LyricLineVo> originList, List<LyricLineVo> baseTransList, 
            int precisionDigitDeviation, TransLyricLostRuleEnum lostRule)
        {
            var originTimeOffsetDict = new Dictionary<long, LyricLineVo>();
            foreach (var one in originList)
            {
                originTimeOffsetDict[one.Timestamp.TimeOffset] = one;
            }

            var notMatchTranslateDict = new Dictionary<int, LyricLineVo>();
            
            // 误差 == 0
            for (var i = 0; i < baseTransList.Count; i++)
            {
                var translate = baseTransList[i];
                var timestamp = translate.Timestamp.TimeOffset;
                
                if (!originTimeOffsetDict.Remove(timestamp))
                {
                    notMatchTranslateDict.Add(i, translate);
                }
            }
            
            if (precisionDigitDeviation != 0)
            {
                foreach (var pair in notMatchTranslateDict)
                {
                    var index = pair.Key;
                    var translate = pair.Value;
                    var timestamp = translate.Timestamp.TimeOffset;

                    var tsStart = Math.Max(index == 0 ? 0 : baseTransList[index - 1].Timestamp.TimeOffset + 1, timestamp - precisionDigitDeviation);
                    
                    long tsEnd;
                    if (index == baseTransList.Count - 1)
                    {
                        tsEnd = Math.Max(timestamp, originList[originList.Count - 1].Timestamp.TimeOffset);
                    }
                    else
                    {
                        tsEnd = baseTransList[index + 1].Timestamp.TimeOffset - 1;
                    }
                    tsEnd = Math.Min(tsEnd, timestamp + precisionDigitDeviation);
                    
                    for (var ts = tsStart; ts <= tsEnd; ts++)
                    {
                        if (originTimeOffsetDict.Remove(ts))
                        {
                            // 将译文时间调整为误差后的译文
                            var newTranslate = new LyricLineVo(translate.Content, new LyricTimestamp(ts));

                            baseTransList[pair.Key] = newTranslate;
                        }
                    }
                }
            }

            // 处理译文缺失规则
            if (lostRule != TransLyricLostRuleEnum.IGNORE)
            {
                foreach (var pair in originTimeOffsetDict)
                {
                    var content = lostRule == TransLyricLostRuleEnum.FILL_ORIGIN ? pair.Value.Content : "";

                    baseTransList.Add(new LyricLineVo(content, pair.Value.Timestamp));
                }
            }

            var transList = new List<LyricLineVo>(baseTransList);
            transList.Sort();

            return transList;
        }
        
        /**
         * 歌词排序函数
         */
        private static int Compare(LyricLineVo originLrc, LyricLineVo translateLrc, bool hasOriginLrcPrior)
        {
            var compareTo = originLrc.CompareTo(translateLrc);

            if (compareTo == 0)
            {
                return hasOriginLrcPrior ? -1 : 1;
            }

            return compareTo;
        }

        /// <summary>
        /// 推断歌词语言
        /// </summary>
        private static LanguageEnum CertainLanguage(List<LyricLineVo> lineVos)
        {
            var certainDict = new Dictionary<LanguageEnum, int>();
            
            var factory = new RankedLanguageIdentifierFactory();
            var identifier = factory.Load("Core14.profile.xml");
            
            foreach (var one in lineVos)
            {
                var languages = identifier.Identify(one.Content);

                var tuple = languages?.First();
                if (tuple == null)
                { 
                    continue;
                }
            
                var languageEnum = CastLanguage(tuple.Item1.Iso639_3);

                if (certainDict.ContainsKey(languageEnum))
                {
                    certainDict[languageEnum]++;
                }
                else
                {
                    certainDict[languageEnum] = 1;
                }
            }
            
            var result = LanguageEnum.OTHER;
            var resultCount = 0;

            foreach (var pair in certainDict)
            {
                if (pair.Value > resultCount)
                {
                    result = pair.Key;
                    resultCount = pair.Value;
                }
            }

            return result;
        }
        
        private static LanguageEnum CastLanguage(string str)
        {
            switch (str.ToUpper())
            {
                case "FRA":
                    return LanguageEnum.FRENCH;
                case "KOR":
                    return LanguageEnum.KOREAN;
                case "ZHO":
                    return LanguageEnum.CHINESE;
                case "ENG":
                    return LanguageEnum.ENGLISH;
                case "ITA":
                    return LanguageEnum.ITALIAN;
                case "RUS":
                    return LanguageEnum.RUSSIAN;
                case "JPN":
                    return LanguageEnum.JAPANESE;
                default:
                    return LanguageEnum.OTHER;
            }
        }
        
        public static LanguageEnum CastLanguageEnum(TransTypeEnum transTypeEnum)
        {
            switch (transTypeEnum)
            {
                case TransTypeEnum.CHINESE:
                    return LanguageEnum.CHINESE;
                case TransTypeEnum.ENGLISH:
                    return LanguageEnum.ENGLISH;
                default:
                    return LanguageEnum.OTHER;
            }
        }

        private static TransTypeEnum CastTransType(LanguageEnum languageEnum)
        {
            switch (languageEnum)
            {
                case LanguageEnum.CHINESE:
                    return TransTypeEnum.CHINESE;
                case LanguageEnum.ENGLISH:
                    return TransTypeEnum.ENGLISH;
                default:
                    return TransTypeEnum.ORIGIN_TRANS;
            }
        }
    }
}