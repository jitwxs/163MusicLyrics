using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kawazu;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Utils
{
    public static class RomajiUtils
    {
        public static async Task<List<LyricLineVo>> ToRomaji(List<LyricLineVo> inputList, List<LyricLineVo> faultList,
            RomajiConfigBean romajiConfig)
        {
            if (inputList.Any(vo => Utilities.HasKana(vo.Content)))
            {
                var converter = new KawazuConverter();
                var mode = ConvertModeEnum(romajiConfig.ModeEnum);
                var system = ConvertSystemEnum(romajiConfig.SystemEnum);

                var resultList = new List<LyricLineVo>();

                foreach (var vo in inputList)
                {
                    var content = await converter.Convert(vo.Content, To.Romaji, mode, system, "(", ")");
                    resultList.Add(new LyricLineVo
                    {
                        Content = content,
                        Timestamp = vo.Timestamp,
                        TimeOffset = vo.TimeOffset
                    });
                }

                return resultList;
            }

            return faultList;
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