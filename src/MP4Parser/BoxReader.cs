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
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Media.ISO
{
    /// <summary>
    /// A utility class the extends BinaryReader and reads values in big endian format from a stream.
    /// </summary>
    public class BoxReader
    {
        public  readonly Stream BaseStream;
        /// <summary>
        /// Construct with a given stream.
        /// </summary>
        public BoxReader(Stream stream)
        {
            BaseStream = stream;
        }

        private int Read(Span<byte> buffer)
        {
            var bytes = BaseStream.Read(buffer);
            if (bytes == 0)
            {
                throw new EndOfStreamException();
            }
            return bytes;
        }

        public short ReadInt16()
        {
            Span<byte> shortValue = stackalloc byte[2];
            Read(shortValue);
            return BinaryPrimitives.ReadInt16BigEndian(shortValue);
        }

        public int ReadInt32()
        {
            Span<byte> intValue = stackalloc byte[4];
            intValue = intValue.Slice(0, Read(intValue));
            return BinaryPrimitives.ReadInt32BigEndian(intValue);
        }

        public long ReadInt64()
        {
            Span<byte> longValue = stackalloc byte[8];
            Read(longValue);
            return BinaryPrimitives.ReadInt64BigEndian(longValue);
        }

        public ushort ReadUInt16()
        {
            return (ushort) ReadInt16();
        }

        public uint ReadUInt32()
        {
            return (uint) ReadInt32();
        }

        public ulong ReadUInt64()
        {
            return (ulong) ReadInt64();
        }

        /// <summary>
        /// Read a GUID value from the byte stream.
        /// </summary>
        public Guid ReadGuid()
        {
            Span<byte> array = stackalloc byte[16];
            Read(array);
            array.Reverse();
            return new Guid(array);
        }

        /// <summary>
        /// This function reads an 1/2/3/4-byte big-endian field from disk.
        /// </summary>
        /// <param name="bytes">The size of the field in bytes.</param>
        public uint ReadVariableLengthField(int bytes)
        {
            uint value = 0;

            if (bytes > 4 || bytes < 1)
            {
                throw new ArgumentException("bytes must be 1/2/3/4 only");
            }

            for (int i = 0; i < bytes; i ++)
            {
                value <<= 8;
                value |= (byte) BaseStream.ReadByte();
            }

            return value;
        }

        public void SkipBytes(int bytesToSkip)
        {
            if (BaseStream.CanSeek)
            {
                BaseStream.Position += bytesToSkip;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[Math.Min(8 * 1024, bytesToSkip)];
                while (bytesToSkip > 0)
                {
                    int bytesRead = Read(buffer);
                    bytesToSkip -= bytesRead;
                }
            }
        }

        public string ReadString()
        {
            var builder = new StringBuilder();
            int c;
            while ((c = BaseStream.ReadByte()) != 0 && c != -1)
            {
                builder.Append((char)c);
            }
            return builder.ToString();
        }
    }
}
