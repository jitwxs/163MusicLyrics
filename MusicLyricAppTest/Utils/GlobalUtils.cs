using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NUnit.Framework;
using static MusicLyricApp.Utils.GlobalUtils;

namespace MusicLyricAppTest.Utils
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
            Assert.AreEqual("1815969317",
                CheckInputId("https://music.163.com/#/song?id=1815969317", SearchSourceEnum.NET_EASE_MUSIC,
                    SearchTypeEnum.SONG_ID));
            Assert.AreEqual("122305109",
                CheckInputId("https://music.163.com/#/album?id=122305109", SearchSourceEnum.NET_EASE_MUSIC,
                    SearchTypeEnum.ALBUM_ID));

            Assert.AreEqual("002owtOq052wu9",
                CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOq052wu9", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.SONG_ID));
            Assert.AreEqual("000k0h474UtgAL",
                CheckInputId("https://y.qq.com/n/ryqq/albumDetail/000k0h474UtgAL", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.ALBUM_ID));

            Assert.Throws(typeof(MusicLyricException),
                () => CheckInputId("https://y.qq.com/n/ryqq/singer/004cyCLc1ByKPx", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.SONG_ID));
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

        [Test]
        public void TestTimestampToLong()
        {
            // 不合法
            Assert.AreEqual(-1, TimestampStrToLong(""));
            Assert.AreEqual(-1, TimestampStrToLong("[12131"));
            Assert.AreEqual(-1, TimestampStrToLong("-1]"));
            
            // 合法
            Assert.AreEqual(62 * 1000 + 3, TimestampStrToLong("[1:2.3]"));
            Assert.AreEqual((12 * 60 + 59) * 1000 + 311, TimestampStrToLong("[12:59.311]"));
            
            // [00:39.17]
            Assert.AreEqual(39 * 1000 + 17, TimestampStrToLong("[00:39.17]"));
        }

        [Test]
        public void TestTimestampLongToStr()
        {
            // 非法
            Assert.Throws<MusicLyricException>(() => TimestampLongToStr(-1, "00"));
            
            Assert.AreEqual("[00:00.000]", TimestampLongToStr(0, "000"));
            
            Assert.AreEqual("[00:00.00]", TimestampLongToStr(0, "00"));
            
            Assert.AreEqual("[01:02.003]", TimestampLongToStr(62 * 1000 + 3, "000"));
            
            Assert.AreEqual("[01:02.03]", TimestampLongToStr(62 * 1000 + 3, "00"));
            
            Assert.AreEqual("[12:59.311]", TimestampLongToStr((12 * 60 + 59) * 1000 + 311, "000"));
            
            Assert.AreEqual("[12:59.0311]", TimestampLongToStr((12 * 60 + 59) * 1000 + 311, "0000"));
        }
    }
}