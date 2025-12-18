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

namespace Media.ISO.Boxes
{
    [Box(BoxType.FileBox)]
    public partial class FileBox : Box
    {
        public uint MajorBrand { get; set; }

        public string MajorBrandName => MajorBrand.GetFourCC();

        public uint MinorVersion { get; set; }

        public List<uint> CompatibleBrands { get; private set; } = new();

        protected override long ParseBody(BoxReader reader, int _)
        {
            MajorBrand = reader.ReadUInt32();
            MinorVersion = reader.ReadUInt32();
            var bytes = Size - HeaderSize - 8;
            while (bytes > 0)
            {
                CompatibleBrands.Add(reader.ReadUInt32());
                bytes -= 4;
            }
            return ContentSize;
        }

        int ContentSize => 8 + CompatibleBrands.Count * 4;

        protected override long ComputeBodySize() => ContentSize;

        protected override long WriteBoxBody(BoxWriter writer)
        {
            writer.WriteUInt32(MajorBrand);
            writer.WriteUInt32(MinorVersion);
            CompatibleBrands.ForEach(writer.WriteUInt32);
            return ContentSize;
        }

        public override string ToString()
        {
            return base.ToString() +
                   $"Major:{MajorBrandName} Minor:{MinorVersion}, Brands:{string.Join(",", CompatibleBrands.Select(brand => brand.GetFourCC()))}";
        }
    }
}
