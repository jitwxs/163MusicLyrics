using System;
using System.Collections.Generic;
using System.Text;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Utils
{
    public static class SrtUtils
    {
        /// <summary>
        /// 将 Lrc 格式，转换为 Srt 格式
        /// </summary>
        /// <param name="inputList">歌词行数据</param>
        /// <param name="timestampFormat">时间戳格式</param>
        /// <param name="dotType">时间戳截位规则</param>
        /// <param name="duration">时长 ms</param>
        /// <returns></returns>
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
                    .Append(start.PrintTimestamp(timestampFormat, dotType)).Append(" --> ").Append(end.PrintTimestamp(timestampFormat, dotType))
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
                        // start > end
                        AddLine(startVo.Timestamp, durationTimestamp, startVo.Content);
                    }
                    else if (compareTo == 0)
                    {
                        // start == end
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
                        // start < end
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
    }
}