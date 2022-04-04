using System;
using NUnit.Framework;

namespace MusicLyricAppTest.Api
{
    [TestFixture]
    public class QQMusicNativeApi
    {
        private MusicLyricApp.Api.QQMusicNativeApi _api = new MusicLyricApp.Api.QQMusicNativeApi();
        
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
        
        [Test]
        public void GetSongLink()
        {
            var link = _api.GetSongLink("0028gDJg3aMcO1");
            Console.WriteLine(link);
        }
    }
}