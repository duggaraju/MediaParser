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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Media.ISO.Boxes
{
    public record struct BoxHeader(long Size, bool LongSize, BoxType Type, Guid? ExtendedType)
    {
        public void Parse(ReadOnlySpan<byte> buffer)
        {
            Size = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            buffer = buffer.Slice(4);
            Type = (BoxType)BinaryPrimitives.ReadUInt32BigEndian(buffer);
            if (Size == 0 && buffer.Length >= 8)
            {
                LongSize = true;
                Size = BinaryPrimitives.ReadInt64BigEndian(buffer);
                buffer = buffer.Slice(8);
            }
            if (Type == BoxType.UuidBox && buffer.Length >= 16)
            {
                ExtendedType = new Guid(buffer);
            }
        }

        void Write(Span<byte> buffer)
        {

        }
    }

    /// <summary>
    /// Common base class for all MP4 boxes.
    /// </summary>
    [DebuggerDisplay("Box={Name} Size={Size}")]
    public class Box
    {
        internal bool _forceLongSize = false;

        /// <summary>
        /// The type of the box.
        /// </summary>
        public BoxType Type { get; }

        /// <summary>
        /// The size of the box.
        /// </summary>
        public long Size { get; set; }

        public List<Box> Children { get; }

        public Guid? ExtendedType { get; set; }

        public Memory<byte> Body { get; set; } = Array.Empty<byte>();

		public Box(BoxType type, Guid? extendedType = null)
        {
            Type = type;
		    ExtendedType = extendedType;
            Children = new List<Box>();
        }

        internal Box(BoxHeader header) : this(header.Type, header.ExtendedType)
        {
            Size = header.Size;
            _forceLongSize = header.LongSize;
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
                ParseHeader(reader);
                bytes += HeaderSize;

                if (CanHaveChildren && depth > 0)
                {
                    ParseChildren(reader, depth--);
                    bytes += Children.Sum(box => box.Size);
                }
                else
                {
                    ParseBoxContent(reader);
                    bytes += BoxContentSize;
                }
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

        /// <summary>
        /// Parse the box header. 
        /// </summary>
        protected virtual void ParseHeader(BoxReader reader)
        {
            // We already know the type and size of the box before we are here. so nothing to parse.
        }


	    protected virtual void ParseBoxContent(BoxReader reader)
	    {
	        var boxBody = Size - HeaderSize;
            if (boxBody > 0)
            {
                Body = new byte[boxBody];
                var bytes = reader.BaseStream.Read(Body.Span);
                if (bytes < boxBody)
                {
                    throw new ParseException(
                        $"The stream is smaller than the box size. Box:{Name} Size:{Size}  Bytes read:{bytes}");
                }
            }
        }

        protected void ParseChildren(BoxReader reader, int depth = int.MaxValue)
	    {
            long bytes = 0;
            while (bytes < Size - HeaderSize)
            {
                var box = BoxFactory.Parse(reader, depth - 1);
                Children.Add(box);
                bytes += box.Size;
            }
        }

        public static bool TryParseHeader(BoxReader reader, out BoxHeader header)
        {
            try
            {
                if (!reader.TryReadUInt32(out var value))
                {
                    header = default;
                    return false;
                }

                long size = value;
                var longSize = false;
                var type = (BoxType) reader.ReadUInt32();
                Guid? extendedType = null;

                if (size == 0)
                {
                    throw new ArgumentException("Box with size 0 is not supported!");
                }
                else if (size == 1)
                {
                    longSize = true;
                    size = reader.ReadInt64();
                }

                if (type == BoxType.UuidBox)
                {
                    extendedType = reader.ReadGuid();
                }
                header = new BoxHeader(size, longSize, type, extendedType);
                return true;
            }
            catch (Exception e)
            {
                throw new ParseException("Failed to parse box details!", e);
            }
        }

        /// <summary>
        /// Get the name of the box in a printer friendly string.
        /// </summary>
        public string Name => Type.GetBoxName();


        /// <summary>
        /// Compute the size of the box itself (without any children).
        /// </summary>
        /// <returns></returns>
	    protected virtual int HeaderSize
        {
            get
            {
                var size = 8;

                if (Size > uint.MaxValue || _forceLongSize)
                {
                    size += 8;
                }

                if (ExtendedType.HasValue)
                {
                    size += 16;
                }

                return size;
            }
        }

        protected virtual int BoxContentSize => Body.Length;

        /// <summary>
        /// Compute the size of the box and upadte the Size field.
        /// </summary>
        /// <returns>The compute size</returns>
        public long ComputeSize()
        {
            long size = HeaderSize;
            if (CanHaveChildren)
            {
                size += Children.Sum(child => child.ComputeSize());
            }
            else
            {
                size += BoxContentSize;
            }
            Size = size;
            return size;
        }

        public virtual bool CanHaveChildren => false;

        /// <summary>
        /// Write the content of the box to a writer.
        /// </summary>
        public int Write(BoxWriter writer)
        {
            var bytes = HeaderSize;
            WriteBoxHeader(writer);
            if (CanHaveChildren)
            {
                Children.ForEach(child => bytes += child.Write(writer));
            }
            else
            {
                bytes += BoxContentSize;
                WriteBoxContent(writer);
            }
            if (bytes > Size)
            {
                Trace.TraceError("Wrote more bytes for Box:{0} Expected:{1} Actual:{2}", Name, Size, bytes);
                throw new ParseException(
                    string.Format(
                        "Serialization wrote more bytes than the size of the box! Exptected:{0} Actual:{1}",
                        Size,
                        bytes));
            }

            Trace.TraceInformation("Writing Box: '{0}' Size: {1}", Name, Size);
            return bytes;
        }

        protected virtual void WriteBoxHeader(BoxWriter writer)
        {
            var extendedSize = false;
	        if (Size < uint.MaxValue || _forceLongSize)
	        {
	            writer.WriteUInt32((uint) Size);
	        }
	        else
	        {
                extendedSize = true;
	            writer.WriteUInt32(1U);
	        }
            writer.WriteUInt32((uint)Type);
            if (extendedSize)
            {
                writer.WriteInt64(Size);
            }

            if (ExtendedType.HasValue)
	        {
	            writer.Write(ExtendedType.Value);
	        }
	    }

        /// <summary>
        /// Writes the box contents to the writer.
        /// </summary>
	    protected virtual void WriteBoxContent(BoxWriter writer)
	    {
            writer.BaseStream.Write(Body.Span);
	    }

	    public IEnumerable<T> GetChildren<T>() where T:Box
	    {
	        return Children.Where(child => child is T).Cast<T>();
	    }

	    public T GetSingleChild<T>() where T : Box
	    {
	        return GetChildren<T>().Single();
	    }
    }
}
