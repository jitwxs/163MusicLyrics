using System;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Api
{
    [TestFixture]
    public class NetEaseMusicNativeApi
    {
        private MusicLyricApp.Api.NetEaseMusicNativeApi _api = new MusicLyricApp.Api.NetEaseMusicNativeApi(() => "");
        
        [Test]
        public void TestGetAlbum()
        {
            _api.GetAlbum("148191532");
        }
        
        [Test]
        public void TestSearch()
        {
            var res = _api.Search("慰问", SearchTypeEnum.PLAYLIST_ID);
            
            Assert.AreNotEqual(0, res.PlaylistCount);
        }

        [Test]
        public void TestGetPlaylist()
        {
            var res = _api.GetPlaylist("7050074027");
            
            Console.WriteLine(res);
        }

        [Test]
        public void TestGetLyric()
        {
            var lyricResult = _api.GetLyric("1987814537");
            Assert.AreEqual(200L, lyricResult.Code);
        }

        [Test]
        public void TestPureGetLyric()
        {
            var lyricResult = _api.GetLyric("549702604");
            Assert.AreEqual(200L, lyricResult.Code);

            var lyricVo = lyricResult.ToVo();
            Assert.False(lyricVo.IsEmpty());
            Assert.True(lyricVo.IsPureMusic());
        }
    }
}