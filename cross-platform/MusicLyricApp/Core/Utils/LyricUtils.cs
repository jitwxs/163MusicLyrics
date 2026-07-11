using hyjiacan.py4n;
using MusicLyricApp.Core.Service.Translate;
using MusicLyricApp.Models;
using NTextCat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToolGood.Words;

namespace MusicLyricApp.Core.Utils;

/// <summary>
/// 歌词处理基类
/// </summary>
public static partial class LyricUtils
{
    [GeneratedRegex(LyricLineVo.TimestampPattern)]
    public static partial Regex GetCommonLegalPrefixRegex();

    private const PinyinFormat PinyinDefineFormat =
        PinyinFormat.WITH_TONE_MARK | PinyinFormat.LOWERCASE | PinyinFormat.WITH_U_UNICODE;

    /// <summary>
    /// 获取输出结果
    /// </summary>
    public static async Task<List<string>> GetOutputContent(LyricVo lyricVo, SettingBean settingBean)
    {
        PersistParamBean param = settingBean.Param;
        ConfigBean config = settingBean.Config;

        DotTypeEnum dotType = config.DotType;
        string timestampFormat = param.OutputFileFormat == OutputFormatEnum.SRT
            ? config.SrtTimestampFormat
            : config.LrcTimestampFormat;

        List<List<LyricLineVo>> voListList = await FormatLyric(lyricVo, settingBean);

        if (config.VerbatimLyricMode != VerbatimLyricModeEnum.DISABLE)
        {
            for (int i = 0; i < voListList.Count; i++)
            {
                voListList[i] = VerbatimLyricUtils.FormatSubLineLyric(voListList[i], timestampFormat, dotType);
            }
        }

        List<string> res = new List<string>();

        foreach (List<LyricLineVo> voList in voListList)
        {
            string line;
            if (param.OutputFileFormat == OutputFormatEnum.SRT)
            {
                line = SrtUtils.LrcToSrt(voList, timestampFormat, dotType, lyricVo.Duration);
            }
            else
            {
                line = string.Join(Environment.NewLine,
                    from o in voList
                    let printed = o.Print(timestampFormat, dotType)
                    select config.VerbatimLyricMode == VerbatimLyricModeEnum.A2_MODE
                        ? VerbatimLyricUtils.ConvertVerbatimLyricFromBasicToA2Mode(printed)
                        : printed
                );
            }

            line = config.ChineseProcessRule switch
            {
                ChineseProcessRuleEnum.SIMPLIFIED_CHINESE => WordsHelper.ToSimplifiedChinese(line),
                ChineseProcessRuleEnum.TRADITIONAL_CHINESE => WordsHelper.ToTraditionalChinese(line),
                _ => line
            };

            res.Add(line);
        }

        return res;
    }

