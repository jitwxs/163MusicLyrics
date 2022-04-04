using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicLyricApp.Utils
{
    public class SrtUtils
    {
        /// <summary>
        /// 将 Lrc 格式，转换为 Srt 格式
        /// </summary>
        /// <param name="input">lrc 格式内容</param>
        /// <param name="duration">时长 ms</param>
        /// <returns></returns>
        public static string LrcToSrt(string input, long duration)
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
                AddSrtLine(TimeSpan.FromMilliseconds(duration));
            }

            return output.ToString();
        }
    }
}