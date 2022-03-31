using System;
using NUnit.Framework;
using static 网易云歌词提取.NetEaseMusicCache;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _163MusicLyrics_Test
{
    [TestFixture]
    public class NetEaseMusicCache
    {
        [Test]
        public void NullException()
        {
            Assert.Throws<ArgumentNullException>(() => PutLyric(0, null));
            Assert.Throws<ArgumentNullException>(() => PutAlbum(0, null));
            Assert.Throws<ArgumentNullException>(() => PutSong(0, null));
        }

    }
}
