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

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TrackHeaderBox)]
    public partial class TrackHeaderBox
    {
        [VersionDependentSize]
        public ulong CreationTime { get; set; }

        [VersionDependentSize]
        public ulong ModificationTime { get; set; }

        public uint TrackId { get; set; }

        [Reserved(4)]
        private byte Reserverd1 { get; set; }

        [VersionDependentSize]
        public ulong Duration { get; set; }

        [Reserved(8)]
        private byte Reserved2 { get; set; }

        public short Layer { get; set; }

        public short AlternateGroup { get; set; }

        public short Volume { get; set; }

        [Reserved(2)]
        private byte Reserved3 { get; set; }

        public uint[] Matrix { get; } = new uint[9];

        public uint Width { get; set; }

        public uint Height { get; set; }

    }
}
