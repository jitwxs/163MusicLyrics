using System;
using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NUnit.Framework;
using static MusicLyricApp.Utils.GlobalUtils;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class GlobalUtilsTest
    {
        [Test]
        public void TestFormatDate()
        {
            var res = FormatDate(1657900800000);
            Console.WriteLine(res);
        }

        [Test]
        public void TestCheckInputIdWithEmpty()
        {
            var exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId(string.Empty, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);

            exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId(null, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);
        }
        
        [Test]
        public void TestCheckInputIdWithNetEase()
        {
            var searchSource = SearchSourceEnum.NET_EASE_MUSIC;
            var searchType = SearchTypeEnum.SONG_ID;

            // 纯数字
            Assert.AreEqual("1", CheckInputId("1", searchSource, searchType).QueryId);
            
            // 不支持非数字
            var exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId("abc123", searchSource, searchType));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);

            // 自动识别音乐提供商和类型
            searchSource = SearchSourceEnum.QQ_MUSIC;

            var res = CheckInputId("https://music.163.com/#/song?id=1815969317", searchSource, searchType);
            Assert.AreEqual("1815969317", res.QueryId);
            Assert.AreEqual(SearchSourceEnum.NET_EASE_MUSIC, res.SearchSource);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, res.SearchType);
            
            // 专辑提取
            res = CheckInputId("https://music.163.com/#/album?id=122305109", searchSource, searchType);
            Assert.AreEqual("122305109", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.ALBUM_ID, res.SearchType);
            
            // 歌单提取
            res = CheckInputId("https://music.163.com/#/playlist?id=7050074027", searchSource, searchType);
            Assert.AreEqual("7050074027", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.PLAYLIST_ID, res.SearchType);
            
            // URL 存在冗余参数
            res = CheckInputId("https://music.163.com/song?id=1431606759&userid=8073630663", searchSource, searchType);
            Assert.AreEqual("1431606759", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, res.SearchType);
        }

        [Test]
        public void TestCheckInputIdWithQQMusic()
        {
            // 支持非数字
            Assert.AreEqual("abc", CheckInputId("abc", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID).QueryId);
            
            // 自动识别音乐提供商和类型
            var res = CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOq052wu9", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID);
            Assert.AreEqual("002owtOq052wu9", res.QueryId);
            Assert.AreEqual(SearchSourceEnum.QQ_MUSIC, res.SearchSource);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, res.SearchType);
            
            // 专辑提取
            res = CheckInputId("https://y.qq.com/n/ryqq/albumDetail/000k0h474UtgAL", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID);
            Assert.AreEqual("000k0h474UtgAL", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.ALBUM_ID, res.SearchType);
            
            // 歌单提取
            res = CheckInputId("https://y.qq.com/n/ryqq/playlist/8694686726", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID);
            Assert.AreEqual("8694686726", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.PLAYLIST_ID, res.SearchType);
            
            // URL 存在冗余参数
            res = CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOQQ052wu9&userid=8073630663", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID);
            Assert.AreEqual("002owtOQQ052wu9", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, res.SearchType);
            
            // 歌曲短链接支持
            res = CheckInputId("https://c.y.qq.com/base/fcgi-bin/u?__=0S6Bg5scnbuj", SearchSourceEnum.NET_EASE_MUSIC,
                SearchTypeEnum.SONG_ID);
            Assert.AreEqual("385359965", res.QueryId);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, res.SearchType);
            
            // 不支持的 URL
            Assert.Throws(typeof(MusicLyricException), () => CheckInputId("https://y.qq.com/n/ryqq/singer/004cyCLc1ByKPx", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
        }

        [Test]
        public void ResolveCustomFunctionTest()
        {
            // 方法未找到
            Assert.AreEqual("2$fillLength(${index},0,322", ResolveCustomFunction("2$fillLength(${index},0,322"));
            // 参数数量不匹配
            Assert.AreEqual("2$fillLength(${index},32)2", ResolveCustomFunction("2$fillLength(${index},32)2"));
            // 不需要补齐
            Assert.AreEqual("2${index}2", ResolveCustomFunction("2$fillLength(${index},0,2)2"));
            // 补齐成功
            Assert.AreEqual("20${index}2", ResolveCustomFunction("2$fillLength(${index},0,9)2"));
            // 截位补齐
            Assert.AreEqual("22${index}2", ResolveCustomFunction("2$fillLength(${index},20,9)2"));
        }
        
        [Test]
        public void GetOutputNameTest()
        {
            var songVo = new SongVo
            {
                Name = "name0",
                Singer = new [] {"singer1", "singer2"},
                Album = "album2",
                DisplayId = "id3",
                Id = "id4",
            };
            
            var saveVo = new SaveVo(10, songVo, null);
            
            Assert.AreEqual("name0 - singer1,singer2", GetOutputName(saveVo, "${name} - ${singer}", ","));
            Assert.AreEqual("10 & name0 p id3 @ name0 - singer1,singer2 x album2", GetOutputName(saveVo, 
                "${index} & ${name} p ${id} @ ${name} - ${singer} x ${album}", ","));
            Assert.AreEqual("$name - $singer", GetOutputName(saveVo, "$name - $singer", ","));
        }
    }
}