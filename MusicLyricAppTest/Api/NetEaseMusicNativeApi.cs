using System;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Api
{
    [TestFixture]
    public class NetEaseMusicNativeApi
    {
        private MusicLyricApp.Api.NetEaseMusicNativeApi _api = new MusicLyricApp.Api.NetEaseMusicNativeApi();
        
        [Test]
        public void GetAlbum()
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
    }
}