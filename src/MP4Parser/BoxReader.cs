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
using System.IO;
using System.Net;

namespace Media.ISO
{
    /// <summary>
    /// A utility class the extends BinaryReader and reads values in big endian format from a stream.
    /// </summary>
    public class BoxReader : BinaryReader
    {
        /// <summary>
        /// Construct with a given stream.
        /// </summary>
        public BoxReader(Stream stream) :
            base(stream)
        {
        }

        public override short ReadInt16()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt16());
        }

        public override int ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt32());
        }

        public override long ReadInt64()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt64());
        }

        public override ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public override uint ReadUInt32()
        {
            return (uint) ReadInt32();
        }

        public override ulong ReadUInt64()
        {
            return (ulong) ReadInt64();
        }

        /// <summary>
        /// Read a GUID value from the byte stream.
        /// </summary>
        public Guid ReadGuid()
        {
            Int32 data1 = ReadInt32();
            Int16 data2 = ReadInt16();
            Int16 data3 = ReadInt16();
            Byte[] data4 = ReadBytes(8);
            return new Guid(data1, data2, data3, data4);
        }

        /// <summary>
        /// This function reads an 1/2/3/4-byte big-endian field from disk.
        /// </summary>
        /// <param name="bytes">The size of the field in bytes.</param>
        public UInt32 ReadVariableLengthField(int bytes)
        {
            uint value = 0;

            if (bytes > 4 || bytes < 1)
            {
                throw new ArgumentException("bytes must be 1/2/3/4 only");
            }

            for (int i = 0; i < bytes; i ++)
            {
                value <<= 8;
                value |= ReadByte();
            }

            return value;
        }

        public void SkipBytes(long bytesToSkip)
        {
            if (BaseStream.CanSeek)
            {
                BaseStream.Position += bytesToSkip;
            }
            else
            {
                byte[] buffer = new byte[Math.Min(16 * 1204, bytesToSkip)];
                while (bytesToSkip > 0)
                {
                    int bytesRead = Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        throw new EndOfStreamException("Reached end of stream.");
                    }
                    bytesToSkip -= bytesRead;
                }
            }
        }
    }
}
