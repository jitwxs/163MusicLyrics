using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MusicLyricApp.Models;

namespace MusicLyricApp.Core.Utils;

public static partial class SrtUtils
{
    [GeneratedRegex(@"^\s*(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})\s*$")]
    private static partial Regex SrtTimestampRegex();

    /// <summary>
    /// 将 Lrc 格式，转换为 Srt 格式
    /// </summary>
    public static string LrcToSrt(List<LyricLineVo> inputList, string timestampFormat, DotTypeEnum dotType, long duration)
    {
        if (inputList.Count == 0)
        {
            return "";
        }

        var index = 1;
        var sb = new StringBuilder();

        void AddLine(LyricTimestamp start, LyricTimestamp end, string content)
        {
            sb
                .Append(index++)
                .Append(Environment.NewLine)
                .Append(start.PrintTimestamp(timestampFormat, dotType))
                .Append(" --> ")
                .Append(end.PrintTimestamp(timestampFormat, dotType))
                .Append(Environment.NewLine)
                .Append(content)
                .Append(Environment.NewLine)
                .Append(Environment.NewLine);
        }

        var durationTimestamp = new LyricTimestamp(duration);

        if (inputList.Count == 1)
        {
            AddLine(inputList[0].Timestamp, durationTimestamp, inputList[0].Content);
        }
        else
        {
            var i = 0;
            for (; i < inputList.Count - 1; i++)
            {
                LyricLineVo startVo = inputList[i], endVo = inputList[i + 1];

                var compareTo = startVo.Timestamp.CompareTo(endVo.Timestamp);

                if (compareTo == 1)
                {
                    AddLine(startVo.Timestamp, durationTimestamp, startVo.Content);
                }
                else if (compareTo == 0)
                {
                    var endTimestamp = durationTimestamp;

                    var j = i + 1;
                    while (++j < inputList.Count)
                    {
                        if (inputList[j].Timestamp.CompareTo(startVo.Timestamp) == 0)
                        {
                            continue;
                        }

                        if (inputList[j].Timestamp.CompareTo(startVo.Timestamp) > 0)
                        {
                            endTimestamp = inputList[j].Timestamp;
                        }

                        break;
                    }

                    do
                    {
                        AddLine(inputList[i].Timestamp, endTimestamp, inputList[i].Content);
                    } while (++i < j);

                    i--;
                }
                else
                {
                    AddLine(startVo.Timestamp, endVo.Timestamp, startVo.Content);
                }
            }

            if (i < inputList.Count)
            {
                var lastVo = inputList[inputList.Count - 1];
                AddLine(lastVo.Timestamp, durationTimestamp, lastVo.Content);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 将 SRT 文本转换为 LRC 文本
    /// </summary>
    public static string SrtToLrc(string srtText, string lrcTimestampFormat, DotTypeEnum dotType)
    {
        if (string.IsNullOrWhiteSpace(srtText))
        {
            return string.Empty;
        }

        var normalized = srtText.Replace("\r\n", "\n").Replace('\r', '\n');
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<LyricLineVo>();

        foreach (var block in blocks)
        {
            var rowList = block.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (rowList.Count < 2)
            {
                continue;
            }

            var rangeRowIndex = rowList[0].Contains("-->") ? 0 : 1;
            if (rangeRowIndex >= rowList.Count || !rowList[rangeRowIndex].Contains("-->"))
            {
                continue;
            }

            var range = rowList[rangeRowIndex].Split("-->", StringSplitOptions.TrimEntries);
            if (range.Length != 2)
            {
                continue;
            }

            var startMs = ParseSrtTimestampMs(range[0]);
            var content = string.Join(" ", rowList.Skip(rangeRowIndex + 1)).Trim();
            lines.Add(new LyricLineVo(content, new LyricTimestamp(startMs)));
        }

        lines = lines.OrderBy(x => x.Timestamp.TimeOffset).ToList();

        return string.Join(Environment.NewLine, lines.Select(x => x.Print(lrcTimestampFormat, dotType)));
    }

    private static long ParseSrtTimestampMs(string text)
    {
        var match = SrtTimestampRegex().Match(text);
        if (!match.Success)
        {
            throw new MusicLyricException(MusicLyricApp.Core.Localization.LocalizationService.Format("Error.InvalidSrtTimestamp", text));
        }

        var h = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var m = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var s = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var msText = match.Groups[4].Value;
        var ms = msText.Length switch
        {
            1 => int.Parse(msText, CultureInfo.InvariantCulture) * 100,
            2 => int.Parse(msText, CultureInfo.InvariantCulture) * 10,
            _ => int.Parse(msText[..3], CultureInfo.InvariantCulture)
        };

        return ((h * 3600L + m * 60L + s) * 1000L) + ms;
    }
}

