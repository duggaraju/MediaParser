using System;
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

    }
}
