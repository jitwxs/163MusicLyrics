using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kawazu;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Utils
{
    public static class RomajiUtils
    {
        public static async Task<List<LyricLineVo>> ToRomaji(List<LyricLineVo> inputList, List<LyricLineVo> faultList,
            RomajiConfigBean romajiConfig, SettingBean settingBean)
        {
            var timestampFormat = settingBean.Param.LrcTimestampFormat;
            var dotType = settingBean.Param.DotType;
            
            if (inputList.Any(vo => Utilities.HasKana(vo.Content)))
            {
                var converter = new KawazuConverter();
                var mode = ConvertModeEnum(romajiConfig.ModeEnum);
                var system = ConvertSystemEnum(romajiConfig.SystemEnum);

                var resultList = new List<LyricLineVo>();

                foreach (var vo in inputList)
                {
                    // need try split sub lyricLineVO, resolve verbatim lyric mode
                    var content = new StringBuilder();
                    foreach (var subVo in LyricLineVo.Split(vo))
                    {
                        var subContent = await converter.Convert(subVo.Content, To.Romaji, mode, system, "(", ")");
                        
                        content.Append(subVo.Timestamp.PrintTimestamp(timestampFormat, dotType) + subContent);
                    }
                    
                    resultList.Add(new LyricLineVo(content.ToString()));
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