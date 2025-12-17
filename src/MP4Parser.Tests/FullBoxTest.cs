using System;
using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests
{
    public class FullBoxTest
    {
        [Fact]
        public void FullBoxConstructorTest()
        {
            const string boxName = "abcd";
            var box = new FullBox(boxName);
            Assert.Equal(boxName, box.Name);
            Assert.Equal(0, box.Version);
            Assert.Equal(0u, box.Flags);
        }

        [Fact]
        public void VersionAndFlagsTest()
        {
            var boxType = "abcd".GetBoxType();
            var box = new FullBox("abcd");
            Assert.Equal(0, box.Version);
            Assert.Equal(0u, box.Flags);

            box.Version = 1;
            box.Flags = 8;
            Assert.Equal(1, box.Version);
            Assert.Equal(8u, box.Flags);

            // changing version doesn't change flags.
            box.Version = 0xFF;
            Assert.Equal(0xFF, box.Version);
            Assert.Equal(8u, box.Flags);

            box.Version = 0;
            Assert.Equal(0, box.Version);
            Assert.Equal(8u, box.Flags);

            //changing flags doesn't change version.
            box.Flags = 0xFFFFFF;
            Assert.Equal(0, box.Version);
            Assert.Equal(0xFFFFFFu, box.Flags);

            box.Flags &= ~1u;
            Assert.Equal(0, box.Version);
            Assert.Equal(0xFFFFFEu, box.Flags);

            box.Flags &= 0x7FFFFF;
            Assert.Equal(0, box.Version);
            Assert.Equal(0x7FFFFEu, box.Flags);

            bool exception = false;
            try
            {
                box.Flags = uint.MaxValue;
            }
            catch (ArgumentException)
            {
                exception = true;
            }
            Assert.True(exception, "ArgumentException expected!");
        }

        [Fact]
        public void FullBoxParseTest()
        {
            var box = new FullBox("abcd");
        }
    }
}
