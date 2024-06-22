using System;
using hyjiacan.py4n;
using NUnit.Framework;

namespace MusicLyricAppTest.Utils
{
    [TestFixture]
    public class PinyinTest
    {

        [Test]
        public void TestQuickStart()
        {
            // 设置拼音输出格式
            const PinyinFormat format = PinyinFormat.WITH_TONE_MARK  | PinyinFormat.LOWERCASE | PinyinFormat.WITH_U_UNICODE;

            // 判断是否是汉字
            Assert.IsTrue(PinyinUtil.IsHanzi('李'));
            
            // 取出指定汉字的所有拼音
            Console.WriteLine(string.Join(",", Pinyin4Net.GetPinyin('好')));
            
            // 取出指定汉字的所有拼音（经过格式化的）
            Console.WriteLine(string.Join(",", Pinyin4Net.GetPinyin('好', format)));
            
            // 取指定汉字的唯一或者第一个拼音
            Console.WriteLine(Pinyin4Net.GetFirstPinyin('好'));
            
            // 取指定汉字的唯一或者第一个拼音（经过格式化的）
            Console.WriteLine(Pinyin4Net.GetFirstPinyin('好', format));

            Console.WriteLine(Pinyin4Net.GetPinyin("你好吗这里是 china 北京！", format));
        }
    }
}