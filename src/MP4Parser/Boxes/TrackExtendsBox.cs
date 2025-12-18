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
    [FullBox(BoxType.TrackExtendsBox)]
    public partial class TrackExtendsBox
    {
        public int TrackId { get; set; }

        public ulong DefaultSampleDescriptionIndex { get; set; } = 0;

        public ulong DefaultSampleDuration { get; set; } = 0;

        public ulong DefaultSampleSize { get; set; } = 0;

        public uint DefaultSampleFlags { get; set; } = 0;

        protected override int ContentSize =>
            4 + // track id
            4 + // reserved
            8 + // default sample description index
            8 + // default sample duration
            8 + // default sample size
            4;   // default sample flags

        protected override void ParseBoxContent(BoxReader reader)
        {
            TrackId = reader.ReadInt32();
            reader.ReadUInt32(); // reserved
            DefaultSampleDescriptionIndex = reader.ReadUInt64();
            DefaultSampleDuration = reader.ReadUInt64();
            DefaultSampleSize = reader.ReadUInt64();
            DefaultSampleFlags = reader.ReadUInt32();
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
        }
    }
}
