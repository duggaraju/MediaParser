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
    [FullBox(BoxType.TrackFragmentRandomAccessBox)]
    public partial class TrackFragmentRandomAccessBox
    {
        public uint TrackId { get; set; }

        public uint EntrySize { get; set; }

        /// <summary>
        /// An entry in the tfra box.
        /// </summary>
        [VersionDependentSize]
        public struct Entry
        {
            /// <summary>
            /// Timestamp of the first sample in moof.
            /// </summary>
            [VersionDependentSize]
            public ulong Time { get; set; }

            /// <summary>
            /// Offset of moof in the file.
            /// </summary>
            [VersionDependentSize]
            public ulong MoofOffset { get; set; }

            /// <summary>
            /// The track fragment number in the moof.
            /// </summary>
            public uint TrackFragmentNumber { get; set; }

            /// <summary>
            /// The Track Run number in the moof.
            /// </summary>
            public uint TrackRunNumber { get; set; }

            /// <summary>
            /// Sample number in the track run that is the random access point.
            /// </summary>
            public uint SampleNumber { get; set; }

            public static int SampleNumberSize(uint flags)
            {
                var size = (flags & 0x03);
                return (int)size + 1;
            }

            public static int TrackRunNumberSize(uint flags)
            {
                var size = (flags & 0x0C) >> 2;
                return (int)size + 1;
            }

            public static int TrackFragmentNumberSize(uint flags)
            {
                var size = (flags & 0x30) >> 4;
                return (int)size + 1;
            }

            public void Write(BoxWriter writer, VersionAndFlags versionAndFlags)
            {
                if (versionAndFlags.Version == 1)
                {
                    writer.WriteUInt64(Time);
                    writer.WriteUInt64(MoofOffset);
                }
                else
                {
                    writer.WriteUInt32((uint)Time);
                    writer.WriteUInt32((uint)MoofOffset);
                }

                var flags = versionAndFlags.Flags;
                writer.WriteVariableLengthField(TrackFragmentNumber, TrackFragmentNumberSize(flags));
                writer.WriteVariableLengthField(TrackRunNumber, TrackRunNumberSize(flags));
                writer.WriteVariableLengthField(SampleNumber, SampleNumberSize(flags));
            }

            public void Read(BoxReader reader, VersionAndFlags versionAndFlags)
            {
                if (versionAndFlags.Version == 1)
                {
                    Time = reader.ReadUInt64();
                    MoofOffset = reader.ReadUInt64();
                }
                else
                {
                    Time = reader.ReadUInt32();
                    MoofOffset = reader.ReadUInt32();
                }

                var flags = versionAndFlags.Flags;
                TrackFragmentNumber = reader.ReadVariableLengthField(TrackFragmentNumberSize(flags));
                TrackRunNumber = reader.ReadVariableLengthField(TrackRunNumberSize(flags));
                SampleNumber = reader.ReadVariableLengthField(SampleNumberSize(flags));
            }

            public int ComputeSize(VersionAndFlags versionAndFlags)
            {
                var baseSize = versionAndFlags.Version == 1
                    ? sizeof(ulong) * 2
                    : sizeof(uint) * 2; // Time + MoofOffset
                var flags = versionAndFlags.Flags;
                return baseSize + SampleNumberSize(flags) + TrackRunNumberSize(flags) + TrackFragmentNumberSize(flags);
            }
        }

        /// <summary>
        ///  The random access entries for this track.
        /// </summary>
        [CollectionLengthPrefix(typeof(uint))]
        public List<Entry> Entries { get; private set; } = new ();

    }
}
