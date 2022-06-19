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
                AreEqual(0, scenario.Minute);
                AreEqual(0, scenario.Second);
                AreEqual(0, scenario.Millisecond);
            
                AreEqual("[00:00.000]", scenario.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            }
        }

        [Test]
        public void TestCreateByTimestamp()
        {
            // 05:16:02.305
            var bean = new LyricTimestamp(18962305L);

            AreEqual(18962305L, bean.TimeOffset);
            AreEqual(316, bean.Minute);
            AreEqual(2, bean.Second);
            AreEqual(305, bean.Millisecond);
            
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
            AreEqual(1, scenario1.Minute);
            AreEqual(2, scenario1.Second);
            AreEqual(3, scenario1.Millisecond);
            
            AreEqual("[01:02.003]", scenario1.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            // scenario2 [00:39.17]
            var scenario2 = new LyricTimestamp("[[00:39.17]");
            AreEqual(0, scenario2.Minute);
            AreEqual(39, scenario2.Second);
            AreEqual(17, scenario2.Millisecond);
            
            AreEqual("[00:39.017]", scenario2.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            // scenario3 [00:39.17]
            var scenario3 = new LyricTimestamp("[00:2]");
            AreEqual(0, scenario3.Minute);
            AreEqual(2, scenario3.Second);
            AreEqual(0, scenario3.Millisecond);
            
            AreEqual("[00:02.000]", scenario3.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
        }
    }
}