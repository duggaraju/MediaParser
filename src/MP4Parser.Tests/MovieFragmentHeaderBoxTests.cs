using Media.ISO.Boxes;
using System.IO;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class MovieFragmentHeaderBoxTests
    {
        private static readonly byte[] Buffer =
        {
            00, 00, 00, 0x10, 0x6d, 0x66, 0x68, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03
        };

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<MovieFragmentHeaderBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(16L, box.Size);
            Assert.Equal(BoxType.MovieFragmentHeaderBox, box.Type);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0x3u, box.SequenceNumber);
            Assert.Equal(0u, box.Flags);
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new MovieFragmentHeaderBox();
            Assert.Equal(BoxType.MovieFragmentHeaderBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.SequenceNumber = 3u;
            box.ComputeSize();
            Assert.Equal(16L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.Equal(box.Size, stream.Length);
            Assert.Equal(Buffer, stream.ToArray());
        }
    }
}
