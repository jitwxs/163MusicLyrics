using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kawazu;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Utils
{
    public static class RomajiUtils
    {
        public static async Task<List<LyricLineVo>> ToRomaji(List<LyricLineVo> inputList, RomajiModeEnum modeEnum, RomajiSystemEnum systemEnum)
        {
            var resultList = new List<LyricLineVo>();
            
            if (inputList.Any(vo => Utilities.HasKana(vo.Content)))
            {
                using (var converter = new KawazuConverter())
                {
                    var mode = ConvertModeEnum(modeEnum);
                    var system = ConvertSystemEnum(systemEnum);

                    foreach (var vo in inputList)
                    {
                        var content = await converter.Convert(vo.Content, To.Romaji, mode, system, "(", ")");
                    
                        // 去重时间戳中的空格 eg [ 00 : 23 . 113 ]
                        content = Regex.Replace(content, "\\[\\s+\\d+\\s+:\\s+\\d+\\s+\\.\\s+\\d+\\s+]", 
                            m => m.ToString().Replace(" ", ""));
                    
                        resultList.Add(new LyricLineVo(content, vo.Timestamp));
                    }
                }
            }

            return resultList;
        }

        private static Mode ConvertModeEnum(RomajiModeEnum modeEnum)
        {
            switch (modeEnum)
            {
                case RomajiModeEnum.NORMAL:
                    return Mode.Normal;
                case RomajiModeEnum.SPACED:
                    return Mode.Spaced;
                case RomajiModeEnum.OKURIGANA:
                    return Mode.Okurigana;
                case RomajiModeEnum.FURIGANA:
                    return Mode.Furigana;
                default:
                    throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
            }
        }

        private static RomajiSystem ConvertSystemEnum(RomajiSystemEnum systemEnum)
        {
            switch (systemEnum)
            {
                case RomajiSystemEnum.NIPPON:
                    return RomajiSystem.Nippon;
                case RomajiSystemEnum.PASSPORT:
                    return RomajiSystem.Passport;
                case RomajiSystemEnum.HEPBURN:
                    return RomajiSystem.Hepburn;
                default:
                    throw new MusicLyricException(ErrorMsg.FUNCTION_NOT_SUPPORT);
            }
        }
    }
}