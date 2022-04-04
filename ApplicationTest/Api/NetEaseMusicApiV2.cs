using System.Collections.Generic;
using Application.Bean;
using Application.Utils;
using NUnit.Framework;
using static Application.Api.NetEaseMusicApiV2;

namespace ApplicationTest.Api
{
    [TestFixture]
    public class NetEaseMusicApiV2
    {
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
        public void ContractSingerTest()
        {
            var rawDetails = DETAILS_TEST_DATA.ToEntity<DetailResult>();

            var details = new DetailResult();
            details.Songs = new Song[1];
            details.Songs[0] = new Song() { Ar = new List<Ar>() };
            details.Songs[0].Ar.Add(new Ar() { Name = "name-1" });
            details.Songs[0].Ar.Add(new Ar() { Name = "name-2" });

            Assert.AreEqual("石页", ContractSinger(rawDetails.Songs[0].Ar));
            Assert.AreEqual("name-1,name-2", ContractSinger(details.Songs[0].Ar));
            Assert.AreEqual(string.Empty, ContractSinger(new List<Ar>()));
        }
    }
}