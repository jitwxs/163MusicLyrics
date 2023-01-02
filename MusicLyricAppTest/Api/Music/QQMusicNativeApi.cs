using System;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Music
{
    [TestFixture]
    public class QQMusicNativeApi
    {
        private MusicLyricApp.Api.Music.QQMusicNativeApi _api = new MusicLyricApp.Api.Music.QQMusicNativeApi(() => "");
        
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
            var lyricResult = _api.GetLyric("003KDOb01SUyD5");
            Assert.NotNull(lyricResult);
        }
        
        [Test]
        public void TestPureGetLyric()
        {
            var lyricResult = _api.GetLyric("003CqWeL2ruZlS");
            Assert.AreEqual(0, lyricResult.Code);

            var lyricVo = lyricResult.ToVo();
            Assert.False(lyricVo.IsEmpty());
            Assert.True(lyricVo.IsPureMusic());
        }
        
        [Test]
        public void GetAlbum()
        {
            var resp = _api.GetAlbum("000IdMCY2pTAiz");
            Console.WriteLine(resp);
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


        [Test]
        public void TestSearch()
        {
            var res = _api.Search("慰问", SearchTypeEnum.PLAYLIST_ID);
            Console.WriteLine(res);
        }
    }
}