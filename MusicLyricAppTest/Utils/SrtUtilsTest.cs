using System;
using System.Threading;
using System.Threading.Tasks;
using Kawazu;
using NUnit.Framework;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class SrtUtilsTest
    {
        [Test]
        public void TestJapaneseLanguageJudge()
        {
            Main("ああだのこうだの知ったもんか 幸先の空は悪天候");
            
            Main("[00:29.720]いつもどおりの通り独り こんな日々もはや懲り懲り");
            
            Thread.Sleep(1000);
        }
        
        private static async Task Main(string content)
        {
            var converter = new KawazuConverter();

            Console.WriteLine(await converter.Convert(content, To.Romaji, Mode.Spaced, RomajiSystem.Hepburn, "(", ")"));
        }
    }
}