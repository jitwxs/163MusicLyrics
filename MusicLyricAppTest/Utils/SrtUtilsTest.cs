using System;
using System.Threading;
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
            DoTest("ああだのこうだの知ったもんか 幸先の空は悪天候");
            
            DoTest("[00:29.720]いつもどおりの通り独り こんな日々もはや懲り懲り");
            
            Thread.Sleep(1000);
        }
        
        private static void DoTest(string content)
        {
            var converter = new KawazuConverter();
            
            Console.WriteLine(converter.Convert(content, To.Hiragana, Mode.Normal, RomajiSystem.Hepburn).Result);
            Console.WriteLine(converter.Convert(content, To.Hiragana, Mode.Okurigana, RomajiSystem.Hepburn).Result);
            
            Console.WriteLine(converter.Convert(content, To.Katakana, Mode.Normal, RomajiSystem.Hepburn).Result);
            Console.WriteLine(converter.Convert(content, To.Katakana, Mode.Okurigana, RomajiSystem.Hepburn).Result);

            Console.WriteLine(converter.Convert(content, To.Romaji, Mode.Normal, RomajiSystem.Hepburn).Result);
            Console.WriteLine(converter.Convert(content, To.Romaji, Mode.Okurigana, RomajiSystem.Hepburn).Result);
        }
    }
}