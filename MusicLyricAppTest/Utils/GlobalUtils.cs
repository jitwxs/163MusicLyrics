using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NUnit.Framework;
using static MusicLyricApp.Utils.GlobalUtils;

namespace ApplicationTest.Utils
{
    [TestFixture]
    public class GlobalUtils
    {
        [Test]
        public void TestCheckInputIdWithNumber()
        {
            Assert.AreEqual("1", CheckInputId("1", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            
            var exception = Assert.Throws(typeof(MusicLyricException), 
                () => CheckInputId(string.Empty, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);
            
            exception = Assert.Throws(typeof(MusicLyricException), 
                () => CheckInputId(null, SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);
        }

        [Test]
        public void TestCheckInputIdWithWord()
        {
            var exception = Assert.Throws(typeof(MusicLyricException), 
                () => CheckInputId("abc", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAL, exception.Message);
            
            Assert.AreEqual("abc", CheckInputId("abc", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID));
        }

        [Test]
        public void TestCheckInputIdWithUrl()
        {
            Assert.AreEqual("1815969317", CheckInputId("https://music.163.com/#/song?id=1815969317", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual("122305109", CheckInputId("https://music.163.com/#/album?id=122305109", SearchSourceEnum.NET_EASE_MUSIC, SearchTypeEnum.ALBUM_ID));
            
            Assert.AreEqual("002owtOq052wu9", CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOq052wu9", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.SONG_ID));
            Assert.AreEqual("000k0h474UtgAL", CheckInputId("https://y.qq.com/n/ryqq/albumDetail/000k0h474UtgAL", SearchSourceEnum.QQ_MUSIC, SearchTypeEnum.ALBUM_ID));
        }
    }
}