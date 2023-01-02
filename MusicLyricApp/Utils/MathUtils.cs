using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MusicLyricApp.Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// 将字符串转换为十六进制 sbyte 数组
        /// </summary>
        /// <param name="s">原始字符串</param>
        /// <param name="sbytes">数组结构</param>
        /// <returns>数组大小</returns>
        public static int ConvertStringToHexSbytes(string s, out sbyte[] sbytes)
        {
            sbytes = new sbyte[1024 * 1024];
            var sz = 0;

            for (var i = 1; i < s.Length; i += 2)
            {
                sbytes[sz++] = (sbyte)(ParseHex(s[i - 1]) * 16 + ParseHex(s[i]));
            }

            return sz;
        }

        /// <summary>
        /// int 转十六进制 ASCII 码
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int ParseHex(int c)
        {
            if (c >= '0' && c <= '9') return (c - '0');
            else if (c >= 'a' && c <= 'f') return (c - 'a' + 10);
            else if (c >= 'A' && c <= 'F') return (c - 'A' + 10);
            else return -1;
        }

        /// <summary>
        /// sbyte[] -> byte[]
        /// </summary>
        /// <param name="sbytes">sbyte[]</param>
        /// <param name="sz">数组大小</param>
        /// <returns></returns>
        public static byte[] SbytesToBytes(sbyte[] sbytes, int sz)
        {
            var bytes = new byte[sz];
            Buffer.BlockCopy(sbytes, 0, bytes, 0, sz);

            return bytes;
        }

        /// <summary>
        /// 基于 SharpZip 的解压工具
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] SharpZipLibDecompress(byte[] data)
        {
            var compressed = new MemoryStream(data);
            var decompressed = new MemoryStream();
            var inputStream = new InflaterInputStream(compressed);

            inputStream.CopyTo(decompressed);

            return decompressed.ToArray();
        }

        public static bool IsBase64(this string base64Str)
        {
            byte[] bytes = null;
            return IsBase64(base64Str, out bytes);
        }
        
        private static readonly char[] Base64CodeArray = {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
            'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
            'v', 'w', 'x', 'y', 'z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '='
        };

        /// <summary>
        /// 是否base64字符串
        /// </summary>
        /// <param name="base64Str">要判断的字符串</param>
        /// <param name="bytes">字符串转换成的字节数组</param>
        /// <returns></returns>
        private static bool IsBase64(string base64Str, out byte[] bytes)
        {
            //string strRegex = "^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{4}|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)$";
            bytes = null;
            if (string.IsNullOrEmpty(base64Str))
                return false;
            else
            {
                if (base64Str.Contains(","))
                    base64Str = base64Str.Split(',')[1];
                if (base64Str.Length % 4 != 0)
                    return false;
                if (base64Str.Any(c => !Base64CodeArray.Contains(c)))
                    return false;
            }

            try
            {
                bytes = Convert.FromBase64String(base64Str);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
