//Copyright 2015 Prakash Duggaraju
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Media.ISO.MP4Parser.Tests
{
    [TestClass]
    [DeploymentItem("test/content")]
    public class BoxFactoryTest
    {
        private const string TestContent =
            "http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_h264.mov";

        private const string TestContent2 =
            @"http://download.blender.org/peach/bigbuckbunny_movies/BigBuckBunny_320x180.mp4";

        private const string LocalTestContent = @"BigBuckBunny_320x180.mp4";

        [TestMethod]
        public void TestGetType()
        {
            foreach (var boxName in BoxConstants.BoxNames)
            {
                var boxType = boxName.GetBoxType();
                var type = BoxFactory.GetDeclaringType(boxType);
                Assert.IsNotNull(type);
                Trace.TraceInformation("Found box {0}/{1:x} Type:{2}", boxName, boxType, type);
            }
        }


        [TestMethod]
        public void ParseTest()
        {
            using (var stream = new FileStream(LocalTestContent, FileMode.Open))
            {
                var boxes = BoxFactory.Parse(stream).ToList();
                foreach (var box in boxes)
                {
                    Trace.TraceInformation("Box:{0}", box);
                }
            }
        }
    }
}
