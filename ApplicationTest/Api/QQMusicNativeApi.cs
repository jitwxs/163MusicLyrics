using NUnit.Framework;

namespace ApplicationTest.Api
{
    [TestFixture]
    public class QQMusicNativeApi
    {
        private Application.Api.QQMusicNativeApi _api = new Application.Api.QQMusicNativeApi();
        
        [Test]
        public void GetSong()
        {
            _api.GetSong("003KDOb01SUyD5");
        }
        
        [Test]
        public void GetLyric()
        {
            _api.GetLyric("003KDOb01SUyD5");
        }
        
        [Test]
        public void GetAlbum()
        {
            _api.GetAlbum("000IdMCY2pTAiz");
        }
    }
}