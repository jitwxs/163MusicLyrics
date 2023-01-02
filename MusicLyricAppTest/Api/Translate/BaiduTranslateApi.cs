using MusicLyricApp.Bean;
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
            var resultVo = _api.Translate(new[]
            {
                "你好世界", 
                "北京欢迎你"
            }, LanguageEnum.CHINESE, LanguageEnum.ENGLISH);
            Assert.NotNull(resultVo);
        }
    }
}