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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Media.ISO.Boxes;

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

        private const string LocalMp4TestContent = @"BigBuckBunny_320x180.mp4";

        private const string LocalFmp4TestContent = @"BigBuckBunny_331.ismv";

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

        private static void DisplayBox(Box box, int index)
        {
            StringBuilder indent = new StringBuilder();
            for (int i = 0; i < index; ++i)
            {
                indent.Append("===>");
            }
            Trace.TraceInformation("{2}Box:{0} Size:{1}", box.Name, box.Size, indent);
            box.Children.ForEach(child => DisplayBox(child, index+1));
        }

        /// <summary>
        /// Helper to test file deserialization and serialization with round trip.
        /// </summary>
        /// <param name="fileName"></param>
        private void FileParsingHelper(string fileName)
        {
            var outputFile = Path.GetTempFileName();
            var originalFile = new FileInfo(fileName);
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var boxes = BoxFactory.Parse(stream).ToList();
                using var filestream = new FileStream(outputFile, FileMode.OpenOrCreate);
                var writer = new BoxWriter(filestream);
                foreach (var box in boxes)
                {
                    DisplayBox(box, 0);
                    box.Write(writer);
                }
                writer.Close();
            }
            var newFile = new FileInfo(outputFile);
            Trace.TraceInformation("Original File: {0} - {1}\n New File:{2} {3}",
                fileName,
                originalFile.Length,
                outputFile,
                newFile.Length
                );
            Assert.AreEqual(originalFile.Length, newFile.Length);
        }

        [TestMethod]
        public void Mp4ParseTest()
        {
            FileParsingHelper(LocalMp4TestContent);
        }

        [TestMethod]
        public void FragmentedMp4ParseTest()
        {
            FileParsingHelper(LocalFmp4TestContent);            
        }
    }
}
