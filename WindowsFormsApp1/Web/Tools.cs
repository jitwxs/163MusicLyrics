using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace 网易云歌词提取.Web
{
    public static class WebTools
    {
        public const string TEST_URL = "bilibili.com";
        private readonly static Ping _ping = new Ping();
                
        /// <summary>
        /// 检测网络延迟, 检测失败返回-1, 单位为毫秒
        /// </summary>
        /// <param name="timeout">时间限制</param>
        /// <returns>网络延迟</returns>
        /// <exception cref="PingException">检测失败</exception>
        public static long GetWebRoundtripTime(int timeout = 200)
        {
            try
            {
                var info = _ping.Send(TEST_URL, timeout);
                if (info.Status == IPStatus.Success)
                    return info.RoundtripTime;
                else
                    return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// 检测网络延迟的异步版本, 检测失败返回-1, 单位为毫秒
        /// </summary>
        /// <returns>网络延迟</returns>
        public static async Task<long> GetWebRoundtripTimeAsync(int timeout = 200)
        {
            try
            {
                var info = await _ping.SendPingAsync(TEST_URL, timeout);
                if (info.Status == IPStatus.Success)
                    return info.RoundtripTime;
                else
                    return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
