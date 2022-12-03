using System.Text;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Bean
{
    [TestFixture]
    public class MusicLyricsVoTest
    {
        [Test]
        public void TestLyricLineVo()
        {
            var scenario1 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");
            
            Assert.AreEqual("[00:12.53]", scenario1.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario2 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");
            
            Assert.AreEqual("[00:12.52]", scenario2.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.DOWN));
            
            var scenario3 = new LyricLineVo("[00:12.526]綺麗な嘘ごと自分を騙し続けて");

            Assert.AreEqual("[00:12.526]", scenario3.Timestamp.PrintTimestamp("[mm:ss.SSS]", DotTypeEnum.DOWN));
            
            var scenario4 = new LyricLineVo("[00:12.523]綺麗な嘘ごと自分を騙し続けて");

            Assert.AreEqual("[00:12.52]", scenario4.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario5 = new LyricLineVo("[00:00.000] 作词 : Kirara Magic");

            Assert.AreEqual("[00:00.00]", scenario5.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
            
            var scenario6 = new LyricLineVo("[01:10.050] 作词 : Kirara Magic");

            Assert.AreEqual("[01:10.05]", scenario6.Timestamp.PrintTimestamp("[mm:ss.SS]", DotTypeEnum.HALF_UP));
        }

        [Test]
        public void TestLyricTimestamp()
        {
            var timestamp = new LyricTimestamp("[End]");
            Assert.AreEqual(0, timestamp.TimeOffset);
        }

        [Test]
        public void TestLyricLineVoSplit1()
        {
            var vo = new LyricLineVo("た", new LyricTimestamp(0));
            
            var voList = LyricLineVo.Split(vo);

            Assert.AreEqual(1, voList.Count);
            
            Assert.AreEqual(vo.Print("[mm:ss.SSS]", DotTypeEnum.DOWN), voList[0].Print("[mm:ss.SSS]", DotTypeEnum.DOWN));
        }

        [Test]
        public void TestLyricLineVoSplit2()
        {
            var vo = new LyricLineVo("た[00:22.153]1な[00:23.113]ぎ[00:23.417]さ[00:23.721]1い[00:24.936]ま[00:25.176]1お" +
                            "[00:25.690]も[00:26.186]1だ1す[00:29.925]な[00:30.173]1う[00:30.781]え[00:31.166]1き" +
                            "[00:32.151]ざ[00:32.421]1こ[00:33.392]と[00:33.696]1ば1き[00:34.928]み[00:35.031]1う[00:35.673]し" +
                            "[00:36.192]1す[00:37.096]が[00:37.400]た[00:37.896]1よ1か[00:40.601]え[00:40.849]1な[00:41.785]み" +
                            "[00:42.097]1あ[00:43.392]し[00:43.609]1も[00:43.872]と[00:44.288]1な[00:45.872]に[00:46.216]1さ" +
                            "[00:47.112]ら[00:47.448]1ゆ[00:49.912]う[00:49.947]1な[00:50.622]ぎ[00:51.476]1な[00:52.099]か" +
                            "[00:52.541]1ひ1ぐ1とお1す1い1ひ[01:00.309]か[01:00.573]1さ1は[01:02.414]な[01:02.686]1び1み1お1な" +
                            "[01:07.478]つ[01:07.524]1あ[01:09.837]い[01:09.917]1ま[01:10.261]い[01:10.286]1こ[01:10.853]こ" +
                            "[01:11.174]ろ[01:11.341]1と1つ[01:13.365]な[01:13.677]1よ[01:15.202]る[01:15.378]1つ[01:16.170]づ" +
                            "[01:16.218]1ほ1なん1ど1き[01:31.082]み[01:31.266]1お[01:31.540]な[01:31.812]1はな1び1み1わ[01:34.911]ら" +
                            "[01:34.986]1か[01:35.402]お[01:35.569]1な[01:35.934]に[01:36.198]1き[01:40.223]ず[01:40.277]1よ" +
                            "[01:41.450]ろ[01:41.503]こ[01:41.800]1く1か[01:43.044]え[01:43.110]1な[01:43.558]み" +
                            "[01:43.642]1じょう1どう1しょう1そう1さ[01:45.781]い[01:45.873]1しゅう1れっ1しゃ1お[01:47.931]と" +
                            "[01:48.507]1なん1ど1こ[01:52.411]と[01:52.443]1ば1き[01:53.870]み[01:53.907]1よ1な[01:55.068]み" +
                            "[01:55.099]1ま1え[01:55.809]ら[01:55.823]1い[01:56.823]ち[01:56.861]1ど1に1ど1か[02:02.344]な" +
                            "[02:02.512]1す1い[02:17.761]き[02:18.081]1の1き1ひ[02:21.133]か[02:21.310]り[02:21.477]1む" +
                            "[02:23.667]ね[02:23.856]1す1て1の1ふ1み1ら[02:31.190]い[02:31.374]1ふた1り1み1はな1び1よ[02:40.839]る" +
                            "[02:41.103]1さ1よ[02:43.372]る[02:43.588]1さ1し[02:45.829]ず[02:45.903]1き1は[02:48.269]な[02:48.533]1は" +
                            "[02:49.727]な[02:49.852]1す[02:51.134]こ[02:51.171]1す[02:53.677]こ[02:53.730]1ひ1み1わ[03:09.052]た" +
                            "[03:09.604]1な[03:10.540]ぎ[03:10.828]さ[03:11.148]1い[03:12.468]ま[03:12.724]1お[03:13.130]も" +
                            "[03:13.626]1だ1す[03:17.290]な[03:17.586]1う[03:18.250]え[03:18.636]1き[03:19.594]ざ[03:19.810]1こ" +
                            "[03:20.800]と[03:21.152]1ば1き[03:22.408]み[03:22.664]1う[03:23.312]し[03:23.785]1す[03:24.608]が" +
                            "[03:24.920]た[03:25.312]1ひ[03:27.798]か[03:28.063]1さ1は[03:29.846]な[03:30.151]1び1み1お1な[03:34.966]つ" +
                            "[03:34.985]1あ[03:37.277]い[03:37.379]1ま[03:37.666]い[03:37.724]1こ[03:38.164]こ[03:38.620]ろ" +
                            "[03:38.788]1と1つ[03:40.895]な[03:41.206]1よ[03:42.655]る[03:42.831]1つ[03:43.633]づ[03:43.755]1ほ]", new LyricTimestamp(0));
            var voList = LyricLineVo.Split(vo);

            var sb = new StringBuilder();
            foreach (var subVo in voList)
            {
                sb.Append(subVo.Print("[mm:ss.SSS]", DotTypeEnum.DOWN));
            }
            
            Assert.AreEqual(vo.Print("[mm:ss.SSS]", DotTypeEnum.DOWN), sb.ToString());
        }
    }
}