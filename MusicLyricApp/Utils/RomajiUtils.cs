using System;
using System.Threading.Tasks;
using Kawazu;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Utils
{
    public static class RomajiUtils
    {
        public static async Task<string[]> ToRomaji(string[] input, RomajiConfigBean romajiConfig)
        {
            if (input == null || input.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = new string[input.Length];
            
            var converter = new KawazuConverter();
            var mode = ConvertModeEnum(romajiConfig.ModeEnum);
            var system = ConvertSystemEnum(romajiConfig.SystemEnum);
            
            for (var i = 0; i < input.Length; i++)
            {
                if (Utilities.HasJapanese(input[i]))
                {
                    var startIndex = input[i].IndexOf("]") + 1;
                    if (startIndex == -1)
                    {
                        result[i] = await converter.Convert(input[i], To.Romaji, mode, system, "(", ")");
                    }
                    else
                    {
                        var head = input[i].Substring(0, startIndex);
                        var body = await converter.Convert(input[i].Substring(startIndex), To.Romaji, mode, system, "(", ")");
                        
                        result[i] = head + body;
                    }
                }
                else
                {
                    result[i] = input[i];
                }
            }

            return result;
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