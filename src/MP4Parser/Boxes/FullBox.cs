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
    public record struct VersionAndFlags(uint Value)
    {
        public byte Version
        {
            get { return (byte)(Value >> 24); }
            set
            {
                var version = (uint)value << 24;
                Value = version | Flags;
            }
        }

        public uint Flags
        {
            get { return (Value & 0xFFFFFF); }
            set
            {
                if (value > 0xFFFFFF)
                {
                    throw new ArgumentException("Flags cannot be greater than 0xFFFFFF", nameof(value));
                }
                Value &= 0xFF000000;
                Value |= value;
            }
        }
    }

    /// <summary>
    /// Represents a full ISO box with version and flags.
    /// </summary>
    public class FullBox : Box
    {
        protected FullBox() : base()
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

        protected override int HeaderSize => base.HeaderSize + sizeof(uint);

        protected override sealed long ParseHeader(BoxReader reader)
        {
            _versionAndFlags = new VersionAndFlags(reader.ReadUInt32());
            return _header.Size + sizeof(uint);
        }

        protected override sealed void WriteBoxHeader(BoxWriter writer)
        {
            base.WriteBoxHeader(writer);
            writer.WriteUInt32(_versionAndFlags.Value);
        }

        public byte Version
        {
            get => _versionAndFlags.Version;
            set => _versionAndFlags.Version = value;
        }

        public uint Flags
        {
            get => _versionAndFlags.Flags;
            set => _versionAndFlags.Flags = value;
        }

        private VersionAndFlags _versionAndFlags;

        protected override long ParseBody(BoxReader reader, int depth)
        {
            ParseBoxContent(reader);
            return ContentSize;
        }

        protected override long WriteBoxBody(BoxWriter writer)
        {
            WriteBoxContent(writer);
            return ContentSize;
        }

        protected virtual int ContentSize => (int)Size - HeaderSize;

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
