using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;

namespace MusicLyricApp.Api.Translate
{
    public abstract class TranslateCacheableApi : ITranslateApi
    {
        protected abstract string[] Translate0(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage);
        
        public string[] Translate(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            if (inputs == null || inputs.Length == 0)
            {
                return new string[] {};
            }

            var cacheDict = GlobalCache.BatchQuery<string, string>(CacheType.TRANSLATE, inputs, out var notHitInputs);
            
            var httpRes = new string[] { };
            if (notHitInputs.Length > 0)
            {
                httpRes = Translate0(notHitInputs, inputLanguage, outputLanguage);
            }
            
            var res = new string[inputs.Length];

            int index = 0, resIndex = 0;
            foreach (var input in inputs)
            {
                if (!cacheDict.TryGetValue(input, out var output))
                {
                    output = httpRes[resIndex++];

                    if (output != null)
                    {
                        GlobalCache.DoCache(CacheType.TRANSLATE, input, output);
                    }
                }
                res[index++] = output;
            }

            return res;
        }
        
        protected static string[][] ChunkArray(string[] inputs, int chunkSize)
        {
            var i = 0;
            return inputs.GroupBy(s => i++ / chunkSize).Select(g => g.ToArray()).ToArray();
        }
    }
}