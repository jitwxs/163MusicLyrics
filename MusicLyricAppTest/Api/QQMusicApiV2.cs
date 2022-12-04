using NUnit.Framework;

namespace MusicLyricAppTest.Api
{
    [TestFixture]
    public class QQMusicApiV2
    {
        private MusicLyricApp.Api.QQMusicApiV2 _api = new MusicLyricApp.Api.QQMusicApiV2();
        
        [Test]
        public void Test()
        {
            _api.GetAlbumVo("000IdMCY2pTAiz");
        }
    }
}