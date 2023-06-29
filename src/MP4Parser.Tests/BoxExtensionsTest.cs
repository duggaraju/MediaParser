using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    public class BoxExtensionsTest
    {
        [TestMethod]
        public void BoxNameTest()
        {
            foreach (BoxType boxType in Enum.GetValues(typeof(BoxType)))
            {
                string name = boxType.GetBoxName();
                Trace.TraceInformation("Box:{0} type:{1:x}", name, boxType);
            }
        }

        [TestMethod]
        public void GetBoxNameTest()
        {
            var ftyp = (BoxType) 0x66747970;
            string name = ftyp.GetBoxName();
            Assert.AreEqual("ftyp", name);
            Assert.AreEqual(ftyp, name.GetBoxType());
        }

        [TestMethod]
        public void GetBoxTypeTest()
        {
            const string boxName = "test";
            var boxType = boxName.GetBoxType();
            Assert.AreEqual(0x74657374U, (uint)boxType);
            Assert.AreEqual(boxName, boxType.GetBoxName());
        }
    }
}
