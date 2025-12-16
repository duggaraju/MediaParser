using Media.ISO.Boxes;
using System.IO;
using System.Linq;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
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

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentRunBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(0x24L, box.Size);
            Assert.Equal(BoxType.TrackFragmentRunBox, box.Type);
            Assert.Equal((byte)1, box.Version);
            Assert.Equal(0x3, box.SampleCount);
            Assert.NotEqual(0u, box.Flags & TrackFragmentRunBox.DataOffsetPresent);
            Assert.NotEqual(0u, box.Flags & TrackFragmentRunBox.FirstSampleFlagsPresent);
            Assert.NotEqual(0u, box.Flags & TrackFragmentRunBox.SampleDurationPresent);
            Assert.Equal(0u, box.Flags & TrackFragmentRunBox.SampleSizePresent);
            Assert.Equal(0u, box.Flags & TrackFragmentRunBox.SampleFlagsPresent);
            Assert.Equal(0u, box.Flags & TrackFragmentRunBox.SampleCompositionOffsetPresent);
            foreach (var sample in box.Samples)
            {
                Assert.Equal(0x12345678u, sample.Duration);
                Assert.Null(sample.Size);
                Assert.Null(sample.Flags);
                Assert.Null(sample.CompositionOffset);
            }
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new TrackFragmentRunBox();
            Assert.Equal(BoxType.TrackFragmentRunBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.Version = 1;
            box.Flags = TrackFragmentRunBox.DataOffsetPresent | TrackFragmentRunBox.FirstSampleFlagsPresent | TrackFragmentRunBox.SampleDurationPresent;
            box.DataOffset = 0x12345678;
            box.FirstSampleFlags = 0x12345678u;
            box.Samples = Enumerable.Range(0, 3).Select(i => new TrackFragmentRunBox.SampleInfo
            {
                Duration = 0x12345678u
            }).ToList();
            box.ComputeSize();
            Assert.Equal(0x24L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.Equal(box.Size, stream.Length);
            Assert.Equal(Buffer, stream.ToArray());
        }
    }
}
