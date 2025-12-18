//Copyright 2025 Prakash Duggaraju
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
using System.Diagnostics;
using System.Reflection;

namespace Media.ISO.Boxes
{
    public record struct BoxHeader(BoxType Type, long BoxSize = 0, Guid? ExtendedType = null, bool LongSize = false)
    {
        public BoxHeader(BoxAttribute attr) : this(attr.Type, 0, attr.ExtendedType)
        {
        }

        public string Name => Type.GetBoxName();

        public string Description => Type.ToString();

        public int Size => sizeof(uint) + sizeof(uint) + (LongSize ? sizeof(long) : 0) + (ExtendedType == null ? 0 : 16);

        public void Parse(BoxReader reader)
        {
            BoxSize = reader.ReadUInt32();
            Type = (BoxType)reader.ReadUInt32();
            if (BoxSize == 0)
            {
                throw new ArgumentException("Box with size 0 is not supported!");
            }
            else if (BoxSize == 1)
            {
                LongSize = true;
                BoxSize = reader.ReadInt64();
            }
            if (Type == BoxType.UuidBox)
            {
                ExtendedType = reader.ReadGuid();
            }
        }

        public void Write(BoxWriter writer)
        {
            writer.WriteUInt32(LongSize ? 1U : (uint)BoxSize);
            writer.WriteUInt32((uint)Type);
            if (LongSize)
            {
                writer.WriteInt64(BoxSize);
            }
            if (Type == BoxType.UuidBox && ExtendedType.HasValue)
            {
                writer.Write(ExtendedType.Value);
            }
        }

        public void Parse(ReadOnlySpan<byte> buffer)
        {
            BoxSize = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            buffer = buffer.Slice(4);
            Type = (BoxType)BinaryPrimitives.ReadUInt32BigEndian(buffer);
            if (BoxSize == 0 && buffer.Length >= 8)
            {
                LongSize = true;
                BoxSize = BinaryPrimitives.ReadInt64BigEndian(buffer);
                buffer = buffer.Slice(8);
            }
            if (Type == BoxType.UuidBox && buffer.Length >= 16)
            {
                ExtendedType = new Guid(buffer);
            }
        }

        public static bool TryParse(ReadOnlySpan<byte> buffer, out BoxHeader header)
        {
            header = default;
            if (!BinaryPrimitives.TryReadUInt32BigEndian(buffer, out var size))
                return false;
            header.BoxSize = size;
            buffer = buffer.Slice(4);
            if (!BinaryPrimitives.TryReadUInt32BigEndian(buffer, out var type))
                return false;
            header.Type = (BoxType)type;
            buffer = buffer.Slice(4);
            if (size == 0)
            {
                return false;
            }
            else if (size == 1)
            {
                header.LongSize = true;
                if (!BinaryPrimitives.TryReadInt64BigEndian(buffer, out var longSize))
                    return false;
                header.BoxSize = longSize;
                buffer = buffer.Slice(8);
            }

            if (type == (uint)BoxType.UuidBox && buffer.Length < 16)
            {
                return false;
            }
            header.ExtendedType = new Guid(buffer);
            return true;
        }

        public static bool TryParse(BoxReader reader, out BoxHeader header)
        {
            header = default;
            try
            {
                if (!reader.TryReadUInt32(out var value))
                {
                    return false;
                }

                if (value == 0)
                {
                    throw new ArgumentException("Box with size 0 is not supported!");
                }

                header.BoxSize = (long)value;
                header.Type = (BoxType)reader.ReadUInt32();
                if (value == 1)
                {
                    header.LongSize = true;
                    header.BoxSize = reader.ReadInt64();
                }

                if (header.Type == BoxType.UuidBox)
                {
                    header.ExtendedType = reader.ReadGuid();
                }
                return true;
            }
            catch (Exception e)
            {
                throw new ParseException($"Failed to parse box {header}!", e);
            }
        }
    }

    /// <summary>
    /// Common base class for all MP4 boxes.
    /// </summary>
    [DebuggerDisplay("Box={Name} Size={Size}")]
    public abstract class Box
    {
        protected BoxHeader _header;

        /// <summary>
        /// The type of the box.
        /// </summary>
        public BoxType Type => _header.Type;

        /// <summary>
        /// The size of the box.
        /// </summary>
        public long Size => _header.BoxSize;

        public Guid? ExtendedType => _header.ExtendedType;

        public Box()
        {
            var attr = GetType().GetCustomAttribute<BoxAttribute>() ?? throw new InvalidOperationException(
                $"BoxType attribute is missing on box class {GetType().FullName}");
            _header = new BoxHeader(attr);
        }


        public Box(BoxType type, Guid? extendedType = null)
        {
            _header.Type = type;
            _header.ExtendedType = extendedType;
        }

        protected Box(BoxHeader header)
        {
            _header = header;
        }

        internal void SetHeader(BoxHeader header)
        {
            _header = header;
        }

        protected Box(string boxName, Guid? extendedType = null)
            : this(boxName.GetBoxType(), extendedType)
        {
        }

        public override string ToString()
        {
            return $"Box:{Name} Size:{Size}";
        }

        internal void Parse(BoxReader reader, int depth = int.MaxValue)
        {
            long bytes = 0;
            try
            {
                bytes += ParseHeader(reader);
                bytes += ParseBody(reader, depth);
            }
            catch (Exception e)
            {
                throw new ParseException(
                    $"Failed to parse box content for box:{Name} size: {Size} bytes read:{bytes}",
                    e);
            }

            if (bytes != Size)
            {
                throw new ParseException(
                    $"Unparsed box content at the end of box: {Name} size: {Size} bytes read : {bytes}");
            }
        }

        protected abstract long ParseBody(BoxReader reader, int depth);

        /// <summary>
        /// Parse the box header.
        /// </summary>
        protected virtual long ParseHeader(BoxReader reader)
        {
            // We already know the type and size of the box before we are here. so nothing to parse.
            return _header.Size;
        }

        /// <summary>
        /// Get the name of the box in a printer friendly string.
        /// </summary>
        public string Name => _header.Name;


        /// <summary>
        /// Compute the size of the box itself (without any children).
        /// </summary>
        /// <returns></returns>
	    protected virtual int HeaderSize => _header.Size;

        /// <summary>
        /// Compute the size of the box and upadte the Size field.
        /// </summary>
        /// <returns>The compute size</returns>
        public long ComputeSize()
        {
            var boxSize = HeaderSize + ComputeBodySize();
            _header.BoxSize = boxSize;
            return boxSize;
        }

        protected abstract long ComputeBodySize();

        /// <summary>
        /// Write the content of the box to a writer.
        /// </summary>
        public long Write(BoxWriter writer)
        {
            long bytes = HeaderSize;
            WriteBoxHeader(writer);
            bytes += WriteBoxBody(writer);
            if (bytes != Size)
            {
                Trace.TraceError("Wrote different bytes for Box:{0} Expected:{1} Actual:{2}", Name, Size, bytes);
                throw new ParseException(
                    $"Serialization wrote more bytes than the size of the box {Name}! Exptected:{Size} Actual:{bytes}");
            }

            Trace.TraceInformation("Writing Box: '{0}' Size: {1}", Name, Size);
            return bytes;
        }

        protected abstract long WriteBoxBody(BoxWriter writer);

        protected virtual void WriteBoxHeader(BoxWriter writer)
        {
            _header.Write(writer);
        }
    }
}
