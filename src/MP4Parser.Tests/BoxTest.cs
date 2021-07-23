using System;
using System.IO;
using System.Linq;
using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class BoxTest
    {
        [TestMethod]
        public void TestConstructor()
        {
            const string boxName = "test";
            const long boxSize = 0x12345678abcdef;
            var type = boxName.GetBoxType();
            var box = new Box(type) {Size = boxSize};
            Assert.AreEqual(type, box.Type);
            Assert.AreEqual(boxName, box.Name);
            Assert.AreEqual(boxSize, box.Size);
            Assert.AreEqual(string.Format("Box:{0} Size:{1}", boxName, boxSize), box.ToString());
            Assert.IsNull(box.ExtendedType);
            Assert.AreEqual(0, box.Children.Count);

            var extendedType = new Guid("7576671E-638A-48CE-AC52-F750DD11F78B");
            box = new Box(type, extendedType) {Size = boxSize};
            Assert.AreEqual(type, box.Type);
            Assert.AreEqual(boxName, box.Name);
            Assert.AreEqual(boxSize, box.Size);
            Assert.AreEqual(string.Format("Box:{0} Size:{1}", boxName, boxSize), box.ToString());
            Assert.IsNotNull(box.ExtendedType);
            Assert.AreEqual(extendedType, box.ExtendedType);
            Assert.AreEqual(0, box.Children.Count);
        
        }


        [TestMethod]
        public void TypedBoxTest()
        {
            var box = new FileBox();
            Assert.AreEqual("ftyp", box.Name);
            Assert.IsFalse(box.CanHaveChildren);
            Assert.AreEqual(0L, box.Size);
        }

        private void BoxParsingHelper(byte[] bytes, uint boxType, long boxSize, long bodyLength = 0, bool roundTrip = true)
        {
            var stream = new MemoryStream(bytes);
            var box = BoxFactory.Parse(stream).Single();
            Assert.AreEqual(boxSize, box.Size);
            Assert.AreEqual(boxType, box.Type);

            if (bodyLength > 0)
            {
                Assert.IsNotNull(box.Body);
                Assert.AreEqual(bodyLength, box.Body.Length);
            }
            else
            {
                Assert.AreEqual(0, box.Body.Length);
            }

            using var output = new MemoryStream(bytes.Length);
            box.Write(output);
            var outputBytes = output.ToArray();
            if (roundTrip)
                CollectionAssert.AreEqual(bytes, outputBytes);
        }

        [TestMethod]
        public void ParseSimpleBoxWithNoBody()
        {
            const byte length = 0x8;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, 0xab, 0xcd, 0xef, 0x01
            };
            BoxParsingHelper(bytes, 0xabcdef01u, bytes.Length);
        }

        [TestMethod]
        public void ParseSimpleBoxWithBody()
        {
            const byte length = 0x10;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, 0xab, 0xcd, 0xef, 0x01,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, 0xabcdef01u, bytes.Length, bodyLength:8);
        }

        [TestMethod]
        public void ParseBoxWithLongLengthNoBody()
        {
            const byte length = 0x10;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, 0x1, 0xab, 0xcd, 0xef, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, length
            };            
            BoxParsingHelper(bytes, 0xabcdef01u, bytes.Length, roundTrip:false);
        }

        [TestMethod]
        public void ParseBoxWithLongLengthAndBody()
        {
            const byte length = 0x18;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, 0x1, 0xab, 0xcd, 0xef, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, length,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, 0xabcdef01u, bytes.Length, bodyLength:8, roundTrip: false);
        }

        [TestMethod]
        public void ParseBoxWithExtendedTypeNoBody()
        {
            uint boxType = "uuid".GetBoxType();
            var boxName = BitConverter.GetBytes((int) boxType);
            const byte length = 0x18;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, boxName[3], boxName[2], boxName[1], boxName[0],
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff
            };
            BoxParsingHelper(bytes, boxType, bytes.Length);
        }

        [TestMethod]
        public void ParseBoxWithExtendedTypeAndBody()
        {
            uint boxType = "uuid".GetBoxType();
            var boxName = BitConverter.GetBytes((int)boxType);
            const byte length = 0x20;
            byte[] bytes =
            {
                0x00, 0x00, 0x00, length, boxName[3], boxName[2], boxName[1], boxName[0],
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
                0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
            };
            BoxParsingHelper(bytes, boxType, bytes.Length, bodyLength:8);
        }

        private void BoxParseExceptionHelper(byte[] bytes, string message = null)
        {
            Box box = null;
            var stream = new MemoryStream(bytes);
            try
            {
                box = BoxFactory.Parse(stream).Single();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(ParseException));
                return;
            }

            Assert.Fail("Parsing not thrown!");
        }

        [TestMethod]
        public void ParseErrorTest()
        {
            // insufficeient bytes.
            byte[] bytes = { 0x00, 0x01 };
            BoxParseExceptionHelper(bytes);

            // fewer bytes than the length of box.
            bytes = new byte[]{0x00, 0x00, 0x00, 0x9, 0xab, 0xcd, 0xef, 0x01};
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
