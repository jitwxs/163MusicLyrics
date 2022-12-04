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
            var res = _api.Search("Slow", SearchTypeEnum.SONG_ID);
            
            Assert.AreNotEqual(0, res.Result.SongCount);
            
            res = _api.Search("Slow", SearchTypeEnum.ALBUM_ID);
            
            Assert.AreNotEqual(0, res.Result.AlbumCount);
        }

        [Test]
        public void TestGetPlaylist()
        {
            var res = _api.GetPlaylist("7050074027");
            
            Console.WriteLine(res);
        }
    }
}