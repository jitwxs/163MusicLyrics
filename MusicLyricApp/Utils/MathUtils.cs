using System;
using System.IO;
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
        public static int ParseHex(int c) {
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
    }
}
