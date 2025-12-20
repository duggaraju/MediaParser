using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class ReservedAttributeTests
    {
        private const string Description = "Video Handler";

        private static readonly uint VideoHandlerCode = BoxExtensions.FromFourCC("vide".AsSpan());

        private static readonly byte[] HandlerBoxBuffer =
        {
            0x00, 0x00, 0x00, 0x2E, // size (46 bytes)
            0x68, 0x64, 0x6C, 0x72, // "hdlr"
            0x00, 0x00, 0x00, 0x00, // version & flags
            0x00, 0x00, 0x00, 0x00, // reserved (4 bytes)
            0x76, 0x69, 0x64, 0x65, // handler "vide"
            0x00, 0x00, 0x00, 0x00, // reserved padding (12 bytes)
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x56, 0x69, 0x64, 0x65, // "Video Handler" string + null terminator
            0x6F, 0x20, 0x48, 0x61,
            0x6E, 0x64, 0x6C, 0x65,
            0x72, 0x00
        };

        [Fact]
        public void ReservedAttribute_ReadsReservedBytes()
        {
            using var stream = new MemoryStream(HandlerBoxBuffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<HandlerBox>(reader);

            Assert.NotNull(box);
            var parsed = box!;
            Assert.Equal(BoxType.HandlerBox, parsed.Type);
            Assert.Equal((byte)0, parsed.Version);
            Assert.Equal(0u, parsed.Flags);
            Assert.Equal(VideoHandlerCode, parsed.Handler);
            Assert.Equal("vide", parsed.HandlerName);
            Assert.Equal(Description, parsed.HandlerDescription);
            Assert.Equal(0, parsed.PreDefined);
            Assert.Equal(0, parsed.Reserverd);
        }

        [Fact]
        public void ReservedAttribute_SerializesPadding()
        {
            var box = new HandlerBox
            {
                Handler = VideoHandlerCode,
                HandlerDescription = Description
            };

            box.ComputeSize();
            Assert.Equal(HandlerBoxBuffer.Length, box.Size);
            using var stream = new MemoryStream();
            box.Write(stream);

            var data = stream.ToArray();
            Assert.Equal(HandlerBoxBuffer.Length, data.Length);
            Assert.Equal(HandlerBoxBuffer, data);

            stream.Position = 0;
            var reader = new BoxReader(stream);
            var reparsed = BoxFactory.Parse<HandlerBox>(reader);
            Assert.NotNull(reparsed);
            var reparsedBox = reparsed!;
            Assert.Equal(box.Handler, reparsedBox.Handler);
            Assert.Equal(box.HandlerDescription, reparsedBox.HandlerDescription);
            Assert.Equal(0, reparsedBox.PreDefined);
            Assert.Equal(0, reparsedBox.Reserverd);
        }
    }
}
