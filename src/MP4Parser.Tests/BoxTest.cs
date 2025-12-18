using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class BoxTest
    {
        [Fact]
        public void TestConstructor()
        {
            const string boxName = "test";
            const long boxSize = 0x12345678abcdef;
            var type = boxName.GetBoxType();
            var header = new BoxHeader(type, boxSize);
            var box = new RawBox(header);
            Assert.Equal(type, box.Type);
            Assert.Equal(boxName, box.Name);
            Assert.Equal(boxSize, box.Size);
            Assert.Equal(string.Format("Box:{0} Size:{1}", boxName, boxSize), box.ToString());
            Assert.Null(box.ExtendedType);

            var extendedType = new Guid("7576671E-638A-48CE-AC52-F750DD11F78B");
            header = new BoxHeader(type, boxSize, extendedType);
            box = new RawBox(header);
            Assert.Equal(type, box.Type);
            Assert.Equal(boxName, box.Name);
            Assert.Equal(boxSize, box.Size);
            Assert.Equal(string.Format("Box:{0} Size:{1}", boxName, boxSize), box.ToString());
            Assert.NotNull(box.ExtendedType);
            Assert.Equal(extendedType, box.ExtendedType);
        }

        [Fact]
        public void TypedBoxTest()
        {
            var box = new FileBox();
            Assert.Equal("ftyp", box.Name);
            Assert.Equal(0L, box.Size);
        }

        private void BoxParsingHelper(byte[] bytes, BoxType boxType, long boxSize, long bodyLength = 0, bool roundTrip = true)
        {
            var stream = new MemoryStream(bytes);
            var box = BoxFactory.ParseBoxes(stream).Single();
            Assert.Equal(boxSize, box.Size);
            Assert.Equal(boxType, box.Type);

            if (box is RawBox rawBox)
            {
                Assert.Equal(bodyLength, rawBox.Body.Length);
            }

            using var output = new MemoryStream(bytes.Length);
            box.Write(output);
            var outputBytes = output.ToArray();
            if (roundTrip)
                Assert.Equal(bytes, outputBytes);
        }

        [Fact]
        public void ParseSimpleBoxWithNoBody()
        {
            const byte length = 0x8;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, 0xab, 0xcd, 0xef, 0x01
            };
            BoxParsingHelper(bytes, (BoxType)0xabcdef01u, bytes.Length);
        }

        [Fact]
        public void ParseSimpleBoxWithBody()
        {
            const byte length = 0x10;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, 0xab, 0xcd, 0xef, 0x01,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, (BoxType)0xabcdef01u, bytes.Length, bodyLength: 8);
        }

        [Fact]
        public void ParseBoxWithLongLengthNoBody()
        {
            const byte length = 0x10;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, 0x1, 0xab, 0xcd, 0xef, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, length
            };
            BoxParsingHelper(bytes, (BoxType)0xabcdef01u, bytes.Length, roundTrip: false);
        }

        [Fact]
        public void ParseBoxWithLongLengthAndBody()
        {
            const byte length = 0x18;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, 0x1, 0xab, 0xcd, 0xef, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, length,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, (BoxType)0xabcdef01u, bytes.Length, bodyLength: 8, roundTrip: false);
        }

        [Fact]
        public void ParseBoxWithExtendedTypeNoBody()
        {
            var boxType = BoxType.UuidBox;
            var boxName = BitConverter.GetBytes((int)boxType);
            const byte length = 0x18;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, boxName[3], boxName[2], boxName[1], boxName[0],
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff
            };
            BoxParsingHelper(bytes, boxType, bytes.Length);
        }

        [Fact]
        public void ParseBoxWithExtendedTypeAndBody()
        {
            var boxType = BoxType.UuidBox;
            var boxName = BitConverter.GetBytes((int)boxType);
            const byte length = 0x20;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, boxName[3], boxName[2], boxName[1], boxName[0],
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, boxType, bytes.Length, bodyLength: 8);
        }

        private void BoxParseExceptionHelper(byte[] bytes, string? message = null)
        {
            Box? box = null;
            var stream = new MemoryStream(bytes);
            try
            {
                box = BoxFactory.ParseBoxes(stream).Single();
            }
            catch (Exception e)
            {
                Assert.IsType<ParseException>(e);
                return;
            }

            Assert.Fail("Parsing not thrown!");
        }

        [Fact]
        public void ParseErrorTest()
        {
            // insufficeient bytes.
            byte[] bytes = { 0x00, 0x01 };
            BoxParseExceptionHelper(bytes);

            // fewer bytes than the length of box.
            bytes = new byte[] { 0x00, 0x00, 0x00, 0x9, 0xab, 0xcd, 0xef, 0x01 };
            BoxParseExceptionHelper(bytes);

            // size set to 1 but not enough bytes.
            bytes = new byte[] { 0x00, 0x00, 0x00, 0x1, 0xab, 0xcd, 0xef, 0x01 };
            BoxParseExceptionHelper(bytes);

            // fewer bytes for type.
            bytes = new byte[] { 0x00, 0x00, 0x00, 0x0, 0xab, 0xcd, 0xef };
            BoxParseExceptionHelper(bytes);

            // fewer bytes for type.
            bytes = new byte[] { 0x00, 0x00, 0x00, 0x0, 0xab, 0xcd, 0xef };
            BoxParseExceptionHelper(bytes);
        }
    }
}
