using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class SegmentIndexBoxTests
    {
        static byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x44,
            0x73, 0x69, 0x64, 0x78,
            0x00, 0x00, 0x00, 0x00,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x00, 0x00, 0x00, 0x03,
            0x12, 0x34, 0x56, 0x78, 
            0x90, 0xab, 0xcd, 0xef,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xab, 0xcd, 0xef,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xab, 0xcd, 0xef,
            0x12, 0x34, 0x56, 0x78,
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<SegmentIndexBox>(reader);
            Assert.IsNotNull(box);
            Assert.AreEqual(0x44L, box.Size);
            Assert.AreEqual(BoxType.SegmentIndexBox, box.Type);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);
            Assert.AreEqual(0x12345678u, box.ReferenceId);
            Assert.AreEqual(0x12345678u, box.TimeScale);
            Assert.AreEqual(0x12345678u, box.EarliestPresentationTime);
            Assert.AreEqual(0x12345678u, box.FirstOffset);
            Assert.AreEqual(0x3, box.Entries.Count);
            foreach (var entry in box.Entries)
            {
                Assert.IsFalse(entry.IsSegmentIndex);
                Assert.AreEqual(0x12345678u, entry.ReferenceSize);
                Assert.AreEqual(0x90abcdefu, entry.SegmentDuration);
                Assert.AreEqual(0x12345678u, entry.SapDeltaTime);
                Assert.IsFalse(entry.StartsWithSap);
            }
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new SegmentIndexBox();
            Assert.AreEqual(BoxType.SegmentIndexBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.TimeScale = 0x12345678;
            box.ReferenceId = 0x12345678;
            box.EarliestPresentationTime = 0x12345678;
            box.FirstOffset = 0x12345678u;
            box.Entries.AddRange(Enumerable.Range(0, 3).Select(i => new SegmentIndexBox.SegmentEntry
            {
                ReferenceSize = 0x12345678u,
                SegmentDuration = 0x90abcdef,
                SapDeltaTime = 0x12345678
            }));
            box.ComputeSize();
            Assert.AreEqual(0x44L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.AreEqual(box.Size, stream.Length);
            CollectionAssert.AreEqual(Buffer, stream.ToArray());
        }
    }
}
