using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Utils;

public static partial class VerbatimLyricUtils
{
    [GeneratedRegex(@"\[\d+,\d+\]")]
    public static partial Regex GetVerbatimLegalPrefixRegex();

    [GeneratedRegex(@"\(\d+,\d+,\d+\)")]
    private static partial Regex GetVerbatimRegex4NetEaseMusicRegex();

    [GeneratedRegex(@"\(\d+,\d+\)")]
    private static partial Regex GetVerbatimRegex4QqMusicRegex();
    
    /// <summary>
    /// need try split sub lyricLineVO, resolve verbatim lyric mode
    /// </summary>
    /// <returns></returns>
    public static List<LyricLineVo> FormatSubLineLyric(List<LyricLineVo> vos, string timestampFormat,
        DotTypeEnum dotType)
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
    
        public static string ConvertVerbatimLyricToCommonLyric4QQMusic(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var defaultParam = new ConfigBean();
        var sb = new StringBuilder();

        foreach (var line in LyricUtils.SplitLrc(input))
        {
            var matches = GetVerbatimLegalPrefixRegex().Matches(line);
            if (matches.Count > 0)
            {
                var group = matches[0].Groups[0];
                int leftParenthesesIndex = group.Index, parenthesesLength = group.Length;

                // [70,80]
                var timeStr = line.Substring(leftParenthesesIndex, parenthesesLength);
                var timeParts = timeStr.Trim('[', ']').Split(',');
                var timestamp = long.Parse(timeParts[0]);
                var duration = long.Parse(timeParts[1]);
                var lyricTimestamp = new LyricTimestamp(timestamp);

                var content = GetVerbatimRegex4QqMusicRegex().Replace(line[parenthesesLength..], string.Empty);

                sb.Append(lyricTimestamp.PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType))
                    .Append(content)
                    .Append(Environment.NewLine);

                // Keep a line-clear cue so IgnoreEmptyLyric can decide whether to retain it.
                if (duration > 0 && !string.IsNullOrWhiteSpace(content))
                {
                    sb.Append(lyricTimestamp.Add(duration)
                        .PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType));
                }
            }
            else
            {
                sb.Append(line);
            }

            sb.Append(Environment.NewLine);
        }

        return sb.ToString();
    }

    public static string DealVerbatimLyric4QQMusic(string originLrc)
    {
        if (string.IsNullOrWhiteSpace(originLrc))
        {
            return string.Empty;
        }

        var defaultParam = new ConfigBean();
        var sb = new StringBuilder();

        foreach (var line in LyricUtils.SplitLrc(originLrc))
        {
            // skip illegal verbatim line, eg: https://y.qq.com/n/ryqq/songDetail/000sNzbP2nHGs2
            if (!line.EndsWith(")"))
            {
                continue;
            }

            var matches = GetVerbatimRegex4QqMusicRegex().Matches(line);
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
                    var timestamp = long.Parse(timeStr.Split(',')[0].Trim()[1..]);
                    var lyricTimestamp = new LyricTimestamp(timestamp);

                    var content = line.Substring(contentStartIndex, leftParenthesesIndex - contentStartIndex);
                    // 首次执行，去除全行时间戳
                    if (i == 0)
                    {
                        content = new LyricLineVo(content).Content;
                    }

                    contentStartIndex = leftParenthesesIndex + parenthesesLength;

                    sb.Append(lyricTimestamp.PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType))
                        .Append(content);

                    // 最后一次执行，增加行结束时间戳
                    if (i == matches.Count - 1)
                    {
                        // 202
                        var timeCostStr = timeStr.Split(',')[1].Trim();
                        var timeCost = long.Parse(timeCostStr[..^1]);

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

    public static string DealVerbatimLyric4NetEaseMusic(string originLrc)
    {
        if (string.IsNullOrWhiteSpace(originLrc))
        {
            return string.Empty;
        }

        var defaultParam = new ConfigBean();
        var sb = new StringBuilder();

        foreach (var originLine in LyricUtils.SplitLrc(originLrc))
        {
            var line = originLine;

            var prefixMatch = GetVerbatimLegalPrefixRegex().Match(line);
            if (prefixMatch.Success)
            {
                var prefix = line.Substring(prefixMatch.Index, prefixMatch.Length);

                // remove prefix
                line = line[(prefixMatch.Index + prefixMatch.Length)..];

                Match match;
                while ((match = GetVerbatimRegex4NetEaseMusicRegex().Match(line)).Success)
                {
                    var group = match.Groups[0];

                    // (390,220,0)
                    var timeStr = line.Substring(group.Index, group.Length);
                    // 390
                    var timestamp = long.Parse(timeStr.Split(',')[0].Trim()[1..]);
                    var lyricTimestamp = new LyricTimestamp(timestamp);

                    line = line.Replace(timeStr,
                        lyricTimestamp.PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType));
                }

                // 追加行尾 timestamp
                var prefixArr = prefix.Split(',');
                var lastTimestamp =
                    new LyricTimestamp(long.Parse(prefixArr[0].Trim()[1..]) + long.Parse(prefixArr[1].Trim()[..^1]));
                line += lastTimestamp.PrintTimestamp(defaultParam.LrcTimestampFormat, defaultParam.DotType);
            }

            sb.Append(line);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// 将普通逐字 LRC 转为 A2 增强扩展格式
    /// </summary>
    public static string ConvertVerbatimLyricFromBasicToA2Mode(string lrcLine)
    {
        if (string.IsNullOrWhiteSpace(lrcLine))
            return lrcLine;

        var matches = LyricUtils.GetCommonLegalPrefixRegex().Matches(lrcLine);

        if (matches.Count == 0)
            return lrcLine;

        var result = "";
        var lastIndex = 0;
        var firstTime = true;

        foreach (Match match in matches)
        {
            var time = match.Groups[0].Value.Trim('[', ']');

            // 添加前面的歌词部分（去掉时间戳）
            result += lrcLine.Substring(lastIndex, match.Index - lastIndex);

            if (firstTime)
            {
                // 行首时间戳用方括号
                result += $"[{time}]";
                firstTime = false;
            }

            // 每个字前加尖括号时间戳
            result += $"<{time}>";

            lastIndex = match.Index + match.Length;
        }

        // 添加剩余的歌词文本
        result += lrcLine[lastIndex..];

        return result;
    }
}
