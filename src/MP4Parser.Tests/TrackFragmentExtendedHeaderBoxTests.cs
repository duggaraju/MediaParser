using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class TrackFragmentExtendedHeaderBoxTests
    {
        static byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x2c,
            0x75, 0x75, 0x69, 0x64,
            0x6d, 0x1d, 0x9b, 0x05,
            0x42, 0xd5, 0x44, 0xe6,
            0x80, 0xe2, 0x14, 0x1d,
            0xaf, 0xf7, 0x57, 0xb2,
            0x01, 0x00, 0x00, 0x00,
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xab, 0xcd, 0xef,
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xab, 0xcd, 0xef
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentExtendedHeaderBox>(reader);
            Assert.IsNotNull(box);
            Assert.AreEqual(44L, box.Size);
            Assert.AreEqual(BoxType.UuidBox, box.Type);
            Assert.AreEqual(Guid.Parse(BoxConstants.TrackFragmentExtendedHeaderBox), box.ExtendedType);
            Assert.AreEqual((byte)1, box.Version);
            Assert.AreEqual(0x1234567890abcdefUL, box.Time);
            Assert.AreEqual(0x1234567890abcdefUL, box.Duration);
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new TrackFragmentExtendedHeaderBox();
            Assert.AreEqual(BoxType.UuidBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual(Guid.Parse(BoxConstants.TrackFragmentExtendedHeaderBox), box.ExtendedType);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.Time = 0x1234567890abcdef;
            box.Duration = 0x1234567890abcdef;
            box.Version = (byte)1;
            box.ComputeSize();
            Assert.AreEqual(44L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.AreEqual(box.Size, stream.Length);
            CollectionAssert.AreEqual(Buffer, stream.ToArray());
        }

    }
}
