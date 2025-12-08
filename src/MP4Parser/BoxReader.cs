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
using System.Threading.Tasks;

namespace Media.ISO
{
    /// <summary>
    /// A utility class the extends BinaryReader and reads values in big endian format from a stream.
    /// </summary>
    public class BoxReader: IDisposable, IAsyncDisposable
    {
        private readonly Stream _innerStream;
        private readonly BufferedStream _bufferedStream;

        public Stream BaseStream => _bufferedStream;

        /// <summary>
        /// Construct with a given stream.
        /// </summary>
        public BoxReader(Stream stream)
        {
            _innerStream = stream;
            _bufferedStream = new BufferedStream(_innerStream);
        }

        public bool TryRead(Span<byte> buffer)
        {
            try
            {
                _bufferedStream.ReadExactly(buffer);
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
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
            TryRead(longValue);
            return BinaryPrimitives.ReadInt64BigEndian(longValue);
        }

        public ushort ReadUInt16()
        {
            return (ushort) ReadInt16();
        }


        public uint ReadUInt32()
        {
            return TryReadInt32(out var value) ? (uint) value : throw new EndOfStreamException();
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
            TryRead(array);
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
                value |= (byte) _bufferedStream.ReadByte();
            }

            return value;
        }

        public void SkipBytes(int bytesToSkip)
        {
            if (_bufferedStream.CanSeek)
            {
                _bufferedStream.Position += bytesToSkip;
            }
            else
            {
                Span<byte> buffer = stackalloc byte[Math.Min(8 * 1024, bytesToSkip)];
                _bufferedStream.ReadExactly(buffer);
            }
        }

        public string ReadString()
        {
            var builder = new StringBuilder();
            int c;
            while ((c = _bufferedStream.ReadByte()) != 0 && c != -1)
            {
                builder.Append((char)c);
            }
            return builder.ToString();
        }

        public void Dispose()
        {
            _bufferedStream.Dispose();
            _innerStream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _bufferedStream.DisposeAsync();
            await _innerStream.DisposeAsync();
        }
    }
}
