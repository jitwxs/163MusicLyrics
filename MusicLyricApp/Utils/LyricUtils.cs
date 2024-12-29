using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using hyjiacan.py4n;
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
        
        private const PinyinFormat PinyinDefineFormat = PinyinFormat.WITH_TONE_MARK  | PinyinFormat.LOWERCASE | PinyinFormat.WITH_U_UNICODE;
        
        /// <summary>
        /// 获取输出结果
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            var param = searchInfo.SettingBean.Param;
            var config = searchInfo.SettingBean.Config;
            
            var dotType = config.DotType;
            var timestampFormat = param.OutputFileFormat == OutputFormatEnum.SRT ? config.SrtTimestampFormat : config.LrcTimestampFormat;
            
            var voListList = await FormatLyric(lyricVo.Lyric, lyricVo.TranslateLyric, searchInfo);

            if (lyricVo.SearchSource == SearchSourceEnum.QQ_MUSIC && config.EnableVerbatimLyric)
            {
                for (var i = 0; i < voListList.Count; i++)
                {
                    voListList[i] = FormatSubLineLyric(voListList[i], timestampFormat, dotType);
                }
            }

            var res = new List<string>();
            
            foreach (var voList in voListList)
            {
                if (param.OutputFileFormat == OutputFormatEnum.SRT)
                {
                    res.Add(SrtUtils.LrcToSrt(voList, timestampFormat, dotType, lyricVo.Duration));
                }
                else
                {
                    res.Add(string.Join(Environment.NewLine,  from o in voList select o.Print(timestampFormat, dotType)));
                }
            }

            return res;
        }

        /// <summary>
        /// 处理逐字歌词格式为常规格式
        /// </summary>
        public static string DealVerbatimLyric(string originLrc, SearchSourceEnum searchSource)
        {
            var defaultParam = new ConfigBean();
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
                        var timeStr = line.Substring(leftParenthesesIndex, parenthesesLength);
                        // 404
                        var timestamp = long.Parse(timeStr.Split(',')[0].Trim().Substring(1));
                        var lyricTimestamp = new LyricTimestamp(timestamp);
                        
                        var content = line.Substring(contentStartIndex, leftParenthesesIndex - contentStartIndex);
                        // 首次执行，去除全行时间戳
                        if (i == 0)
                        {
                            content = new LyricLineVo(content).Content;
                        }
                        
                        contentStartIndex = leftParenthesesIndex + parenthesesLength;

                        sb.Append(lyricTimestamp.PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType)).Append(content);
                        
                        // 最后一次执行，增加行结束时间戳
                        if (i == matches.Count - 1)
                        {
                            // 202
                            var timeCostStr = timeStr.Split(',')[1].Trim();
                            var timeCost = long.Parse(timeCostStr.Substring(0, timeCostStr.Length - 1));

                            sb.Append(lyricTimestamp.Add(timeCost)
                                .PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType));
                        }
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
        private static async Task<List<List<LyricLineVo>>> FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var outputLyricsTypes = searchInfo.SettingBean.Config.DeserializationOutputLyricsTypes();
            var showLrcType = searchInfo.SettingBean.Param.ShowLrcType;
            var searchSource = searchInfo.SettingBean.Param.SearchSource;
            var ignoreEmptyLyric = searchInfo.SettingBean.Config.IgnoreEmptyLyric;

            var res = new List<List<LyricLineVo>>();
            
            // 未配置任何输出
            if (outputLyricsTypes.Count == 0)
            {
                return res;
            }
            
            var originLyrics = SplitLrc(originLrc, searchSource, ignoreEmptyLyric);

            var originLyricsOutputSortInConfig = outputLyricsTypes.IndexOf(LyricsTypeEnum.ORIGIN);
            
            // 仅输出原文            
            if (outputLyricsTypes.Count == 1 && originLyricsOutputSortInConfig != -1)
            {
                res.Add(originLyrics);
                return res;
            }

            // 原始译文歌词的空行没有意义，指定 true 不走配置
            var basicTransLyrics = SplitLrc(translateLrc, searchSource, true);
            
            var lyricsComplexList = await DealTranslateLyric(originLyrics, basicTransLyrics, searchInfo.SettingBean.Config.TransConfig, outputLyricsTypes);

            // 原文歌词插入到结果集的指定位置
            if (originLyricsOutputSortInConfig != -1)
            {
                lyricsComplexList.Insert(originLyricsOutputSortInConfig, originLyrics);
            }

            var single = new List<LyricLineVo>();
            switch (showLrcType)
            {
                case ShowLrcTypeEnum.STAGGER:
                    foreach (var each in lyricsComplexList)
                    {
                        single = SortLrc(single, each, true);
                    }
                    break;
                case ShowLrcTypeEnum.ISOLATED:
                    if (searchInfo.SettingBean.Config.SeparateFileForIsolated)
                    {
                        res.AddRange(lyricsComplexList);
                    }
                    else
                    {
                        foreach (var each in lyricsComplexList)
                        {
                            single.AddRange(each);
                        }
                    }
                    break;
                case ShowLrcTypeEnum.MERGE:
                    foreach (var each in lyricsComplexList)
                    {
                        single = MergeLrc(single, each, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    }
                    break;
                default:
                    throw new NotSupportedException("not support showLrcType: " + showLrcType);
            }

            if (single.Count > 0)
            {
                res.Add(single);
            }

            return res;
        }

        private static string[] SplitLrc(string lrc)
        {
            // 换行符统一
            return (lrc ?? "")
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

            if (c.Count == 0)
            {
                return c;
            }
            
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
        /// <param name="outputLyricsTypes">输出歌词类型列表</param>
        /// <returns></returns>
        public static async Task<List<List<LyricLineVo>>> DealTranslateLyric(List<LyricLineVo> originList, 
            List<LyricLineVo> baseTransList, TransConfigBean transConfig, List<LyricsTypeEnum> outputLyricsTypes)
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
            
            foreach (var transTypeEnum in outputLyricsTypes)
            {
                switch (transTypeEnum)
                {
                    case LyricsTypeEnum.ORIGIN_TRANS:
                        result.Add(transList);
                        break;
                    case LyricsTypeEnum.ROMAJI:
                        if (originLanguage == LanguageEnum.JAPANESE)
                        {
                            result.Add(await RomajiUtils.ToRomaji(originList, transConfig.RomajiModeEnum, transConfig.RomajiSystemEnum));
                        }
                        break;
                    case LyricsTypeEnum.PINYIN:
                        if (originLanguage == LanguageEnum.CHINESE)
                        {
                            result.Add(await ToPinyin(originList, transConfig.LostRule));
                        }
                        break;
                    case LyricsTypeEnum.CHINESE:
                    case LyricsTypeEnum.ENGLISH:
                        // 输出语言和原始歌词语言只有不同时，才翻译
                        if (CastToLyricsTypeEnum(originLanguage) != transTypeEnum)
                        {
                            // 输出语言和已有译文语言相同
                            if (CastToLyricsTypeEnum(baseTransLanguage) == transTypeEnum)
                            {
                                // 仅当已有译文未输出时，才输出
                                if (!outputLyricsTypes.Contains(LyricsTypeEnum.ORIGIN_TRANS))
                                {
                                    result.Add(transList);
                                }
                            }
                            else
                            {
                                var outputLanguage = CastToLanguageEnum(transTypeEnum);

                                // 调用合适的翻译 API
                                foreach (var translateApi in GetAvailableTranslateApi(transConfig))
                                {
                                    string[] inputs = null, outputs = null;
                                    
                                    if (translateApi.IsSupport(originLanguage, outputLanguage))
                                    {
                                        // 使用原文尝试进行翻译
                                        inputs = originList.Select(e => e.Content).ToArray();
                                        outputs = translateApi.Translate(inputs, originLanguage, outputLanguage);
                                    }
                                    else if (transList.Count != 0 && translateApi.IsSupport(baseTransLanguage, outputLanguage))
                                    {
                                        // 使用译文尝试翻译
                                        inputs = transList.Select(e => e.Content).ToArray();
                                        outputs = translateApi.Translate(inputs, baseTransLanguage, outputLanguage);
                                    }

                                    if (inputs != null && outputs != null)
                                    {
                                        var outputList = new List<LyricLineVo>();
                                        for (var i = 0; i < inputs.Length; i++)
                                        {
                                            outputList.Add(new LyricLineVo(outputs[i], originList[i].Timestamp));
                                        }
                                        result.Add(outputList);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            return result;
        }
        
        public static List<ITranslateApi> GetAvailableTranslateApi(TransConfigBean transConfig)
        {
            var res = new List<ITranslateApi>();
            
            try
            {
                res.Add(new CaiYunTranslateApi(transConfig.CaiYunToken));
            }
            catch (MusicLyricException)
            {
                try
                {
                    res.Add(new BaiduTranslateApi(transConfig.BaiduTranslateAppId, transConfig.BaiduTranslateSecret));
                }
                catch (MusicLyricException)
                {
                        
                }
            }

            return res;
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
        
        private static Task<List<LyricLineVo>> ToPinyin(List<LyricLineVo> inputList, TransLyricLostRuleEnum lostRule)
        {
            var resultList = new List<LyricLineVo>();
            
            foreach (var vo in inputList)
            {
                string content;
                
                if (vo.IsIllegalContent())
                {
                    if (lostRule == TransLyricLostRuleEnum.IGNORE)
                    {
                        continue;
                    }
                    else
                    {
                        content = "";
                    }
                }
                else
                {
                    content = Pinyin4Net.GetPinyin(vo.Content, PinyinDefineFormat);
                }
                    
                resultList.Add(new LyricLineVo(content, vo.Timestamp));
            }

            return Task.FromResult(resultList);
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
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()
                       .Single(str => str.EndsWith("Core14.profile.xml"))))
            {
                var factory = new RankedLanguageIdentifierFactory();
                var identifier = factory.Load(stream);
                
                var certainDict = new Dictionary<LanguageEnum, int>();
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
        
        public static LanguageEnum CastToLanguageEnum(LyricsTypeEnum lyricsTypeEnum)
        {
            switch (lyricsTypeEnum)
            {
                case LyricsTypeEnum.CHINESE:
                    return LanguageEnum.CHINESE;
                case LyricsTypeEnum.ENGLISH:
                    return LanguageEnum.ENGLISH;
                default:
                    return LanguageEnum.OTHER;
            }
        }

        private static LyricsTypeEnum CastToLyricsTypeEnum(LanguageEnum languageEnum)
        {
            switch (languageEnum)
            {
                case LanguageEnum.CHINESE:
                    return LyricsTypeEnum.CHINESE;
                case LanguageEnum.ENGLISH:
                    return LyricsTypeEnum.ENGLISH;
                default:
                    return LyricsTypeEnum.ORIGIN_TRANS;
            }
        }
    }
}