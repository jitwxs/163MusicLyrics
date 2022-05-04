using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Bean
{
    [TestFixture]
    public class MusicLyricsVoTest
    {
        [Test]
        public void TestLyricTimestamp()
        {
            // 空数据 && 不合法
            foreach (var scenario in new[]
                     {
                         new LyricTimestamp(), 
                         new LyricTimestamp("[12131"),
                         new LyricTimestamp("-1]")
                     })
            {
                Assert.AreEqual(0, scenario.Minute);
                Assert.AreEqual(0, scenario.Second);
                Assert.AreEqual(0, scenario.Millisecond);
            
                Assert.AreEqual("[00:00.000]", scenario.ToString());
            }
            
            // scenario1 [1:2.3]
            var scenario1 = new LyricTimestamp("[1:2.3]");
            Assert.AreEqual(1, scenario1.Minute);
            Assert.AreEqual(2, scenario1.Second);
            Assert.AreEqual(3, scenario1.Millisecond);
            
            Assert.AreEqual("[01:02.003]", scenario1.ToString());
            
            // scenario2 [00:39.17]
            var scenario2 = new LyricTimestamp("[[00:39.17]");
            Assert.AreEqual(0, scenario2.Minute);
            Assert.AreEqual(39, scenario2.Second);
            Assert.AreEqual(17, scenario2.Millisecond);
            
            Assert.AreEqual("[00:39.017]", scenario2.ToString());
            
            // scenario3 [00:39.17]
            var scenario3 = new LyricTimestamp("[00:2]");
            Assert.AreEqual(0, scenario3.Minute);
            Assert.AreEqual(2, scenario3.Second);
            Assert.AreEqual(0, scenario3.Millisecond);
            
            Assert.AreEqual("[00:02.000]", scenario3.ToString());
        }
    }
}