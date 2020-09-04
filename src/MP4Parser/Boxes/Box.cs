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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Media.ISO.Boxes
{
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

        public Guid? ExtendedType { get; }

        public Stream BoxBody { get; set; }

		public Box(uint type, Guid? extendedType = null)
        {
            Type = type;
		    ExtendedType = extendedType;
            Children = new List<Box>();
        }

        public Box(string boxName, Guid? extendedType = null)
            : this(boxName.GetBoxType(), extendedType)
	    {
	    }

		public override string ToString()
        {
            return $"Box:{Name} Size:{Size} {ExtendedType}";
        }

		internal void Parse(BoxReader reader, long boxOffset, int depth = int.MaxValue)
        {
            long boxEnd = Size == 0 ? reader.BaseStream.Length : boxOffset + Size;
		    try
		    {
                ParseBoxHeader(reader);

                if (CanHaveChildren && depth > 0)
                {
                    ParseChildren(reader, boxEnd);
                }
                else
                {
                    ParseBoxContent(reader, boxEnd);
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
        protected virtual void ParseBoxHeader(BoxReader reader)
        {
            // We already know the type and size of the box before we are here. so nothing to parse.
        }


	    protected virtual void ParseBoxContent(BoxReader reader, long boxEnd)
	    {
	        long boxBody = boxEnd - reader.BaseStream.Position;
	        if (reader.BaseStream.Length - reader.BaseStream.Position < boxBody)
	        {
	            throw new ParseException(
                    $"The stream is smaller than the box size. Box:{Name} Size:{Size} stream Offset:{reader.BaseStream.Position} Length:{reader.BaseStream.Length}");
	        }

	        if (boxBody > 0 && reader.BaseStream.CanSeek)
	        {
	            BoxBody = new SubStream(reader.BaseStream, reader.BaseStream.Position, boxBody);
	            reader.BaseStream.Seek(boxEnd, SeekOrigin.Begin);
	        }
	        else
	        {
                //TODO: Read to a memory stream since the stream is not seekbale.
	            Trace.TraceInformation("Skipping {0} bytes of the unknown box:{1}", boxBody, this);
                reader.SkipBytes(boxBody);
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

        public static long ParseHeader(BoxReader reader, out uint type, out long size, out Guid? extendedType)
        {
            long offset = reader.BaseStream.Position;
            extendedType = null;

            try
            {
                size = reader.ReadUInt32();
                type = reader.ReadUInt32();
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
            }
            catch (Exception e)
            {
                throw new ParseException("Failed to parse box details!", e);
            }

            return reader.BaseStream.Position - offset;
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

        protected virtual long BoxContentSize => BoxBody == null ? 0 : BoxBody.Length;

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

        /// <summary>
        /// Writes the box contents to the writer.
        /// </summary>
	    protected virtual void WriteBoxContent(BoxWriter writer)
	    {
	        if (BoxBody != null)
	        {
	            BoxBody.Position = 0;
	            BoxBody.CopyTo(writer.BaseStream);
            }
	    }

	    public IEnumerable<T> GetChildren<T>() where T:Box
	    {
	        return Children.Where(child => child.GetType() == typeof(T)).Cast<T>();
	    }

	    public T GetSingleChild<T>() where T : Box
	    {
	        return GetChildren<T>().Single();
	    }
    }
}
