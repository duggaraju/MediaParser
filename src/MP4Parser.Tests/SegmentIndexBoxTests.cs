using Media.ISO.Boxes;
using System.IO;
using System.Linq;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
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

        [Fact]
        public void TestDeserialize()
        {
            var stream = new MemoryStream(Buffer, writable: false);
            var reader = new BoxReader(stream);
            var box = BoxFactory.Parse<SegmentIndexBox>(reader);
            Assert.NotNull(box);
            Assert.Equal(0x44L, box.Size);
            Assert.Equal(BoxType.SegmentIndexBox, box.Type);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);
            Assert.Equal(0x12345678u, box.ReferenceId);
            Assert.Equal(0x12345678u, box.TimeScale);
            Assert.Equal(0x12345678u, box.EarliestPresentationTime);
            Assert.Equal(0x12345678u, box.FirstOffset);
            Assert.Equal(0x3, box.Entries.Count);
            foreach (var entry in box.Entries)
            {
                Assert.False(entry.IsSegmentIndex);
                Assert.Equal(0x12345678u, entry.ReferenceSize);
                Assert.Equal(0x90abcdefu, entry.SegmentDuration);
                Assert.Equal(0x12345678u, entry.SapDeltaTime);
                Assert.False(entry.StartsWithSap);
            }
        }

        [Fact]
        public void TestSerialize()
        {
            var box = new SegmentIndexBox();
            Assert.Equal(BoxType.SegmentIndexBox, box.Type);
            Assert.Equal(0L, box.Size);
            Assert.Equal((byte)0, box.Version);
            Assert.Equal(0u, box.Flags);

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
            Assert.Equal(0x44L, box.Size);
            var stream = new MemoryStream(32);
            box.Write(stream);
            stream.Position = 0;
            Assert.Equal(box.Size, stream.Length);
            Assert.Equal(Buffer, stream.ToArray());
        }
    }
}
