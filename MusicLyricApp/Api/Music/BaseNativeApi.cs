using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using MusicLyricApp.Utils;

namespace MusicLyricApp.Api.Music
{
    public abstract class BaseNativeApi
    {
        public const string Useragent =
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

        public string DefaultCookie = "NMTID=" + Guid.NewGuid();

        private Func<string> _cookieFunc;

        protected BaseNativeApi(Func<string> cookieFunc)
        {
            _cookieFunc = cookieFunc;
        }
        
        protected abstract string HttpRefer();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">链接</param>
        /// <param name="paramDict">参数</param>
        /// <param name="method">模式</param>
        /// <exception cref="WebException"></exception>
        /// <returns></returns>
        protected string SendPost(string url, Dictionary<string, string> paramDict)
        {
            string result;
            using (var wc = new WebClient())
            {
                var cookie = _cookieFunc.Invoke();
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    cookie = DefaultCookie;
                }
                
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                wc.Headers.Add(HttpRequestHeader.Referer, HttpRefer());
                wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent);
                wc.Headers.Add(HttpRequestHeader.Cookie, cookie);

                var request = new NameValueCollection();
                foreach (var keyPair in @paramDict)
                {
                    request.Add(keyPair.Key, keyPair.Value);
                }

                var bytes = wc.UploadValues(url, "POST", request);
                result = Encoding.UTF8.GetString(bytes);
            }

            return result;
        }
        
        protected string SendJsonPost(string url, Dictionary<string, object> paramDict)
        {
            using (var wc = new WebClient())
            {
                var cookie = _cookieFunc.Invoke();
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    cookie = DefaultCookie;
                }
                
                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                wc.Headers.Add(HttpRequestHeader.Referer, HttpRefer());
                wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent);
                wc.Headers.Add(HttpRequestHeader.Cookie, cookie);

                return wc.UploadString(url, paramDict.ToJson());
            }
        }
    }
}