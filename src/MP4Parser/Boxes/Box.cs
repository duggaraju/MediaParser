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
    public record struct BoxHeader(long Size, uint Type, Guid? ExtendedType);

	/// <summary>
	/// Common base class for all MP4 boxes.
	/// </summary>
    public class Box
    {
        /// <summary>
        /// The type of the box.
        /// </summary>
        public uint Type { get; }

        /// <summary>
        /// The size of the box.
        /// </summary>
        public long Size { get; set; }

        public List<Box> Children { get; }

        public Guid? ExtendedType { get; set; }

        public Memory<byte> Body { get; set; } = Array.Empty<byte>();

		public Box(uint type, Guid? extendedType = null)
        {
            Type = type;
		    ExtendedType = extendedType;
            Children = new List<Box>();
        }

        protected Box(string boxName, Guid? extendedType = null)
            : this(boxName.GetBoxType(), extendedType)
	    {
	    }

		public override string ToString()
        {
            return $"Box:{Name} Size:{Size}";
        }

		internal void Parse(BoxReader reader, long boxOffset, int depth = int.MaxValue)
        {
            long boxEnd = Size == 0 ? reader.BaseStream.Length : boxOffset + Size;
		    try
		    {
                ParseHeader(reader);

                if (CanHaveChildren && depth > 0)
                {
                    ParseChildren(reader, boxEnd);
                }
                else
                {
                    ParseContent(reader, boxEnd);
                }
		    }
		    catch (Exception e)
		    {
                throw new ParseException(
                    $"Failed to parse box content for box:{Name} at offset:{reader.BaseStream.Position}", 
                    e);
		    }

		    if (reader.BaseStream.Position != boxEnd)
		    {
		        throw new ParseException(
                    $"Unparsed box content at the end of box: {this} Offset:{reader.BaseStream.Position}");
		    }
        }

        /// <summary>
        /// Parse the box header. 
        /// </summary>
        protected virtual void ParseHeader(BoxReader reader)
        {
            // We already know the type and size of the box before we are here. so nothing to parse.
        }


	    protected virtual void ParseContent(BoxReader reader, long boxEnd)
	    {
	        long boxBody = boxEnd - reader.BaseStream.Position;
	        if (reader.BaseStream.Length - reader.BaseStream.Position < boxBody)
	        {
	            throw new ParseException(
                    $"The stream is smaller than the box size. Box:{Name} Size:{Size} stream Offset:{reader.BaseStream.Position} Length:{reader.BaseStream.Length}");
	        }
            if (boxBody > 0)
            {
                Body = new byte[boxBody];
                reader.BaseStream.Read(Body.Span);
            }
        }

        protected void ParseChildren(BoxReader reader, long boxEnd, int depth = int.MaxValue)
	    {
	        while (reader.BaseStream.Position < boxEnd)
	        {
	            var box = BoxFactory.Parse(reader, depth - 1);
                Children.Add(box);
	        }
	    }

        public static BoxHeader ParseBoxHeader(BoxReader reader)
        {
            try
            {
                long size = reader.ReadUInt32();
                var type = reader.ReadUInt32();
                Guid? extendedType = null;

                if (size == 0)
                {
                    //box is till the end of the stream.
                    size = reader.BaseStream.Length;
                }
                else if (size == 1)
                {
                    size = reader.ReadInt64();
                }

                if (type == BoxConstants.UuidBoxType)
                {
                    extendedType = reader.ReadGuid();
                }
                return new BoxHeader(size, type, extendedType);
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
	    protected virtual long HeaderSize
        {
            get
            {
                long size = 8;

                if (Size > uint.MaxValue)
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

        protected virtual long BoxContentSize => Body.Length;

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
        public void Write(BoxWriter writer)
        {
            var startPosition = writer.BaseStream.Position;
            WriteBoxHeader(writer);
            if (CanHaveChildren)
            {
                Children.ForEach(child => child.Write(writer));
            }
            else
            {
                WriteBoxContent(writer);
            }
            if (writer.BaseStream.Position - startPosition > Size)
            {
                Trace.TraceError("Wrote more bytes for Box:{0} Expected:{1} Actual:{2}", Name, Size,
                    writer.BaseStream.Position - startPosition);
                throw new ParseException(
                    string.Format(
                        "Serialization wrote more bytes than the size of the box! Exptected:{0} Actual:{1}",
                        Size,
                        writer.BaseStream.Position - startPosition));
            }

            Trace.TraceInformation(
                "Writing Box :{0} Size:{1} Start:{2} End:{3}", Name, Size, startPosition, writer.BaseStream.Position);
        }

        protected virtual void WriteBoxHeader(BoxWriter writer)
        {
	        if (Size < uint.MaxValue)
	        {
	            writer.WriteUInt32((uint) Size);
	        }
	        else
	        {
	            writer.WriteUInt32(1U);
	        }
            writer.WriteUInt32(Type);
	        if (ExtendedType.HasValue)
	        {
	            writer.Write(ExtendedType.Value);
	        }
	    }

        protected void WriteHeader(Span<byte> buffer)
        {
            if (buffer.Length < Size)
            {
                throw new ArgumentException("Insufficient buffer", nameof(buffer));
            }
            BinaryPrimitives.WriteUInt32BigEndian(buffer, Type);
            buffer = buffer.Slice(4);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Size);
            buffer = buffer.Slice(4);
            if (ExtendedType.HasValue)
            {
                ExtendedType.Value.TryWriteBytes(buffer);
                buffer = buffer.Slice(16);
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
            var type = BoxExtensions.GetBoxType<T>();
	        return Children.Where(child => child.Type == type).Cast<T>();
	    }

	    public T GetSingleChild<T>() where T : Box
	    {
	        return GetChildren<T>().Single();
	    }
    }
}
