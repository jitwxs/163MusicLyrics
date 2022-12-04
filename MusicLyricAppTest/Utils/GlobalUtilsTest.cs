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
            Assert.AreEqual("7050074027",
                CheckInputId("https://music.163.com/#/playlist?id=7050074027", SearchSourceEnum.NET_EASE_MUSIC,
                    SearchTypeEnum.PLAYLIST_ID));

            Assert.AreEqual("002owtOq052wu9",
                CheckInputId("https://y.qq.com/n/ryqq/songDetail/002owtOq052wu9", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.SONG_ID));
            Assert.AreEqual("000k0h474UtgAL",
                CheckInputId("https://y.qq.com/n/ryqq/albumDetail/000k0h474UtgAL", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.ALBUM_ID));
            Assert.AreEqual("8694686726",
                CheckInputId("https://y.qq.com/n/ryqq/playlist/8694686726", SearchSourceEnum.QQ_MUSIC,
                    SearchTypeEnum.PLAYLIST_ID));

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
    }
}