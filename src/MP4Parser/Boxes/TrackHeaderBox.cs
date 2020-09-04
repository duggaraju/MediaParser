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

using System;
using System.Diagnostics;

namespace Media.ISO.Boxes
{
    [BoxType(BoxConstants.TrackHeaderBox)]
    public class TrackHeaderBox : FullBox
    {
        public TrackHeaderBox() : base(BoxConstants.TrackHeaderBox)
        {
        }

        public ulong CreationTime { get; set; }

        public ulong ModificationTime { get; set; }

        public uint TrackId { get; set; }

        public ulong Duration { get; set; }

        public short Layer { get; set; }

        public short AlternateGroup { get; set; }

        public short Volume { get; set; }

        public readonly int[] Matrix = new int[9];

        public uint Width { get; set; }

        public uint Height { get; set; }

        /// <summary>
        /// Parse the box content.
        /// </summary>
        protected override void ParseBoxContent(BoxReader reader, long boxEnd)
        {
            if (Version == 1)
            {
                CreationTime = reader.ReadUInt64();
                ModificationTime = reader.ReadUInt64();
                TrackId = reader.ReadUInt32();
                var reserved = reader.ReadUInt32();
                Debug.Assert(reserved == 0);
                Duration = reader.ReadUInt64();
            }
            else
            {
                CreationTime = reader.ReadUInt32();
                ModificationTime = reader.ReadUInt32();
                TrackId = reader.ReadUInt32();
                var reserved = reader.ReadUInt32();
                Debug.Assert(reserved == 0);
                Duration = reader.ReadUInt32();                
            }
            reader.SkipBytes(8);
            Layer = reader.ReadInt16();
            AlternateGroup = reader.ReadInt16();
            Volume = reader.ReadInt16();
            reader.SkipBytes(2);
            for (int i = 0; i < Matrix.Length; ++i)
            {
                Matrix[i] = reader.ReadInt32();
            }
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            if (Version == 1)
            {
                writer.WriteUInt64(CreationTime);
                writer.WriteUInt64(ModificationTime);
                writer.WriteUInt32(TrackId);
                writer.WriteUInt32(0);
                writer.WriteUInt64(Duration);
            }
            else
            {
                writer.WriteUInt32((uint)CreationTime);
                writer.WriteUInt32((uint)ModificationTime);
                writer.WriteUInt32(TrackId);
                writer.WriteUInt32(0);
                writer.WriteUInt32((uint)Duration);
            }
            writer.SkipBytes(8);
            writer.WriteInt16(Layer);
            writer.WriteInt16(AlternateGroup);
            writer.WriteInt16(Volume);
            writer.SkipBytes(2);
            Array.ForEach(Matrix, writer.WriteInt32);
            writer.WriteUInt32(Width);
            writer.WriteUInt32(Height);
        }
    }
}
