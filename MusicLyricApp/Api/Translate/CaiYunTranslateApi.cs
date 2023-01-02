using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;
using Newtonsoft.Json;

namespace MusicLyricApp.Api.Translate
{
    /// <summary>
    /// 彩云小译 API
    /// https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api/
    /// </summary>
    public class CaiYunTranslateApi : TranslateCacheableApi
    {
        private readonly string _token;

        private const string URL = "http://api.interpreter.caiyunai.com/v1/translator";

        private const int HTTP_CHUNK_SIZE = 15;

        /// <summary>
        /// 初始化 API
        /// https://dashboard.caiyunapp.com/v1/token/
        /// </summary>
        /// <param name="token">token 鉴权</param>
        public CaiYunTranslateApi(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new MusicLyricException(ErrorMsg.CAIYUN_TRANSLATE_AUTH_FAILED);
            }
            
            _token = token;
        }

        protected override string[] Translate0(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            string transType;

            switch (outputLanguage)
            {
                case LanguageEnum.CHINESE:
                    transType = "auto2zh";
                    break;
                case LanguageEnum.ENGLISH:
                    transType = "auto2en";
                    break;
                default:
                    // not support
                    return inputs;
            }

            var res = new string[inputs.Length];
            
            var headers = new Dictionary<string, string>
            {
                { "x-authorization", "token " + _token },
            };

            var index = 0;
            foreach (var input in ChunkArray(inputs, HTTP_CHUNK_SIZE))
            {
                var param = new Dictionary<string, object>
                {
                    { "source", input },
                    { "trans_type", transType },
                    { "request_id", "demo" },
                    { "detect", true },
                };

                var resultStr = HttpUtils.HttpPost(URL, param.ToJson(), "application/json", 30, headers);

                var result = JsonConvert.DeserializeObject<TranslateBean.CaiYunTranslateResult>(resultStr);

                if (result.Target == null)
                {
                    throw new MusicLyricException(ErrorMsg.CAIYUN_TRANSLATE_AUTH_FAILED);
                }
                
                foreach (var one in result.Target)
                {
                    res[index++] = one;
                }
            }

            return res;
        }
    }
}