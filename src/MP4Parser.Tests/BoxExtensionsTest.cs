using System;
using System.Diagnostics;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class BoxExtensionsTest
    {
        [Fact]
        public void BoxNameTest()
        {
            foreach (BoxType boxType in Enum.GetValues(typeof(BoxType)))
            {
                string name = boxType.GetBoxName();
                Trace.TraceInformation("Box:{0} type:{1:x}", name, boxType);
            }
        }

        [Fact]
        public void GetBoxNameTest()
        {
            var ftyp = (BoxType)0x66747970;
            string name = ftyp.GetBoxName();
            Assert.Equal("ftyp", name);
            Assert.Equal(ftyp, name.GetBoxType());
        }

        [Fact]
        public void GetBoxTypeTest()
        {
            const string boxName = "test";
            var boxType = boxName.GetBoxType();
            Assert.Equal(0x74657374U, (uint)boxType);
            Assert.Equal(boxName, boxType.GetBoxName());
        }
    }
}
