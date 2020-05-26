using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 网易云歌词提取
{
    public class NeteaseMusicResultCache
    {
        private static Dictionary<long, SaveVO> cache = new Dictionary<long, SaveVO>();

        public static bool Contains(long songId)
        {
            return cache.ContainsKey(songId);
        }

        public static SaveVO Get(long songId)
        {
            SaveVO result = null;
            cache.TryGetValue(songId, out result);
            return result;
        }

        public static void Put(long songId, SaveVO saveVO)
        {
            cache.Add(songId, saveVO);
        }
    }
}
