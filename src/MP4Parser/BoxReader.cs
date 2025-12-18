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

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Media.ISO
{
    /// <summary>
    /// A utility class the extends BinaryReader and reads values in big endian format from a stream.
    /// </summary>
    public class BoxReader
    {
        public readonly Stream BaseStream;

        /// <summary>
        /// Construct with a given stream.
        /// </summary>
        public BoxReader(Stream stream)
        {
            BaseStream = stream;
        }

        public bool TryRead(Span<byte> buffer)
        {
            var bytes = BaseStream.Read(buffer);
            if (bytes == 0)
                return false; // end of stream.
            if (bytes < buffer.Length)
            {
                throw new EndOfStreamException("Not enough bytes");
            }
            return true;
        }

        public short ReadInt16()
        {
            Span<byte> shortValue = stackalloc byte[2];
            TryRead(shortValue);
            return BinaryPrimitives.ReadInt16BigEndian(shortValue);
        }

        public bool TryReadInt32(out int value)
        {
            value = default;
            Span<byte> intValue = stackalloc byte[4];
            if (TryRead(intValue))
            {
                value = BinaryPrimitives.ReadInt32BigEndian(intValue);
                return true;
            }
            return false;
        }

        public bool TryReadUInt32(out uint value)
        {
            if (TryReadInt32(out var val))
            {
                value = (uint)val;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public int ReadInt32()
        {
            return TryReadInt32(out var value) ? value : throw new EndOfStreamException();
        }

        public long ReadInt64()
        {
            Span<byte> longValue = stackalloc byte[8];
            return TryRead(longValue) ? BinaryPrimitives.ReadInt64BigEndian(longValue) : throw new EndOfStreamException();
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }


        public uint ReadUInt32()
        {
            return TryReadInt32(out var value) ? (uint)value : throw new EndOfStreamException();
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        /// <summary>
        /// Read a GUID value from the byte stream.
        /// </summary>
        public Guid ReadGuid()
        {
            Span<byte> array = stackalloc byte[16];
            if (!TryRead(array)) throw new EndOfStreamException();
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

            for (int i = 0; i < bytes; i++)
            {
                value <<= 8;
                value |= (byte)BaseStream.ReadByte();
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
                    int bytesRead = BaseStream.Read(buffer);
                    if (bytesRead == 0)
                        throw new EndOfStreamException();
                    bytesToSkip -= bytesRead;
                }
            }
        }

        public string ReadString(int length = -1)
        {
            if (length < 0)
            {
                var bytes = new List<byte>();
                int value;
                while ((value = BaseStream.ReadByte()) != -1)
                {
                    if (value == 0)
                    {
                        break;
                    }

                    bytes.Add((byte)value);
                }

                return bytes.Count == 0 ? string.Empty : Encoding.UTF8.GetString(bytes.ToArray());
            }

            if (length == 0)
            {
                return string.Empty;
            }

            Span<byte> buffer = stackalloc byte[length];
            BaseStream.ReadExactly(buffer);

            int end = length;
            if (buffer[end - 1] == 0)
            {
                end--;
            }

            int start = 0;
            bool nullTerminated = end < length;
            if (!nullTerminated)
            {
                start = Math.Min(1, end);
            }

            int count = Math.Max(0, end - start);
            if (count == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer.Slice(start, count));
        }

        public void Read(Span<byte> buffer)
        {
            if (!TryRead(buffer))
            {
                throw new EndOfStreamException();
            }
        }

    }
}
