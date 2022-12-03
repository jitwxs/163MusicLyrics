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
            var res1 = _api.GetSong("204422870");
            var res2= _api.GetSong("001RaE0n4RrGX9");
            
            Assert.AreEqual(res1.Code, res2.Code);
            Assert.AreEqual(res1.Data.Length, res2.Data.Length);
            Assert.AreEqual(res1.Data[0].Id, res2.Data[0].Id);
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
        
        [Test]
        public void TestGetPlaylist()
        {
            var res = _api.GetPlaylist("8694686726");
            
            Console.WriteLine(res);
        }
    }
}