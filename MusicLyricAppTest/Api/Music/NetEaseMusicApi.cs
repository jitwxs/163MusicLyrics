using System.Collections.Generic;
using System.Linq;
using MusicLyricApp.Bean;
using MusicLyricApp.Cache;
using MusicLyricApp.Utils;
using NUnit.Framework;

namespace MusicLyricAppTest.Api.Music
{
    [TestFixture]
    public class NetEaseMusicApiV2
    {
        private MusicLyricApp.Api.Music.NetEaseMusicApi _api = new MusicLyricApp.Api.Music.NetEaseMusicApi(() => "");
        
        private const string DETAILS_TEST_DATA =
            "{\"Songs\":[{\"Name\":\"如果星星能记得(Vocaloid)\",\"Id\":447309968,\"Pst\":0,\"T\":0," +
            "\"Ar\":[{\"Id\":12200045,\"Name\":\"石页\",\"Tns\":[],\"Alias\":[]}],\"Alia\":[],\"Pop\":75.0,\"St\":0," +
            "\"Rt\":null,\"Fee\":0,\"V\":103,\"Crbt\":null,\"Cf\":\"\",\"Al\":{\"Id\":35060085,\"Name\":\"石页的V家曲\"," +
            "\"PicUrl\":\"https://p4.music.126.net/CUMANQGhUXEjB-Db1CvSwQ==/109951162824061491.jpg\",\"Tns\":[]," +
            "\"Pic\":109951162824061491},\"Dt\":157506,\"H\":{\"Br\":320000,\"Fid\":0,\"Size\":6302868,\"Vd\":-11799.0}," +
            "\"M\":{\"Br\":192000,\"Fid\":0,\"Size\":3781738,\"Vd\":-9400.0},\"L\":{\"Br\":128000,\"Fid\":0," +
            "\"Size\":2521173,\"Vd\":-8100.0},\"A\":null,\"Cd\":\"01\",\"No\":1,\"RtUrl\":null,\"Ftype\":0," +
            "\"RtUrls\":[],\"Rurl\":null,\"Rtype\":0,\"Mst\":9,\"Cp\":0,\"Mv\":0,\"PublishTime\":1481880909423," +
            "\"Privilege\":null}],\"Privileges\":[{\"Id\":447309968,\"Fee\":0,\"Payed\":0,\"St\":0,\"Pl\":320000," +
            "\"Dl\":999000,\"Sp\":7,\"Cp\":1,\"Subp\":1,\"Cs\":false,\"Maxbr\":999000,\"Fl\":320000,\"Toast\":false," +
            "\"Flag\":2}],\"Code\":200}";
        
        [Test]
        public void TestContractSinger()
        {
            var rawDetails = DETAILS_TEST_DATA.ToEntity<DetailResult>();

            var details = new DetailResult();
            details.Songs = new Song[1];
            details.Songs[0] = new Song() { Ar = new List<Ar>() };
            details.Songs[0].Ar.Add(new Ar() { Name = "name-1" });
            details.Songs[0].Ar.Add(new Ar() { Name = "name-2" });

            Assert.AreEqual("石页", string.Join(",", rawDetails.Songs[0].Ar.Select(e => e.Name)));
            Assert.AreEqual("name-1,name-2", string.Join(",", details.Songs[0].Ar.Select(e => e.Name)));
            Assert.AreEqual(string.Empty, string.Join(",",new List<Ar>().Select(e => e.Name)));
        }

        [Test]
        public void Test()
        {
            _api.GetAlbumVo("148191532");
        }
        
        [Test]
        public void TestGetPlaylist()
        {
            const string playlistId = "7050074027";
            const SearchSourceEnum searchSource = SearchSourceEnum.NET_EASE_MUSIC;
            
            var res = _api.GetPlaylistVo(playlistId);
            
            Assert.True(res.IsSuccess());

            var resData = res.Data;

            var cacheData = GlobalCache.Query<PlaylistVo>(searchSource, CacheType.PLAYLIST_VO, playlistId);
            
            // playlistVo 正确缓存
            Assert.AreEqual(resData, cacheData);
            
            foreach (var simpleSongVo in resData.SimpleSongVos)
            {
                // song 正确缓存
                Assert.NotNull(GlobalCache.Query<Song>(searchSource, CacheType.NET_EASE_SONG, simpleSongVo.Id));
                Assert.NotNull(GlobalCache.Query<Song>(searchSource, CacheType.NET_EASE_SONG, simpleSongVo.DisplayId));
            }
        }
    }
}