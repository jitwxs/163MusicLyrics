using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;
using NUnit.Framework;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class LyricUtilsTest
    {
        /// <summary>
        /// 译文匹配精度误差
        /// </summary>
        [Test]
        public void TestTranslateMatchPrecisionDeviation()
        {
            var settingBean = new SettingBean();
 
            var translateList = new List<LyricLineVo>();
            var originList = new List<LyricLineVo>();
            
            originList.Add(new LyricLineVo("내가 말하는 것까지 Babe", new LyricTimestamp(80)));
            
            translateList.Add(new LyricLineVo("包括我在说的东西", new LyricTimestamp(100)));
            
            // scenario1: 未配置精度误差
            
            var res = LyricUtils.DealTranslateLyric(originList, translateList, settingBean);
            
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(100, res[0].Timestamp.TimeOffset);
            
            // scenario2: 配置精度误差

            settingBean.Param.TranslateMatchPrecisionDeviation = 20;
            
            res = LyricUtils.DealTranslateLyric(originList, translateList, settingBean);
            
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(80, res[0].Timestamp.TimeOffset);
        }
    }
}