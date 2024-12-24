using System;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Music
{
    [TestFixture]
    public class NetEaseMusicNativeApi
    {
        private MusicLyricApp.Api.Music.NetEaseMusicNativeApi _api = new MusicLyricApp.Api.Music.NetEaseMusicNativeApi(() => "");
        
        [Test]
        public void TestGetAlbum()
        {
            _api.GetAlbum("148191532");
        }
        
        [Test]
        public void TestSearchSong()
        {
            var res = _api.Search("慰问", SearchTypeEnum.SONG_ID);
            
            Assert.AreEqual(ErrorMsg.NEED_LOGIN, res.ErrorMsg);
            Assert.IsNull(res.Data);
        }
        
        [Test]
        public void TestSearchPlaylist()
        {
            var res = _api.Search("慰问", SearchTypeEnum.PLAYLIST_ID);
            
            Assert.AreEqual(ErrorMsg.NEED_LOGIN, res.ErrorMsg);
            Assert.IsNull(res.Data);
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
        public void TestGetSongs()
        {
            var res = _api.GetSongs(new[] {"1987814537"});
            Assert.AreEqual(1, res.Count);
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