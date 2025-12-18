using Media.ISO;
using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class EventMessageBoxTests
    {
        [Fact]
        public void Version0RoundTripsPayload()
        {
            const string scheme = "urn:test:event";
            const string value = "slate";
            const uint timeScale = 1000;
            const uint presentationTime = 4242;
            const uint duration = 1337;
            const uint id = 99;

            var expectedBytes = CreateVersion0Bytes(scheme, value, timeScale, presentationTime, duration, id);

            var parsed = Parse(expectedBytes);
            Assert.Equal(BoxType.EmsgBox, parsed.Type);
            Assert.Equal((byte)0, parsed.Version);
            Assert.Equal(scheme, parsed.Scheme);
            Assert.Equal(value, parsed.Value);
            Assert.Equal(timeScale, parsed.TimeScale);
            Assert.Equal((ulong)presentationTime, parsed.PresentationTime);
            Assert.Equal(duration, parsed.Duration);
            Assert.Equal(id, parsed.Id);

            var box = new EventMessageBox
            {
                Version = 0,
                Scheme = scheme,
                Value = value,
                TimeScale = timeScale,
                PresentationTime = presentationTime,
                Duration = duration,
                Id = id
            };

            var size = box.ComputeSize();
            Assert.Equal((long)expectedBytes.Length, size);
            Assert.Equal(size, box.Size);

            using var stream = new MemoryStream(expectedBytes.Length);
            box.Write(stream);
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        [Fact]
        public void Version1RoundTripsPayload()
        {
            const string scheme = "com.test.stream";
            const string value = "playout";
            const uint timeScale = 192000;
            const ulong presentationTime = 0x1_0000_1234UL;
            const uint duration = 4096;
            const uint id = 314159;

            var expectedBytes = CreateVersion1Bytes(scheme, value, timeScale, presentationTime, duration, id);

            var parsed = Parse(expectedBytes);
            Assert.Equal(BoxType.EmsgBox, parsed.Type);
            Assert.Equal((byte)1, parsed.Version);
            Assert.Equal(scheme, parsed.Scheme);
            Assert.Equal(value, parsed.Value);
            Assert.Equal(timeScale, parsed.TimeScale);
            Assert.Equal(presentationTime, parsed.PresentationTime);
            Assert.Equal(duration, parsed.Duration);
            Assert.Equal(id, parsed.Id);

            var box = new EventMessageBox
            {
                Version = 1,
                Scheme = scheme,
                Value = value,
                TimeScale = timeScale,
                PresentationTime = presentationTime,
                Duration = duration,
                Id = id
            };

            var size = box.ComputeSize();
            Assert.Equal((long)expectedBytes.Length, size);
            Assert.Equal(size, box.Size);

            using var stream = new MemoryStream(expectedBytes.Length);
            box.Write(stream);
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        private static EventMessageBox Parse(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var reader = new BoxReader(stream);
            var parsed = BoxFactory.Parse<EventMessageBox>(reader);
            Assert.NotNull(parsed);
            return parsed!;
        }

        // Helper builders ensure the expected byte layout stays readable in the tests above.
        private static byte[] CreateVersion0Bytes(string scheme, string value, uint timeScale, uint presentationTime, uint duration, uint id)
        {
            using var bodyStream = new MemoryStream();
            var bodyWriter = new BoxWriter(bodyStream);
            bodyWriter.WriteString(scheme);
            bodyWriter.WriteString(value);
            bodyWriter.WriteUInt32(timeScale);
            bodyWriter.WriteUInt32(presentationTime);
            bodyWriter.WriteUInt32(duration);
            bodyWriter.WriteUInt32(id);
            var body = bodyStream.ToArray();

            using var stream = new MemoryStream();
            var writer = new BoxWriter(stream);
            writer.WriteUInt32((uint)(body.Length + 12));
            writer.WriteUInt32((uint)BoxType.EmsgBox);
            writer.WriteUInt32(0u);
            writer.Write(body.AsSpan());
            return stream.ToArray();
        }

        private static byte[] CreateVersion1Bytes(string scheme, string value, uint timeScale, ulong presentationTime, uint duration, uint id)
        {
            using var bodyStream = new MemoryStream();
            var bodyWriter = new BoxWriter(bodyStream);
            bodyWriter.WriteUInt32(timeScale);
            bodyWriter.WriteUInt64(presentationTime);
            bodyWriter.WriteUInt32(duration);
            bodyWriter.WriteUInt32(id);
            bodyWriter.WriteString(scheme);
            bodyWriter.WriteString(value);
            var body = bodyStream.ToArray();

            using var stream = new MemoryStream();
            var writer = new BoxWriter(stream);
            writer.WriteUInt32((uint)(body.Length + 12));
            writer.WriteUInt32((uint)BoxType.EmsgBox);
            writer.WriteUInt32(0x01000000);
            writer.Write(body.AsSpan());
            return stream.ToArray();
        }
    }
}
