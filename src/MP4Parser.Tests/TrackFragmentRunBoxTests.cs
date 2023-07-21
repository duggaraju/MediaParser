using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class TrackFragmentRunBoxTests
    {
        static byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x24,
            0x74, 0x72, 0x75, 0x6e,
            0x01, 0x00, 0x01, 0x05,
            0x00, 0x00, 0x00, 0x03,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78,
        };

        [TestMethod]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentRunBox>(reader);
            Assert.IsNotNull(box);
            Assert.AreEqual(0x24L, box.Size);
            Assert.AreEqual(BoxType.TrackFragmentRunBox, box.Type);
            Assert.AreEqual((byte)1, box.Version);
            Assert.AreEqual(0x3, box.SampleCount);
            Assert.AreNotEqual(0u, box.Flags & TrackFragmentRunBox.DataOffsetPresent);
            Assert.AreNotEqual(0u, box.Flags & TrackFragmentRunBox.FirstSampleFlagsPresent);
            Assert.AreNotEqual(0u, box.Flags & TrackFragmentRunBox.SampleDurationPresent);
            Assert.AreEqual(0u, box.Flags & TrackFragmentRunBox.SampleSizePresent);
            Assert.AreEqual(0u, box.Flags & TrackFragmentRunBox.SampleFlagsPresent);
            Assert.AreEqual(0u, box.Flags & TrackFragmentRunBox.SampleCompositionOffsetPresent);
            foreach (var sample in box.Samples)
            {
                Assert.AreEqual(0x12345678u, sample.Duration);
                Assert.IsNull(sample.Size);
                Assert.IsNull(sample.Flags);
                Assert.IsNull(sample.CompostionOffset);
            }
        }

        [TestMethod]
        public void TestSerialize()
        {
            var box = new TrackFragmentRunBox();
            Assert.AreEqual(BoxType.TrackFragmentRunBox, box.Type);
            Assert.AreEqual(0L, box.Size);
            Assert.AreEqual((byte)0, box.Version);
            Assert.AreEqual(0u, box.Flags);

            box.Version = 1;
            box.Flags = TrackFragmentRunBox.DataOffsetPresent | TrackFragmentRunBox.FirstSampleFlagsPresent | TrackFragmentRunBox.SampleDurationPresent;
            box.DataOffset = 0x12345678;
            box.FirstSampleFlags = 0x12345678u;
            box.Samples = Enumerable.Range(0, 3).Select(i => new TrackFragmentRunBox.SampleInfo
            {
                Duration = 0x12345678u
            }).ToList();
            box.ComputeSize();
            Assert.AreEqual(0x24L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.AreEqual(box.Size, stream.Length);
            CollectionAssert.AreEqual(Buffer, stream.ToArray());
        }
    }
}
