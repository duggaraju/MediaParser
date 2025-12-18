using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
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

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentHeaderBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(20L, box.Size);
            Assert.Equal(BoxType.TrackFragmentHeaderBox, box.Type);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0x3u, box.TrackId);
            Assert.NotEqual(0u, box.Flags & TrackFragmentHeaderBox.SampleDescriptionIndexPresent);
            Assert.Equal(0x12345678u, box.SampleDescriptionIndex);
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new TrackFragmentHeaderBox();
            Assert.Equal(BoxType.TrackFragmentHeaderBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.TrackId = 3u;
            box.SampleDescriptionIndex = 0x12345678;
            box.ComputeSize();
            Assert.Equal(20L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.Equal(box.Size, stream.Length);
            Assert.Equal(Buffer, stream.ToArray());
        }
    }
}
