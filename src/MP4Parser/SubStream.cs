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

namespace Media.ISO
{
    /// <summary>
    /// A stream implementation over a partial range of the bytes.
    /// </summary>
    public class SubStream : Stream
    {

        public SubStream(Stream baseStream, long offset, long length)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            if (!baseStream.CanRead || !baseStream.CanSeek)
            {
                throw new ArgumentException("Stream must be readable and seekable!", "baseStream");
            }

            if (offset < 0 || offset >= baseStream.Length)
                throw new ArgumentException("Offset cannot be negative or greater than stream length.", "offset");
            
            if (length < 0 || offset + length > baseStream.Length)
                throw new ArgumentException("length cannot be negative or point beyond the strem length.", "length");

            _length = length;
            _offset = offset;
            _stream = baseStream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_offset + offset > _stream.Length)
                throw new ArgumentException("Offset out of range!", nameof(offset));
            var remaining = Length - Position;
            if (remaining <= 0)
                return 0;
            if (remaining < count)
            {
                count = (int) remaining;
            }
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _length;

        public override long Position 
        {
            get { return _stream.Position - _offset; }
            set { _stream.Position = _offset + value;  }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_stream != null)
                {
                    try
                    {
                        _stream.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error:{0}", ex);
                        throw;
                    }
                    _stream = null;
                }
            }
        }

        private Stream _stream;

        private readonly long _offset;

        private readonly long _length;
    }
}
