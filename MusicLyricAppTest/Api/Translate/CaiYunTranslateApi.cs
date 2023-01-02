using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Translate
{
    [TestFixture]
    public class CaiYunTranslateApi
    {
        private MusicLyricApp.Api.Translate.CaiYunTranslateApi _api =
            new MusicLyricApp.Api.Translate.CaiYunTranslateApi("please replace your caiYun token");
        
        [Test]
        public void TestTranslate()
        {
            // 鉴权失败
            Assert.Throws(typeof(MusicLyricException),
                () => _api.Translate(new[] { "你好世界" }, LanguageEnum.CHINESE, LanguageEnum.ENGLISH));

            // var result1 = _api.Translate(new[] {"你好世界"}, LanguageEnum.CHINESE, LanguageEnum.ENGLISH);
            // Assert.AreEqual(1, result1.Length);
            //
            // var result2 = _api.Translate(new[] {"Hello World", "Translate English"}, LanguageEnum.ENGLISH, LanguageEnum.CHINESE);
            // Assert.AreEqual(2, result2.Length);
        }
    }
}