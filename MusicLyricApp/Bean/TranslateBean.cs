namespace MusicLyricApp.Bean
{
    public class TranslateBean
    {
        public class CaiYunTranslateResult
        {
            public string[] Target { get; set; }

            public int Rc { get; set; }

            public double Confidence { get; set; }
        }

        public class BaiduTranslateResult
        {
            /// <summary>
            /// 错误码 https://fanyi-api.baidu.com/doc/21
            ///</summary>
            public string error_code { get; set; }

            /// <summary>
            /// 源语种
            /// </summary>
            public string from { get; set; }

            /// <summary>
            /// 目标语种
            /// </summary> 
            public string to { get; set; }

            public TransResult[] trans_result { get; set; }
            
            public class TransResult
            {
                /// <summary>
                /// 源字符串
                /// </summary>
                public string src { get; set; }
                /// <summary>
                /// 翻译后的字符串
                /// </summary>
                public string dst { get; set; }
            }
        }
    }
}