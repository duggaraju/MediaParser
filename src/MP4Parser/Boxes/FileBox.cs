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

        [CollectionLengthToEnd]
        public List<uint> CompatibleBrands { get; private set; } = new();

        protected override long ComputeBodySize() => sizeof(uint) + sizeof(uint) + CompatibleBrands.Count * sizeof(uint);

        public override string ToString()
        {
            return base.ToString() +
                   $"Major:{MajorBrandName} Minor:{MinorVersion}, Brands:{string.Join(",", CompatibleBrands.Select(brand => brand.GetFourCC()))}";
        }
    }
}
