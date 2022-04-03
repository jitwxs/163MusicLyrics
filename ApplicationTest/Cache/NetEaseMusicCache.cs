using System;
using NUnit.Framework;
using static Application.Cache.NetEaseMusicCache;

namespace ApplicationTest.Cache
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