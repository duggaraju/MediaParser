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
            foreach( var boxName in BoxConstants.BoxNames)
            {
                uint boxType = boxName.GetBoxType();
                string name = boxType.GetBoxName();
                Assert.AreEqual(boxName, name);
                Trace.TraceInformation("Box:{0} type:{1:x}", boxName, boxType);
            }
        }

        [TestMethod]
        public void GetBoxNameTest()
        {
            const uint ftyp = 0x66747970;
            string name = ftyp.GetBoxName();
            Assert.AreEqual("ftyp", name);
            Assert.AreEqual(ftyp, name.GetBoxType());
        }

        [TestMethod]
        public void GetBoxTypeTest()
        {
            const string boxName = "test";
            var boxType = boxName.GetBoxType();
            Assert.AreEqual(0x74657374U, boxType);
            Assert.AreEqual(boxName, boxType.GetBoxName());
        }
    }
}
