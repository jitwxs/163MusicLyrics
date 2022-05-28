using System;

namespace MusicLyricApp.Bean
{
    public static class Constants
    {
        public const string Version = "v4.4";
        
        public static readonly string SettingPath = Environment.CurrentDirectory + "\\MusicLyricAppSetting.json";
        
        public static readonly string IpaDicPath = Environment.CurrentDirectory + "\\IpaDic";

        public const int SettingFormOffset = 20;

        public static readonly string[] IpaDicDependency = {
            IpaDicPath + "\\char.bin",
            IpaDicPath + "\\matrix.bin",
            IpaDicPath + "\\sys.dic",
            IpaDicPath + "\\unk.dic",
        };
    }
}