using System.Collections.Generic;

namespace 网易云歌词提取
{
    public class NeteaseMusicResultCache
    {
        private static Dictionary<string, SaveVO> cache = new Dictionary<string, SaveVO>();

        public static bool Contains(string songId)
        {
            return cache.ContainsKey(songId);
        }

        public static SaveVO Get(string songId)
        {
            SaveVO result = null;
            cache.TryGetValue(songId, out result);
            return result;
        }

        public static void Put(string songId, SaveVO saveVO)
        {
            cache.Add(songId, saveVO);
        }
    }
}