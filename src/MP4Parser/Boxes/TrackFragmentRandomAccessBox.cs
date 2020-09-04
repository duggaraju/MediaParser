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

namespace Media.ISO.Boxes
{
    [BoxType(BoxConstants.TrackFragmentRandomAccessBox)]
    public class TrackFragmentRandomAccessBox : FullBox
    {
        public TrackFragmentRandomAccessBox()
            : base(BoxConstants.TrackFragmentRandomAccessBox)
        {
        }

        public uint TrackId { get; set; }

        /// <summary>
        /// An entry in the tfra box.
        /// </summary>
        public struct Entry
        {
            /// <summary>
            /// Timestamp of the first sample in moof.
            /// </summary>
            public ulong Time { get; set; }
            /// <summary>
            /// Offset of moof in the file.
            /// </summary>
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
        }

        /// <summary>
        ///  The random access entries for this track.
        /// </summary>
        public readonly List<Entry> Entries = new List<Entry>();

        /// <summary>
        /// Parse the box content.
        /// </summary>
        protected override void ParseBoxContent(BoxReader reader, long boxEnd)
        {
            TrackId = reader.ReadUInt32();
            _sizeOfEntry = reader.ReadInt32();
            Entries.Clear();
            Entries.Capacity = reader.ReadInt32();
            for(int i = 0; i < Entries.Capacity; ++i)
            {
                Entry entry = new Entry();
                if (Version == 1)
                {
                    entry.Time= reader.ReadUInt64();
                    entry.MoofOffset = reader.ReadUInt64();
                }
                else
                {
                    entry.Time = reader.ReadUInt32();
                    entry.MoofOffset = reader.ReadUInt32();
                }
                entry.TrackFragmentNumber = reader.ReadVariableLengthField(SizeOfTrackFragmentNumber + 1);
                entry.TrackRunNumber = reader.ReadVariableLengthField(SizeOfTrackRunNumber + 1);
                entry.SampleNumber = reader.ReadVariableLengthField(SizeOfSampleNumber + 1);
                Entries.Add(entry);
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32(TrackId);
            writer.WriteInt32(_sizeOfEntry);
            writer.WriteInt32(Entries.Count);
            Entries.ForEach(entry =>
            {
                if (Version == 1)
                {
                    writer.WriteUInt64(entry.Time);
                    writer.WriteUInt64(entry.MoofOffset);
                }
                else
                {
                    writer.WriteUInt32((uint)entry.Time);
                    writer.WriteUInt32((uint)entry.MoofOffset);
                }
                writer.WriteVariableLengthField(entry.TrackFragmentNumber, SizeOfTrackFragmentNumber + 1);
                writer.WriteVariableLengthField(entry.TrackRunNumber, SizeOfTrackRunNumber + 1);
                writer.WriteVariableLengthField(entry.SampleNumber, SizeOfSampleNumber + 1);
            });
        }

        public int SizeOfTrackFragmentNumber
        {
            get { return (_sizeOfEntry & 0x30) >> 4; }
        }

        public int SizeOfTrackRunNumber
        {
            get { return (_sizeOfEntry & 0xC0) >> 2; }
        }

        public int SizeOfSampleNumber
        {
            get {  return _sizeOfEntry & 0x3; }
        }

        private int _sizeOfEntry;
    }
}
