using Media.ISO;
using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class ReservedAttributeTests
    {
        private static readonly byte[] Buffer =
        {
            0x00, 0x00, 0x00, 0x18,
            0x72, 0x73, 0x76, 0x62,
            0x01, 0x00, 0x00, 0x00,
            0x11, 0x22, 0x33, 0x44,
            0xde, 0xad, 0xbe, 0xef,
            0x55, 0x66, 0x77, 0x88
        };

        [Fact]
        public void ReservedAttribute_ReadsReservedBytes()
        {
            using var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<ReservedFieldsBox>(reader);

            Assert.NotNull(box);
            var parsed = box!;
            Assert.Equal("rsvb".GetBoxType(), parsed.Type);
            Assert.Equal((byte)1, parsed.Version);
            Assert.Equal(0u, parsed.Flags);
            Assert.Equal(0x11223344u, parsed.LeadingValue);
            Assert.Equal(ReservedFieldsBox.ReservedFieldLength, parsed.ReservedBytes);
            Assert.Equal(0x55667788u, parsed.TrailingValue);
        }

        [Fact]
        public void ReservedAttribute_SerializesPadding()
        {
            var box = new ReservedFieldsBox
            {
                Version = 1,
                LeadingValue = 0x11223344u,
                TrailingValue = 0x55667788u
            };

            box.ComputeSize();
            using var stream = new MemoryStream();
            box.Write(stream);

            var data = stream.ToArray();
            Assert.Equal(0x18, data.Length);

            const int reservedOffset = 12 + sizeof(uint);
            for (var i = 0; i < ReservedFieldsBox.ReservedFieldLength; i++)
            {
                Assert.Equal(0, data[reservedOffset + i]);
            }

            stream.Position = 0;
            var reader = new BoxReader(stream);
            var reparsed = BoxFactory.Parse<ReservedFieldsBox>(reader);
            Assert.NotNull(reparsed);
            var reparsedBox = reparsed!;
            Assert.Equal(box.LeadingValue, reparsedBox.LeadingValue);
            Assert.Equal(ReservedFieldsBox.ReservedFieldLength, reparsedBox.ReservedBytes);
            Assert.Equal(box.TrailingValue, reparsedBox.TrailingValue);
        }
    }
}
