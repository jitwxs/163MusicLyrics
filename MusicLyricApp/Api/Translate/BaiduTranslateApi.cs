using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using MusicLyricApp.Utils;
using Newtonsoft.Json;

namespace MusicLyricApp.Api.Translate
{

    /// <summary>
    /// 百度翻译 API
    /// https://fanyi-api.baidu.com/product/11
    /// </summary>
    public class BaiduTranslateApi : TranslateCacheableApi
    {
        private readonly string _appId, _secret;
        private readonly int _delay;
        private DateTime _last;
        
        private const string API_URL = "https://fanyi-api.baidu.com/api/trans/vip/translate";

        /// <summary>
        /// 单次翻译文本长度限定为6000字节以内（汉字约为2000个）
        /// </summary>
        private const int HTTP_CHUNK_WORD_LENGTH = 2000;
        
        /// <summary>
        /// 初始化 API
        /// https://fanyi-api.baidu.com/manage/developer
        /// </summary>
        /// <param name="appId">APP ID</param>
        /// <param name="secret">密钥</param>
        /// <param name="delay">延迟，目前百度云 QPS 不能超过 1s，因此至少配置为 1000</param>
        /// <exception cref="MusicLyricException"></exception>
        public BaiduTranslateApi(string appId, string secret, int delay = 1000)
        {
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret))
            {
                throw new MusicLyricException(ErrorMsg.BAIDU_TRANSLATE_AUTH_FAILED);
            }

            _appId = appId;
            _secret = secret;
            _delay = delay;
        }
        
        protected override ResultVo<string[]> Translate0(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            string from = CastLanguageEnum(inputLanguage), to = CastLanguageEnum(outputLanguage);

            var inputsList = new List<List<string>>();
            var totalCount = 0;
            
            for (var i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];

                totalCount += input.Length;

                var index = totalCount / HTTP_CHUNK_WORD_LENGTH;

                if (inputsList.Count < index + 1)
                {
                    inputsList.Add(new List<string>());
                }
                
                inputsList[index].Add(input);
            }
            
            var md5 = MD5.Create();
            var transResultDict = new Dictionary<string, string>();
            
            foreach (var subInputs in inputsList)
            {
                var source = string.Join("\n", subInputs);
                
                var md5Byte = md5.ComputeHash(Encoding.UTF8.GetBytes(_appId + source + _last.Millisecond + _secret)) ?? throw new ArgumentNullException("md5.ComputeHash(Encoding.UTF8.GetBytes(_appId + source + last.Millisecond + _secret))");
                var md5String = new string(md5Byte.SelectMany(s => $"{s:x2}").ToArray());
                var utf8 = System.Web.HttpUtility.UrlEncode(source, Encoding.UTF8);
                
                var url = $"{API_URL}?q={utf8}&from={from}&to={to}&appid={_appId}&salt={_last.Millisecond}&sign={md5String}";

                var time = DateTime.Now - _last;
                _last = DateTime.Now;
                if (time.TotalMilliseconds < _delay)
                    Thread.Sleep(_delay - (int)time.TotalMilliseconds);
                
                var resultStr = HttpUtils.HttpGet(url);

                var translateResult = JsonConvert.DeserializeObject<TranslateBean.BaiduTranslateResult>(resultStr);

                var errorCode = translateResult.error_code;

                if (errorCode != null && !errorCode.Equals("52000"))
                {
                    return ResultVo<string[]>.Failure(errorCode + " -> https://fanyi-api.baidu.com/doc/21");
                }
                
                foreach (var one in translateResult.trans_result)
                {
                    transResultDict[one.src] = one.dst;
                }
            }

            var outputs = new string[inputs.Length];
            
            for (var i = 0; i < inputs.Length; i++)
            {
                var output = transResultDict[inputs[i]];

                outputs[i] = output ?? "";
            }

            return new ResultVo<string[]>(outputs);
        }

        protected override bool IsSupport0(LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            // all support
            return true;
        }

        private static string CastLanguageEnum(LanguageEnum languageEnum)
        {
            switch (languageEnum)
            {
                case LanguageEnum.ENGLISH:
                    return "en";
                case LanguageEnum.JAPANESE:
                    return "jp";
                case LanguageEnum.KOREAN:
                    return "kor";
                case LanguageEnum.RUSSIAN:
                    return "ru";
                case LanguageEnum.FRENCH:
                    return "fra";
                case LanguageEnum.ITALIAN:
                    return "it";
                case LanguageEnum.CHINESE:
                    return "zh";
                default:
                    return "auto";
            }
        }
    }
}