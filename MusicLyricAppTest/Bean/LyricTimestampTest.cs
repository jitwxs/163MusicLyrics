using MusicLyricApp.Bean;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace MusicLyricAppTest.Bean
{
    [TestFixture]
    public class LyricTimestampTest
    {
        [Test]
        public void TestIllegalScenario()
        {
            // 空数据 && 不合法
            foreach (var scenario in new[]
                     {
                         new LyricTimestamp(""), 
                         new LyricTimestamp("[12131"),
                         new LyricTimestamp("-1]")
                     })
            {
                AreEqual("[00:00.000]", scenario.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            }
        }

        [Test]
        public void TestCreateByTimestamp()
        {
            // 05:16:02.305
            var bean = new LyricTimestamp(18962305L);

            AreEqual(18962305L, bean.TimeOffset);
            
            AreEqual("[05:16:02+305]", bean.PrintTimestamp("[HH:mm:ss+SSS]", DotTypeEnum.HALF_UP));
            AreEqual("[316:02+305]", bean.PrintTimestamp("[mm:ss+SSS]", DotTypeEnum.DOWN));
            
            AreEqual("[05:16:02:31]", bean.PrintTimestamp("[HH:mm:ss:SS]", DotTypeEnum.HALF_UP));
            AreEqual("[316:02:30]", bean.PrintTimestamp("[mm:ss:SS]", DotTypeEnum.DOWN));
            
            AreEqual("[05H:16m:02s:3]", bean.PrintTimestamp("[HHH:mmm:sss:S]", DotTypeEnum.HALF_UP));
            AreEqual("[316@02@3]", bean.PrintTimestamp("[mm@ss@S]", DotTypeEnum.DOWN));
        }

        [Test]
        public void TestCreateByTimestampStr()
        {
            // scenario1 [1:2.3]
            var scenario1 = new LyricTimestamp("[1:2.3]");
            
            AreEqual("[01:02.300]", scenario1.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            // scenario2 [00:39.17]
            var scenario2 = new LyricTimestamp("[[00:39.17]");
            
            AreEqual("[00:39.170]", scenario2.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            // scenario3 [00:39.17]
            var scenario3 = new LyricTimestamp("[00:2]");
            
            AreEqual("[00:02.000]", scenario3.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
        }

        [Test]
        public void TestIssue109()
        {
            var scenario = new LyricTimestamp("[04:17.995]");
            
            AreEqual("[04:18.00]", scenario.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            scenario = new LyricTimestamp("[04:19.093]");
            
            AreEqual("[04:19.09]", scenario.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            scenario = new LyricTimestamp("[00:10.98]");
            
            AreEqual("[00:10.980]", scenario.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            AreEqual("[00:10.980]", scenario.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.HALF_UP));
        }
    }
}