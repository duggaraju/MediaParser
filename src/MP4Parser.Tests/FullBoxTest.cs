using System;
using Media.ISO.Boxes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class FullBoxTest
    {
        [TestMethod]
        public void FullBoxConstructorTest()
        {
            const string boxName = "abcd";
            var box = new FullBox("abcd");
            Assert.AreEqual(boxName, box.Name);
            Assert.AreEqual(12, box.ComputeSize());
            Assert.AreEqual(box.Version, 0);
            Assert.AreEqual(box.Flags, 0u);

            box = new FullBox(boxName.GetBoxType());
            Assert.AreEqual(boxName, box.Name);
            Assert.AreEqual(12, box.ComputeSize());
            Assert.AreEqual(box.Version, 0);
            Assert.AreEqual(box.Flags, 0u);
        }

        [TestMethod]
        public void VersionAndFlagsTest()
        {
            var boxType = "abcd".GetBoxType();
            var box = new FullBox("abcd");
            Assert.AreEqual(box.Version, 0);
            Assert.AreEqual(box.Flags, 0u);

            box.Version = 1;
            box.Flags = 8;
            Assert.AreEqual(1, box.Version);
            Assert.AreEqual(8u, box.Flags);

            // changing version doesn't change flags.
            box.Version = 0xFF;
            Assert.AreEqual(0xFF, box.Version);
            Assert.AreEqual(8u, box.Flags);

            box.Version = 0;
            Assert.AreEqual(0, box.Version);
            Assert.AreEqual(8u, box.Flags);

            //changing flags doesn't change version.
            box.Flags = 0xFFFFFF;
            Assert.AreEqual(0, box.Version);
            Assert.AreEqual(0xFFFFFFu, box.Flags);

            box.Flags &= ~1u;
            Assert.AreEqual(0, box.Version);
            Assert.AreEqual(0xFFFFFEu, box.Flags);

            box.Flags &= 0x7FFFFF;
            Assert.AreEqual(0, box.Version);
            Assert.AreEqual(0x7FFFFEu, box.Flags);

            bool exception = false;
            try
            {
                box.Flags = uint.MaxValue;
            }
            catch (ArgumentException)
            {
                exception = true;
            }
            Assert.IsTrue(exception, "ArgumentException expected!");
        }

        [TestMethod]
        public void FullBoxParseTest()
        {
            var box = new FullBox("abcd");
        }
    }
}
