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

using System.Collections.Generic;
using System.Linq;

namespace Media.ISO.Boxes
{
    [BoxType(BoxConstants.FileBox)]
    public class FileBox : Box
    {
        public FileBox()
            : base(BoxConstants.FileBox)
        {
            CompatibleBrands = new List<int>();
        }

        public int MajorBrand { get; set; }

        public string MajorBrandName => MajorBrand.GetBoxName();

        public int MinorVersion { get; set; }

        public List<int> CompatibleBrands { get; private set; }

        protected override void ParseBoxContent(BoxReader reader, long boxEnd)
        {
            MajorBrand = reader.ReadInt32();
            MinorVersion = reader.ReadInt32();
            while (reader.BaseStream.Position < boxEnd)
            {
                CompatibleBrands.Add(reader.ReadInt32());
            }
        }

        protected override long BoxContentSize => 8 + CompatibleBrands.Count * 4;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteInt32(MajorBrand);
            writer.WriteInt32(MinorVersion);
            CompatibleBrands.ForEach(writer.WriteInt32);
        }

        public override string ToString()
        {
            return base.ToString() +
                   string.Format("Major:{0} Minor:{1}, Brands:{2}", MajorBrandName, MinorVersion,
                       string.Join(",", CompatibleBrands.Select(brand => ((uint)brand).GetBoxName())));
        }
    }
}
