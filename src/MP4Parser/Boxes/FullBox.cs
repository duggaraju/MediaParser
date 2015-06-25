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

        public FullBox(uint type, Guid? extendedType) :
            base(type, extendedType)
        {
            
        }

        protected override long GetBoxSize()
        {
            return base.GetBoxSize() + 4;
        }

        protected override void ParseBoxContent(BoxReader reader, long boxEnd)
        {
            _versionAndFlags = reader.ReadUInt32();
        }

        protected override void WriteBoxHeader(BoxWriter writer)
        {
            base.WriteBoxHeader(writer);
            writer.WriteUInt32(_versionAndFlags);
        }

        public byte Version
        {
            get { return (byte) (_versionAndFlags >> 24); }
            set { _versionAndFlags |= ((uint)value << 24); }
        }

        public uint Flags
        {
            get { return (_versionAndFlags & 0xFFFFF); }
            set { _versionAndFlags |= (value & 0xFFFFF); }
        }

        private uint _versionAndFlags;
    }
}
