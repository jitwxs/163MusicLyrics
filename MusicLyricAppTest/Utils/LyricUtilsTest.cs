using System;
using System.Collections.Generic;
using MusicLyricApp.Bean;
using MusicLyricApp.Utils;
using NUnit.Framework;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class LyricUtilsTest
    {
        private readonly List<LyricsTypeEnum> _outputLyricsTypes = new List<LyricsTypeEnum> { LyricsTypeEnum.ORIGIN_TRANS };
        
        [Test]
        public void TestDealVerbatimLyric()
        {
            const string line = "[11562,5703]こ(11562,356)こ(11918,646)ろ(12564,270)を(12834,798) (13632,10)隠(13642,592)し(14234,356)て(14590,248)ひ(14838,260)と(15098,428)り(15526,396)で(15922,1343)";
            var output = LyricUtils.DealVerbatimLyric(line, SearchSourceEnum.QQ_MUSIC);
            
            Assert.AreEqual("[00:11.562]こ[00:11.918]こ[00:12.564]ろ[00:12.834]を[00:13.632] [00:13.642]隠[00:14.234]し[00:14.590]て[00:14.838]ひ[00:15.098]と[00:15.526]り[00:15.922]で[00:17.265]" + Environment.NewLine, output);
        }

        /// <summary>
        /// 处理译文歌词 | 不存在原文歌词
        /// </summary>
        [Test]
        public void TestDealTranslateLyricWithEmptyOriginLyric()
        {
            var transConfig = new TransConfigBean();
            var translateList = new List<LyricLineVo>();
            
            var res = LyricUtils.DealTranslateLyric(new List<LyricLineVo>(), translateList, 
                transConfig, _outputLyricsTypes).Result;
            
            Assert.AreEqual(0, res.Count);
        }

        /// <summary>
        /// 译文匹配精度误差
        /// </summary>
        [Test]
        public void TestTranslateMatchPrecisionDeviation()
        {
            var transConfig = new TransConfigBean();
 
            var translateList = new List<LyricLineVo>();
            var originList = new List<LyricLineVo>();
            

            originList.Add(new LyricLineVo("내가 말하는 것까지 Babe", new LyricTimestamp(80)));
            
            translateList.Add(new LyricLineVo("包括我在说的东西", new LyricTimestamp(100)));
            
            // scenario1: 未配置精度误差
            
            var res = LyricUtils.DealTranslateLyric(originList, translateList, transConfig, _outputLyricsTypes).Result;
            
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(100, res[0][0].Timestamp.TimeOffset);
            
            // scenario2: 配置精度误差

            transConfig.MatchPrecisionDeviation = 20;
            
            res = LyricUtils.DealTranslateLyric(originList, translateList, transConfig, _outputLyricsTypes).Result;
            
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(80, res[0][0].Timestamp.TimeOffset);
        }
    }
}