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

namespace Media.ISO
{
    /// <summary>
    /// Implements a writer class that writes data in big endian format to stream.
    /// </summary>
    public class BoxWriter
    {
        public readonly Stream BaseStream;

        /// <summary>
        /// Construct a writer that writes to the given stream.
        /// </summary>
        public BoxWriter(Stream stream)
        {
            BaseStream = stream;
        }

        #region signed writes

        public void Write(ReadOnlySpan<byte> data)
        {
            BaseStream.Write(data);
        }

        public void WriteInt16(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(buffer, value);
            Write(buffer);
        }

        public void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            Write(buffer);
        }

        public void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, value);
            Write(buffer);
        }

        #endregion

        #region unsigned writes

        /// <summary>
        /// Writes a unsigned 16 bit integer
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteUInt16(ushort value)
        {
            WriteInt16((short)value);
        }

        /// <summary>
        /// Writes an unsigned 32 bit integer in big endian format.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteUInt32(uint value)
        {
            WriteInt32((int)value);
        }

        /// <summary>
        /// Writes an unsigned 64 bit integer in big endian format.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt64(ulong value)
        {
            WriteInt64((long)value);
        }

        #endregion


        /// <summary>
        /// This function writes an 8/16/24/32-bit big-endian.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="bytes">The size of the field in bytes (1/2/3/4).</param>
        public void WriteVariableLengthField(uint value, int bytes)
        {
            if (bytes <1 || bytes > 4)
            {
                throw new ArgumentException("bytes must be 1/2/3/4");
            }

            for (int i = bytes - 1 ; i >= 0; --i)
            {
                byte output = (byte)(value >> (i * 8));
                BaseStream.WriteByte(output);
            }
        }

        /// <summary>
        /// Write a GUID Value in the big endian order.
        /// </summary>
        public void Write(Guid value)
        {
            Span<byte> buffer = stackalloc byte[16];
            if (value.TryWriteBytes(buffer))
            {
                buffer.Reverse();
            }
            Write(buffer);
        }

        public void SkipBytes(int bytes)
        {
            if (BaseStream.CanSeek)
            {
                BaseStream.Position += bytes;
            }
            else
            {
                // Fill with 0s.
                Span<byte> buffer = stackalloc byte[Math.Min(bytes, 16*1024)];
                while (bytes > 0)
                {
                    if (bytes < buffer.Length)
                    {
                        buffer = buffer.Slice(0, bytes);
                    }
                    Write(buffer);
                    bytes -= buffer.Length;
                }
            }
        }

        public void WriteString(string value)
        {
            foreach (char c in value)
            {
                BaseStream.WriteByte((byte)c);
            }
            BaseStream.WriteByte(0);
        }
    }
}
