using Application.Bean;
using Application.Exception;
using NUnit.Framework;
using static Application.Utils.GlobalUtils;

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
    }
}