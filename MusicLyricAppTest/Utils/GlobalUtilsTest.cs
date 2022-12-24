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
            var paramBean = new PersistParamBean();

            var exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId(string.Empty, paramBean));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);

            exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId(null, paramBean));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);
        }
        
        [Test]
        public void TestCheckInputIdWithNetEase()
        {
            var paramBean = new PersistParamBean
            {
                SearchSource = SearchSourceEnum.NET_EASE_MUSIC
            };

            // 纯数字
            Assert.AreEqual("1", CheckInputId("1", paramBean));
            
            // 不支持非数字
            var exception = Assert.Throws(typeof(MusicLyricException), () => CheckInputId("abc123", paramBean));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);

            // 自动识别音乐提供商和类型
            paramBean.SearchSource = SearchSourceEnum.QQ_MUSIC;
            Assert.AreEqual("1815969317", CheckInputId("https://music.163.com/#/song?id=1815969317", paramBean));
            Assert.AreEqual(SearchSourceEnum.NET_EASE_MUSIC, paramBean.SearchSource);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, paramBean.SearchType);
            
            // 专辑提取
            Assert.AreEqual("122305109", CheckInputId("https://music.163.com/#/album?id=122305109", paramBean));
            Assert.AreEqual(SearchTypeEnum.ALBUM_ID, paramBean.SearchType);
            
            // 歌单提取
            Assert.AreEqual("7050074027", CheckInputId("https://music.163.com/#/playlist?id=7050074027", paramBean));
            Assert.AreEqual(SearchTypeEnum.PLAYLIST_ID, paramBean.SearchType);
            
            // URL 存在冗余参数
            Assert.AreEqual("1431606759", CheckInputId("https://music.163.com/song?id=1431606759&userid=8073630663", paramBean));
            Assert.AreEqual(SearchTypeEnum.SONG_ID, paramBean.SearchType);
        }

        [Test]
        public void TestCheckInputIdWithQQMusic()
        {
            var paramBean = new PersistParamBean
            {
                SearchSource = SearchSourceEnum.QQ_MUSIC
            };
            
            // 支持非数字
            Assert.AreEqual("abc", CheckInputId("abc", paramBean));
            
            // 自动识别音乐提供商和类型
            paramBean.SearchSource = SearchSourceEnum.NET_EASE_MUSIC;
            Assert.AreEqual("002owtOq052wu9", CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOq052wu9", paramBean));
            Assert.AreEqual(SearchSourceEnum.QQ_MUSIC, paramBean.SearchSource);
            Assert.AreEqual(SearchTypeEnum.SONG_ID, paramBean.SearchType);
            
            // 专辑提取
            Assert.AreEqual("000k0h474UtgAL", CheckInputId("https://y.qq.com/n/ryqq/albumDetail/000k0h474UtgAL", paramBean));
            Assert.AreEqual(SearchTypeEnum.ALBUM_ID, paramBean.SearchType);
            
            // 歌单提取
            Assert.AreEqual("8694686726", CheckInputId("https://y.qq.com/n/ryqq/playlist/8694686726", paramBean));
            Assert.AreEqual(SearchTypeEnum.PLAYLIST_ID, paramBean.SearchType);
            
            // URL 存在冗余参数
            Assert.AreEqual("002owtOQQ052wu9", CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOQQ052wu9&userid=8073630663", paramBean));
            Assert.AreEqual(SearchTypeEnum.SONG_ID, paramBean.SearchType);
            
            // 歌曲短链接支持
            Assert.AreEqual("385359965", CheckInputId("https://c.y.qq.com/base/fcgi-bin/u?__=0S6Bg5scnbuj", paramBean));
            Assert.AreEqual(SearchTypeEnum.SONG_ID, paramBean.SearchType);
            
            // 不支持的 URL
            Assert.Throws(typeof(MusicLyricException), () => CheckInputId("https://y.qq.com/n/ryqq/singer/004cyCLc1ByKPx", paramBean));
        }

        [Test]
        public void GetOutputNameTest()
        {
            var songVo = new SongVo()
            {
                Name = "name",
                Singer = "singer"
            };
            Assert.AreEqual("name - singer", GetOutputName(songVo, OutputFilenameTypeEnum.NAME_SINGER));
            Assert.AreEqual("singer - name", GetOutputName(songVo, OutputFilenameTypeEnum.SINGER_NAME));
            Assert.AreEqual("name", GetOutputName(songVo, OutputFilenameTypeEnum.NAME));
        }
    }
}