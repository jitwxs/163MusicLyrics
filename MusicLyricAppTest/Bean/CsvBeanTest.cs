using System;
using System.Collections.Generic;
using MusicLyricApp.Bean;
using NUnit.Framework;

namespace MusicLyricAppTest.Bean
{
    [TestFixture]
    public class CsvUtils
    {
        [Test]
        public void TestNewInstance()
        {
            var songVos = new List<SongVo>();

            for (var i = 0; i < 10; i++)
            {
                var vo = new SongVo
                {
                    Album = i.ToString(),
                    Name = (i * 100).ToString()
                };

                songVos.Add(vo);
            }
            
            var bean = new CsvBean();
            
            bean.AddColumn("name");
            bean.AddColumn("album");
            
            foreach (var songVo in songVos)
            {
                bean.AddData(songVo.Name);
                bean.AddData(songVo.Album);
                bean.NextLine();
            }
            Console.WriteLine(bean);
        }
    }
}