using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class TrackFragmentDecodeTimeBoxTests
    {
        static byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x14,
            0x74, 0x66, 0x64, 0x74,
            0x01, 0x00, 0x00, 0x00,
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xab, 0xcd, 0xef
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentDecodeTimeBox>(reader);
            Assert.IsNotNull(box);
            Assert.AreEqual(20L, box.Size);
            Assert.AreEqual(BoxType.TrackFragmentDecodeTimeBox, box.Type);
            Assert.AreEqual((byte)1, box.Version);
            Assert.AreEqual(0x1234567890abcdefUL, box.BaseMediaDecodeTime);
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new TrackFragmentDecodeTimeBox();
            Assert.AreEqual(BoxType.TrackFragmentDecodeTimeBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.BaseMediaDecodeTime = 0x1234567890abcdef;
            box.Version = (byte)1;
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
