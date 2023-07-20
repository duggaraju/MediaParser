using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class TrackFragmentHeaderBoxTests
    {
        static byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x14,
            0x74, 0x66, 0x68, 0x64,
            0x00, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x03,
            0x12, 0x34, 0x56, 0x78
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentHeaderBox>(reader);    
            Assert.IsNotNull(box);
            Assert.AreEqual(20L, box.Size);
            Assert.AreEqual(BoxType.TrackFragmentHeaderBox, box.Type);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0x3u, box.TrackId);
            Assert.AreNotEqual(0u, box.Flags & TrackFragmentHeaderBox.SampleDescriptionIndexPresent);
            Assert.AreEqual(0x12345678u, box.SampleDescriptionIndex);
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new TrackFragmentHeaderBox();
            Assert.AreEqual(BoxType.TrackFragmentHeaderBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.TrackId = 3u;
            box.SampleDescriptionIndex = 0x12345678;
            box.ComputeSize();
            Assert.AreEqual(20L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.AreEqual(box.Size, stream.Length);
            CollectionAssert.AreEqual(Buffer, stream.ToArray());
        }
    }
}
