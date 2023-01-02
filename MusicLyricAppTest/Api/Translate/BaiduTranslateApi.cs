using MusicLyricApp.Bean;
using MusicLyricApp.Exception;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Translate
{
    [TestFixture]
    public class BaiduTranslateApi
    {
        private MusicLyricApp.Api.Translate.BaiduTranslateApi _api =
            new MusicLyricApp.Api.Translate.BaiduTranslateApi("please replace your baidu appId", "please replace your baidu secret");
            
        [Test]
        public void TestTranslate()
        {
            // 鉴权失败
            Assert.Throws(typeof(MusicLyricException), () => _api.Translate(new[]
            {
                "你好世界", 
                "北京欢迎你"
            }, LanguageEnum.CHINESE, LanguageEnum.ENGLISH));
        }
    }
}