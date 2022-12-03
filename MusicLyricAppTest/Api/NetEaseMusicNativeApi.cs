using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Api
{
    [TestFixture]
    public class NetEaseMusicNativeApi
    {
        private MusicLyricApp.Api.NetEaseMusicNativeApi _api = new MusicLyricApp.Api.NetEaseMusicNativeApi();
        
        [Test]
        public void TestSearch()
        {
            var res = _api.Search("Slow", SearchTypeEnum.SONG_ID);
            
            Assert.AreNotEqual(0, res.Result.SongCount);
            
            res = _api.Search("Slow", SearchTypeEnum.ALBUM_ID);
            
            Assert.AreNotEqual(0, res.Result.AlbumCount);
        }
    }
}