    /// <summary>
    /// 歌词格式化
    /// </summary>
    /// <param name="lyricVo">歌词 Vo</param>
    /// <param name="settingBean">处理参数</param>
    /// <returns></returns>
    private static async Task<List<List<LyricLineVo>>> FormatLyric(LyricVo lyricVo, SettingBean settingBean)
    {
        List<LyricsTypeEnum> outputLyricsTypes = settingBean.Config.DeserializationOutputLyricsTypes();
        ShowLrcTypeEnum showLrcType = settingBean.Param.ShowLrcType;
        SearchSourceEnum searchSource = lyricVo.SearchSource;
        bool ignoreEmptyLyric = settingBean.Config.IgnoreEmptyLyric;

        List<List<LyricLineVo>> res = new List<List<LyricLineVo>>();

        // 1. 未配置任何输出
        if (outputLyricsTypes.Count == 0)
        {
            return res;
        }

        List<LyricLineVo> originLyrics = SplitLrc(lyricVo.Lyric, searchSource, ignoreEmptyLyric);

        // 2. 仅输出原文  
        int originLyricsOutputSortInConfig = outputLyricsTypes.IndexOf(LyricsTypeEnum.ORIGIN);
        if (outputLyricsTypes.Count == 1 && originLyricsOutputSortInConfig != -1)
        {
            res.Add(originLyrics);
            return res;
        }

        // 3. 处理其他译文
        List<List<LyricLineVo>> lyricsComplexList = await DealTranslateLyric(originLyrics, lyricVo, settingBean.Config.TransConfig,
            searchSource, outputLyricsTypes);

        // 原文歌词插入到结果集的指定位置
        if (originLyricsOutputSortInConfig != -1)
        {
            lyricsComplexList.Insert(originLyricsOutputSortInConfig, originLyrics);
        }

        List<LyricLineVo> single = new List<LyricLineVo>();
        switch (showLrcType)
        {
            case ShowLrcTypeEnum.STAGGER:
                foreach (List<LyricLineVo> each in lyricsComplexList)
                {
                    single = SortLrc(single, each, true);
                }

                break;
            case ShowLrcTypeEnum.ISOLATED:
                if (settingBean.Config.SeparateFileForIsolated)
                {
                    res.AddRange(lyricsComplexList);
                }
                else
                {
                    foreach (List<LyricLineVo> each in lyricsComplexList)
                    {
                        single.AddRange(each);
                    }
                }

                break;
            case ShowLrcTypeEnum.MERGE:
                single = MergeLrc(originLyrics, lyricsComplexList, settingBean.Param.LrcMergeSeparator);
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

    public static string[] SplitLrc(string lrc)
    {
        return (lrc ?? "")
            .Replace("\r\n", "\n")
            .Replace("\r", "")
            .Split('\n')
            .Select(line => line.Trim())               // 去除行首尾空白
            .Where(line => !string.IsNullOrEmpty(line)) // 过滤空行
            .ToArray();
    }

    /**
     * 切割歌词
     */
    private static List<LyricLineVo> SplitLrc(string lrc, SearchSourceEnum searchSource, bool ignoreEmptyLine)
    {
        string[] temp = SplitLrc(lrc);

        List<LyricLineVo> resultList = new List<LyricLineVo>();

        foreach (string line in temp)
        {
            // QQ 音乐歌词正式开始标识符
            if (searchSource == SearchSourceEnum.QQ_MUSIC)
            {
                if ("[offset:0]".Equals(line) || line.StartsWith("[kana:"))
                {
                    resultList.Clear();
                    continue;
                }
            }

            LyricLineVo lyricLineVo = new LyricLineVo(line);

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
        List<LyricLineVo> c = new List<LyricLineVo>();

        int i = 0, j = 0;

        while (i < lenA && j < lenB)
        {
            int compare = Compare(listA[i], listB[j], aFirst);

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
    private static List<LyricLineVo> MergeLrc(List<LyricLineVo> baseLyric, List<List<LyricLineVo>> mergingLyrics, string splitText)
    {
        // handle empty base
        if (baseLyric == null || baseLyric.Count == 0)
        {
            return [];
        }

        // initialize buckets for each base line
        List<List<LyricLineVo>> buckets = new(baseLyric.Count);
        for (int i = 0; i < baseLyric.Count; i++)
        {
            buckets.Add([]);
        }

        // distribute each line from mergingLyrics into the closest baseLyric index
        if (mergingLyrics != null)
        {
            foreach (List<LyricLineVo> oneLyrics in mergingLyrics)
            {
                if (oneLyrics == null) continue;

                foreach (LyricLineVo line in oneLyrics)
                {
                    // find closest index in baseLyric by timestamp
                    long ts = line.Timestamp.TimeOffset;
                    int bestIndex = 0;
                    long bestDiff = Math.Abs(baseLyric[0].Timestamp.TimeOffset - ts);

                    for (int i = 1; i < baseLyric.Count; i++)
                    {
                        long diff = Math.Abs(baseLyric[i].Timestamp.TimeOffset - ts);
                        if (diff < bestDiff)
                        {
                            bestDiff = diff;
                            bestIndex = i;
                        }
                    }

                    buckets[bestIndex].Add(line);
                }
            }
        }

        // merge buckets into resulting list, using baseLyric timestamps
        List<LyricLineVo> result = new(baseLyric.Count);

        for (int i = 0; i < buckets.Count; i++)
        {
            List<LyricLineVo> bucket = buckets[i];

            List<string> parts = [];
            foreach (LyricLineVo item in bucket)
            {
                parts.Add(item.Content);
            }

            string mergedContent;
            if (parts.Count == 0)
            {
                continue;
            }

            mergedContent = string.Join(splitText ?? string.Empty, parts);
            result.Add(new LyricLineVo(mergedContent, baseLyric[i].Timestamp));
        }

        return result;
    }

    /// <summary>
    /// 译文逻辑处理: 译文精度误差, 译文缺省规则, 译文类型填充
    /// </summary>
    /// <param name="originList">原文歌词</param>
    /// <param name="lyricVo">歌词 Vo</param>
    /// <param name="transConfig">译文配置</param>
    /// <param name="searchSource">搜索来源</param>
    /// <param name="outputLyricsTypes">输出歌词类型列表</param>
    /// <returns></returns>
    private static async Task<List<List<LyricLineVo>>> DealTranslateLyric(List<LyricLineVo> originList, LyricVo lyricVo,
        TransConfigBean transConfig, SearchSourceEnum searchSource, List<LyricsTypeEnum> outputLyricsTypes)
    {
        List<List<LyricLineVo>> result = new List<List<LyricLineVo>>();

        // 不存在原文歌词
        if (originList.Count == 0)
        {
            return result;
        }

        // 1. 初始化原始译文
        // 1.1 原始译文歌词的空行没有意义，指定 true 不走配置
        List<LyricLineVo> baseTransList = SplitLrc(lyricVo.TranslateLyric, searchSource, true);
        // 1.2 处理译文精度误差, 译文缺省规则
        List<LyricLineVo> transList = ResolveTransLyricDigitDeviationAndLost(originList, baseTransList,
            transConfig.MatchPrecisionDeviation, transConfig.LostRule);

        // 推断原文歌词和原始译文歌词，对应语言
        LanguageEnum originLanguage = CertainLanguage(originList), baseTransLanguage = CertainLanguage(transList);

        // 2. 处理其他输出类型
        foreach (LyricsTypeEnum transTypeEnum in outputLyricsTypes)
        {
            switch (transTypeEnum)
            {
                case LyricsTypeEnum.ORIGIN_TRANS:
                    result.Add(transList);
                    break;
                case LyricsTypeEnum.TRANSLITERATION:
                    List<LyricLineVo> baseTransliterationList = SplitLrc(lyricVo.TransliterationLyric, searchSource, true);
                    List<LyricLineVo> transliterationList = ResolveTransLyricDigitDeviationAndLost(originList,
                        baseTransliterationList, transConfig.MatchPrecisionDeviation, transConfig.LostRule);
                    result.Add(transliterationList);
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
                            LanguageEnum outputLanguage = CastToLanguageEnum(transTypeEnum);

                            // 调用合适的翻译 API
                            foreach (ITranslateApi translateApi in GetAvailableTranslateApi(transConfig))
                            {
                                string[]? inputs = null, outputs = null;

                                if (translateApi.IsSupport(originLanguage, outputLanguage))
                                {
                                    // 使用原文尝试进行翻译
                                    inputs = originList.Select(e => e.Content).ToArray();
                                    outputs = translateApi.Translate(inputs, originLanguage, outputLanguage);
                                }
                                else if (transList.Count != 0 &&
                                         translateApi.IsSupport(baseTransLanguage, outputLanguage))
                                {
                                    // 使用译文尝试翻译
                                    inputs = transList.Select(e => e.Content).ToArray();
                                    outputs = translateApi.Translate(inputs, baseTransLanguage, outputLanguage);
                                }

                                if (inputs != null && outputs != null)
                                {
                                    List<LyricLineVo> outputList = new List<LyricLineVo>();
                                    for (int i = 0; i < inputs.Length; i++)
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
        List<ITranslateApi> res = new List<ITranslateApi>();

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
    private static List<LyricLineVo> ResolveTransLyricDigitDeviationAndLost(List<LyricLineVo> originList,
        List<LyricLineVo> baseTransList,
        int precisionDigitDeviation, TransLyricLostRuleEnum lostRule)
    {
        Dictionary<long, LyricLineVo> originTimeOffsetDict = new Dictionary<long, LyricLineVo>();
        foreach (LyricLineVo one in originList)
        {
            originTimeOffsetDict[one.Timestamp.TimeOffset] = one;
        }

        Dictionary<int, LyricLineVo> notMatchTranslateDict = new Dictionary<int, LyricLineVo>();

        // 误差 == 0
        for (int i = 0; i < baseTransList.Count; i++)
        {
            LyricLineVo translate = baseTransList[i];
            long timestamp = translate.Timestamp.TimeOffset;

            if (!originTimeOffsetDict.Remove(timestamp))
            {
                notMatchTranslateDict.Add(i, translate);
            }
        }

        if (precisionDigitDeviation != 0)
        {
            foreach (KeyValuePair<int, LyricLineVo> pair in notMatchTranslateDict)
            {
                int index = pair.Key;
                LyricLineVo translate = pair.Value;
                long timestamp = translate.Timestamp.TimeOffset;

                long tsStart = Math.Max(index == 0 ? 0 : baseTransList[index - 1].Timestamp.TimeOffset + 1,
                    timestamp - precisionDigitDeviation);

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

                for (long ts = tsStart; ts <= tsEnd; ts++)
                {
                    if (originTimeOffsetDict.Remove(ts))
                    {
                        // 将译文时间调整为误差后的译文
                        LyricLineVo newTranslate = new LyricLineVo(translate.Content, new LyricTimestamp(ts));

                        baseTransList[pair.Key] = newTranslate;
                    }
                }
            }
        }

        // 处理译文缺失规则
        if (lostRule != TransLyricLostRuleEnum.IGNORE)
        {
            foreach (KeyValuePair<long, LyricLineVo> pair in originTimeOffsetDict)
            {
                string content = lostRule == TransLyricLostRuleEnum.FILL_ORIGIN ? pair.Value.Content : "";

                baseTransList.Add(new LyricLineVo(content, pair.Value.Timestamp));
            }
        }

        List<LyricLineVo> transList = new List<LyricLineVo>(baseTransList);
        transList.Sort();

        return transList;
    }

    private static Task<List<LyricLineVo>> ToPinyin(List<LyricLineVo> inputList, TransLyricLostRuleEnum lostRule)
    {
        List<LyricLineVo> resultList = new List<LyricLineVo>();

        foreach (LyricLineVo vo in inputList)
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
        int compareTo = originLrc.CompareTo(translateLrc);

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
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("Core14.profile.xml")));
        RankedLanguageIdentifierFactory factory = new RankedLanguageIdentifierFactory();
        RankedLanguageIdentifier identifier = factory.Load(stream);

        Dictionary<LanguageEnum, int> certainDict = new Dictionary<LanguageEnum, int>();
        foreach (LyricLineVo one in lineVos)
        {
            IEnumerable<Tuple<LanguageInfo, double>> languages = identifier.Identify(one.Content);

            Tuple<LanguageInfo, double>? tuple = languages?.First();
            if (tuple == null)
            {
                continue;
            }

            LanguageEnum languageEnum = CastLanguage(tuple.Item1.Iso639_3);

            if (!certainDict.TryAdd(languageEnum, 1))
            {
                certainDict[languageEnum]++;
            }
        }

        return certainDict.Count == 0 ? LanguageEnum.OTHER : certainDict.MaxBy(pair => pair.Value).Key;
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