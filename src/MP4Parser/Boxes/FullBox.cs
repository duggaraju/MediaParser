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

namespace Media.ISO.Boxes
{
    /// <summary>
    /// Represents a full ISO box with version and flags.
    /// </summary>
    public class FullBox : Box
    {
        public FullBox(): base()
        {
        }

        protected FullBox(BoxType type, Guid? extendedType = null) :
            base(type, extendedType)
        {
        }

        /// <summary>
        /// Construct a full box using a box name.
        /// </summary>
        public FullBox(string boxName) : base(boxName)
        {
        }

        protected sealed override long ComputeBodySize() => ContentSize;

        protected override int HeaderSize => base.HeaderSize + 4;

        protected override sealed long ParseHeader(BoxReader reader)
        {
            _versionAndFlags = reader.ReadUInt32();
            return _header.Size + 4;
        }

        protected override sealed void WriteBoxHeader(BoxWriter writer)
        {
            base.WriteBoxHeader(writer);
            writer.WriteUInt32(_versionAndFlags);
        }

        public byte Version
        {
            get { return (byte)(_versionAndFlags >> 24); }
            set
            {
                var version = (uint)value << 24;
                _versionAndFlags = version | Flags;
            }
        }

        public uint Flags
        {
            get { return (_versionAndFlags & 0xFFFFFF); }
            set
            {
                if (value > 0xFFFFFF)
                {
                    throw new ArgumentException("Flags cannot be greater than 0xFFFFFF", nameof(value));
                }
                _versionAndFlags &= 0xFF000000;
                _versionAndFlags |= value;
            }
        }

        private uint _versionAndFlags;

        protected override long ParseBoxBody(BoxReader reader, int depth)
        {
            ParseBoxContent(reader);
            return ContentSize;
        }

        protected override long WriteBoxBody(BoxWriter writer)
        {
            WriteBoxContent(writer);
            return ContentSize;
        }

        protected virtual int ContentSize => (int) Size - HeaderSize;

        protected virtual void WriteBoxContent(BoxWriter writer)
        {
            writer.SkipBytes(ContentSize);
        }

        protected virtual void ParseBoxContent(BoxReader reader)
        {
            reader.SkipBytes(ContentSize);
        }
    }
}
