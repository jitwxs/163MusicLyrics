using System;
using System.Collections.Generic;

namespace MusicLyricApp.Bean
{
    public static class Constants
    {
        public const string Version = "v5.7";
        
        public static readonly string SettingPath = Environment.CurrentDirectory + "\\MusicLyricAppSetting.json";
        
        public static readonly string IpaDicPath = Environment.CurrentDirectory + "\\IpaDic";

        public const int SettingFormOffset = 20;

        public static readonly string[] IpaDicDependency = {
            IpaDicPath + "\\char.bin",
            IpaDicPath + "\\matrix.bin",
            IpaDicPath + "\\sys.dic",
            IpaDicPath + "\\unk.dic",
        };
        
        public static readonly string[] VerbatimLyricDependency = {
            Environment.CurrentDirectory + "\\QQMusicVerbatim.dll",
        };

        public static class HelpTips
        {
            public const string Prefix = "【提示区】";

            public enum TypeEnum
            {
                /// <summary>
                /// 时间戳设置
                /// </summary>
                TIME_STAMP_SETTING,
            }

            public static string GetContent(TypeEnum typeEnum)
            {
                var contentList = new List<string> { Prefix };

                switch (typeEnum)
                {
                    case TypeEnum.TIME_STAMP_SETTING:
                        contentList.Add("您可根据需要自行调整时间戳格式，系统定义了以下的『元变量』，您可直接使用");
                        contentList.Add("HH -> 小时，采用 24 小时制，结果为 0 ~ 23");
                        contentList.Add("mm -> 分钟，结果为 0 ~ 59");
                        contentList.Add("ss -> 秒，结果为 0 ~ 59");
                        contentList.Add("S -> 毫秒，仅保留一位，结果为 0 ~ 9");
                        contentList.Add("SS -> 毫秒，仅保留两位，结果为 0 ~ 99");
                        contentList.Add("SSS -> 毫秒，结果为 0 ~ 999");
                        contentList.Add("当毫秒的占位符为 S 或 SS 时，『毫秒截位规则』配置生效");
                        break;
                }

                return string.Join(Environment.NewLine + Environment.NewLine, contentList);
            }
        }
    }
}