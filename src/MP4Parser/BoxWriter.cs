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
using System.IO;
using System.Net;

namespace Media.ISO
{
    /// <summary>
    /// Implements a writer class that writes data in big endian format to stream.
    /// </summary>
    public class BoxWriter : BinaryWriter
    {
        /// <summary>
        /// Construct a writer that writes to the given stream.
        /// </summary>
        public BoxWriter(Stream stream) :
            base(stream)
        {
        }

        #region signed writes

        public void WriteInt16(short value)
        {
            base.Write(IPAddress.HostToNetworkOrder(value));
        }

        public void WriteInt32(int value)
        {
            base.Write(IPAddress.HostToNetworkOrder(value));
        }

        public void WriteInt64(long value)
        {
            base.Write(IPAddress.HostToNetworkOrder(value));
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

            for (int i = 0; i < bytes; ++i)
            {
                byte output = (byte)(value >> (bytes * 8));
                Write(output);
            }
        }

        /// <summary>
        /// Write a GUID Value in the big endian order.
        /// </summary>
        public void Write(Guid value)
        {
            Byte[] guid = value.ToByteArray();
            Debug.Assert(16 == guid.Length);
            WriteInt32(BitConverter.ToInt32(guid, 0));
            WriteInt16(BitConverter.ToInt16(guid, 4));
            WriteInt16(BitConverter.ToInt16(guid, 6));
            Write(guid, 8, 8);
        }

        public void SkipBytes(long bytes)
        {
            if (BaseStream.CanSeek)
            {
                BaseStream.Position += bytes;
            }
            else
            {
                // Fill with 0s.
                byte[] buffer = new byte[Math.Min(bytes, 16*1024)];
                Array.Clear(buffer, 0, buffer.Length);
                while (bytes > 0)
                {
                    int bytesToWrite = (int)Math.Min(bytes, buffer.Length);
                    Write(buffer, 0, bytesToWrite);
                    bytes -= buffer.Length;
                }
            }
        }
    }
}
