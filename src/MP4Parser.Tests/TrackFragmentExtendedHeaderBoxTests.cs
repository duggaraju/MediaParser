using Media.ISO.Boxes;
using System;
using System.IO;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
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

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<TrackFragmentExtendedHeaderBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(44L, box.Size);
            Assert.Equal(BoxType.UuidBox, box.Type);
            Assert.Equal(Guid.Parse(BoxConstants.TrackFragmentExtendedHeaderBox), box.ExtendedType);
            Assert.Equal((byte)1, box.Version);
            Assert.Equal(0x1234567890abcdefUL, box.Time);
            Assert.Equal(0x1234567890abcdefUL, box.Duration);
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new TrackFragmentExtendedHeaderBox();
            Assert.Equal(BoxType.UuidBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal(Guid.Parse(BoxConstants.TrackFragmentExtendedHeaderBox), box.ExtendedType);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.Time = 0x1234567890abcdef;
            box.Duration = 0x1234567890abcdef;
            box.Version = (byte)1;
            box.ComputeSize();
            Assert.Equal(44L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.Equal(box.Size, stream.Length);
            Assert.Equal(Buffer, stream.ToArray());
        }

    }
}
