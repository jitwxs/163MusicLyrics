using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using 网易云歌词提取;
using static 网易云歌词提取.NetEaseMusicUtils;

namespace _163MusicLyrics_Test
{
    [TestFixture]
    public class NetEaseMusicUtilsTest
    {
        private const string LYRICS_TEST_DATA =
            "{\"Sgc\":false,\"Sfy\":true,\"Qfy\":true,\"Nolyric\":false," +
            "\"Uncollected\":false,\"TransUser\":null," +
            "\"LyricUser\":{\"Id\":3651114,\"Status\":99,\"Demand\":0,\"Userid\":85756151,\"Nickname\":\"星尘TwinkleStar\",\"Uptime\":0}," +
            "\"Lrc\":{\"Version\":36,\"Lyric\":\"[00:00.000] 作词 : 柚桐\n[00:01.000] 作曲 : 石页\n[00:10.35]如果星星还记得\n[00:14.78]编曲：石页\n[00:17.75]电吉他：石页\n[00:25.81]房瓦上停留着从何处飞来的寒鸦\n[00:32.21]零碎夜晚陪谁看烟火刹那\n[00:37.33]房前摆上一盆永不枯萎的花\n[00:41.46]静听 熄灭了灯火的人家\n[00:46.85]看夜空中星星多美丽闪烁着点点微光\n[00:52.03]等谁再说起那记忆中的年华\n[00:57.38]脚步太过沉重才会让人心灵那般拖沓\n[01:03.06]如果星星能记得的话\n[01:09.87]手边放着旧时候的那些泛黄书札\n[01:14.64]森林中密布盛开美丽繁花\n[01:19.62]看星星有多美听着流水哗啦\n[01:23.74]轻哼 青春唱过谁的童话\n[01:29.65]懵懂的年华我们也许总是在想着长大\n[01:34.89]随曾经一尘不变的记忆落下\n[01:40.24]只是想追寻昔日执着至今的小小步伐\n[01:45.88]如果星星能记得的话\n\"}," +
            "\"Klyric\":{\"Version\":0,\"lyric\":\"\"},\"Tlyric\":{\"Version\":0,\"Lyric\":\"\"},\"Code\":200}";

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
        public void CheckInputIdAndInputIsNuber()
        {
            Assert.AreEqual(1, CheckInputId("1", out var output_1));
            Assert.AreEqual(ErrorMsg.SUCCESS, output_1);
            Assert.AreEqual(0, NetEaseMusicUtils.CheckInputId(string.Empty, out var output_2));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAG, output_2);
            Assert.AreEqual(0, NetEaseMusicUtils.CheckInputId(null, out var output_3));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAG, output_3);
        }

        [Test]
        public void CheckInputIdAndInputNotIsNuber()
        {
            Assert.AreEqual(0, CheckInputId("abc", out var output_1));
            Assert.AreEqual(ErrorMsg.INPUT_ID_ILLEGAG, output_1);
        }

        [Test]
        public void GetLyricVoThrowException()
        {
            var search = new SearchInfo();
            var lyrics = new LyricResult();

            //等合并后
            //Assert.Throws<ArgumentNullException>(() => GetLyricVo(null, 0, null, out _));
            //Assert.Throws<ArgumentNullException>(() => GetLyricVo(null, 0, search, out _));
            //Assert.Throws<ArgumentNullException>(() => GetLyricVo(lyrics, 0, null, out _));
            Assert.DoesNotThrow(() => GetLyricVo(lyrics, 0, search, out _));
        }

        [Test]
        public void GetLyricVoResult()
        {
            //var rawLyrics = JsonConvert.DeserializeObject<LyricResult>(TEST_DATA);
            //var lyricVo = GetLyricVo(rawLyrics, 0, null, out _);
        }

        [Test]
        public void GetOutputNameTest()
        {
            var songVo = new SongVo()
            {
                Name = "name",
                Singer = "singer"
            };
            Assert.AreEqual("name - singer", 
                GetOutputName(songVo, new SearchInfo() { OutputFileNameType = OUTPUT_FILENAME_TYPE_ENUM.NAME_SINGER }));
            Assert.AreEqual("singer - name", 
                GetOutputName(songVo, new SearchInfo() { OutputFileNameType = OUTPUT_FILENAME_TYPE_ENUM.SINGER_NAME }));
            Assert.AreEqual("name",
                GetOutputName(songVo, new SearchInfo() { OutputFileNameType = OUTPUT_FILENAME_TYPE_ENUM.NAME }));
        }

        [Test]
        public void ContractSingerTest()
        {
            var rawDetails = JsonConvert.DeserializeObject<DetailResult>(DETAILS_TEST_DATA);

            var details = new DetailResult();
            details.Songs = new Song[1];
            details.Songs[0] = new Song() { Ar = new List<Ar>() };
            details.Songs[0].Ar.Add(new Ar() { Name = "name-1"});
            details.Songs[0].Ar.Add(new Ar() { Name = "name-2"});

            Assert.AreEqual("石页", ContractSinger(rawDetails.Songs[0].Ar));
            Assert.AreEqual("name-1,name-2", ContractSinger(details.Songs[0].Ar));
            Assert.AreEqual(string.Empty, ContractSinger(new List<Ar>()));
        }

        [Test]
        public void ContractSingerException()
        {
            Assert.Throws<ArgumentNullException>(() => ContractSinger(null));
        }
    }
}