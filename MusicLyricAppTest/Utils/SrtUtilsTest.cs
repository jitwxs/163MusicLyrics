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

            var task = converter.Convert(content, To.Romaji, Mode.Spaced, RomajiSystem.Hepburn, "(", ")");

            Console.WriteLine(task.Result);
        }
    }
}