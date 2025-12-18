using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
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

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentDecodeTimeBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(20L, box.Size);
            Assert.Equal(BoxType.TrackFragmentDecodeTimeBox, box.Type);
            Assert.Equal((byte)1, box.Version);
            Assert.Equal(0x1234567890abcdefUL, box.BaseMediaDecodeTime);
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new TrackFragmentDecodeTimeBox();
            Assert.Equal(BoxType.TrackFragmentDecodeTimeBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.BaseMediaDecodeTime = 0x1234567890abcdef;
            box.Version = (byte)1;
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
