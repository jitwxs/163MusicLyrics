using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Bean
{
    [TestFixture]
    public class MusicLyricsVoTest
    {
        [Test]
        public void TestLyricLineVo()
        {
            var scenario1 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");
            
            Assert.AreEqual("[00:12.53]", scenario1.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario2 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");
            
            Assert.AreEqual("[00:12.52]", scenario2.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.DOWN));
            
            var scenario3 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");

            Assert.AreEqual("[00:12.526]", scenario3.Timestamp.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            var scenario4 = new LyricLineVo("[00:12.523]綺麗な嘘ごと自分を騙し続けて");

            Assert.AreEqual("[00:12.52]", scenario4.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario5 = new LyricLineVo("[00:00.000] 作词 : Kirara Magic");

            Assert.AreEqual("[00:00.00]", scenario5.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario6 = new LyricLineVo("[01:10.050] 作词 : Kirara Magic");

            Assert.AreEqual("[01:10.50]", scenario6.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
        }
    }
}