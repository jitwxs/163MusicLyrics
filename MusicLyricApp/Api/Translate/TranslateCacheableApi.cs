using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;

namespace MusicLyricApp.Api.Translate
{
    public abstract class TranslateCacheableApi : ITranslateApi
    {
        private const string PREFIX = "";
        
        protected abstract ResultVo<string[]> Translate0(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage);

        protected abstract bool IsSupport0(LanguageEnum inputLanguage, LanguageEnum outputLanguage);
        
        public string[] Translate(string[] inputs, LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            if (inputs == null || inputs.Length == 0)
            {
                return new string[] {};
            }

            var cacheDict = GlobalCache.BatchQuery<string>(PREFIX, CacheType.TRANSLATE, inputs, out var notHitInputs);
            
            var httpRes = new string[] { };
            if (notHitInputs.Length > 0)
            {
                httpRes = Translate0(notHitInputs, inputLanguage, outputLanguage).Assert().Data;
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
                        GlobalCache.DoCache(PREFIX, CacheType.TRANSLATE, input, output);
                    }
                }
                res[index++] = output;
            }

            return res;
        }

        public bool IsSupport(LanguageEnum inputLanguage, LanguageEnum outputLanguage)
        {
            return IsSupport0(inputLanguage, outputLanguage);
        }

        protected static string[][] ChunkArray(string[] inputs, int chunkSize)
        {
            var i = 0;
            return inputs.GroupBy(s => i++ / chunkSize).Select(g => g.ToArray()).ToArray();
        }
    }
}