using MusicLyricApp.Bean;
using MusicLyricApp.Utils;
using NUnit.Framework;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class LyricUtilsTest
    {
        [Test]
        public void TestSetTimeStamp2Dot()
        {
            var scenario1 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");

            LyricUtils.SetMillisecondScale(scenario1, DotTypeEnum.HALF_UP, 2);
            
            Assert.AreEqual("[00:12.53]", scenario1.Timestamp.ToString(OutputFormatEnum.LRC));
            
            var scenario2 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");

            LyricUtils.SetMillisecondScale(scenario2, DotTypeEnum.DOWN, 2);
            
            Assert.AreEqual("[00:12.52]", scenario2.Timestamp.ToString(OutputFormatEnum.LRC));
            
            var scenario3 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");

            LyricUtils.SetMillisecondScale(scenario3, DotTypeEnum.DISABLE, 2);
            
            Assert.AreEqual("[00:12.526]", scenario3.Timestamp.ToString(OutputFormatEnum.LRC));
            
            var scenario4 = new LyricLineVo("[00:12.523]綺麗な嘘ごと自分を騙し続けて");

            LyricUtils.SetMillisecondScale(scenario4, DotTypeEnum.HALF_UP, 2);
            
            Assert.AreEqual("[00:12.52]", scenario4.Timestamp.ToString(OutputFormatEnum.LRC));
            
            var scenario5 = new LyricLineVo("[00:00.000] 作词 : Kirara Magic");

            LyricUtils.SetMillisecondScale(scenario5, DotTypeEnum.HALF_UP, 2);
            
            Assert.AreEqual("[00:00.00]", scenario5.Timestamp.ToString(OutputFormatEnum.LRC));
            
            var scenario6 = new LyricLineVo("[01:10.050] 作词 : Kirara Magic");

            LyricUtils.SetMillisecondScale(scenario6, DotTypeEnum.HALF_UP, 2);
            
            Assert.AreEqual("[01:10.50]", scenario6.Timestamp.ToString(OutputFormatEnum.LRC));
        }
    }
}