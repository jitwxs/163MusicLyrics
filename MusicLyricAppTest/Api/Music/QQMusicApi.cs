using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Music
{
    [TestFixture]
    public class QQMusicApiV2
    {
        private MusicLyricApp.Api.Music.QQMusicApi _api = new MusicLyricApp.Api.Music.QQMusicApi(() => "");
        
        [Test]
        public void Test()
        {
            _api.GetAlbumVo("000IdMCY2pTAiz");
        }
        
        [Test]
        public void TestGetPlaylist()
        {
            const string playlistId = "8694686726";
            
            var res = _api.GetPlaylistVo(playlistId);
            
            Assert.True(res.IsSuccess());

            var resData = res.Data;

            var cacheData = GlobalCache.Query<PlaylistVo>(SearchSourceEnum.QQ_MUSIC, CacheType.PLAYLIST_VO, playlistId);
            
            // playlistVo 正确缓存
            Assert.AreEqual(resData, cacheData);
            
            foreach (var simpleSongVo in resData.SimpleSongVos)
            {
                // song 正确缓存
                Assert.NotNull(GlobalCache.Query<QQMusicBean.Song>(SearchSourceEnum.QQ_MUSIC, CacheType.QQ_MUSIC_SONG, simpleSongVo.Id));
                Assert.NotNull(GlobalCache.Query<QQMusicBean.Song>(SearchSourceEnum.QQ_MUSIC, CacheType.QQ_MUSIC_SONG, simpleSongVo.DisplayId));
            }
        }
    }
}