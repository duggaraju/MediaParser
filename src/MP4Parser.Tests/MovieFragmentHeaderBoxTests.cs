using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class MovieFragmentHeaderBoxTests
    {
        private static readonly byte[] Buffer = 
        {
            00, 00, 00, 0x10, 0x6d, 0x66, 0x68, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<MovieFragmentHeaderBox>(reader);
            Assert.IsNotNull(box);
            Assert.AreEqual(16L, box.Size);
            Assert.AreEqual(BoxType.MovieFragmentHeaderBox, box.Type);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0x3u, box.SequenceNumber);
            Assert.AreEqual(0u, box.Flags);
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new MovieFragmentHeaderBox();
            Assert.AreEqual(BoxType.MovieFragmentHeaderBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.SequenceNumber = 3u;
            box.ComputeSize();
            Assert.AreEqual(16L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.AreEqual(box.Size, stream.Length);
            CollectionAssert.AreEqual(Buffer, stream.ToArray());
        }
    }
}
