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
        public uint Type { get; private set; }

        /// <summary>
        /// The size of the box.
        /// </summary>
        public long Size { get; set; }

        public List<Box> Children { get; private set; }

        public Guid? ExtendedType { get; private set; }

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
            return string.Format("Box:{0} Size:{1}", Name, Size);
        }

		internal void Parse(BoxReader reader, long boxOffset, int depth = int.MaxValue)
        {
            long boxEnd = Size == 0 ? reader.BaseStream.Length : boxOffset + Size;

            if (CanHaveChildren && depth > 0)
		    {
		        ParseChildren(reader, boxEnd);
		    }
		    else
		    {
		        ParseBoxContent(reader, boxEnd);
		    }

		    if (reader.BaseStream.Position != boxEnd)
		    {
		        throw new ParseException(
                    string.Format("Unparsed box content at the end of box: {0} Offset:{1}", this, reader.BaseStream.Position));
		    }
        }

	    protected virtual void ParseBoxContent(BoxReader reader, long boxEnd)
	    {
	        long boxBody = boxEnd - reader.BaseStream.Position;
	        if (reader.BaseStream.CanSeek)
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

        public static long ParseHeader(BoxReader reader, out uint type, out long size, out Guid extendedType)
        {
            long offset = reader.BaseStream.Position;
            extendedType = Guid.Empty;
            size = reader.ReadUInt32();
            type = reader.ReadUInt32();
            if (size == 0)
            {
                //box is till the end of the stream.
                size = long.MinValue;
            }
            else if( size == 1)
            {
                size = reader.ReadInt64();
            }

            if(type == BoxConstants.UuidBoxType)
            {
                extendedType = reader.ReadGuid();
            }
            return reader.BaseStream.Position - offset;
        }

        /// <summary>
        /// Get the name of the box in a printer friendly string.
        /// </summary>
        public string Name
        {
            get
            {
                return Type.GetBoxName();
            }
        }


        /// <summary>
        /// Compute the size of the box itself (without any children).
        /// </summary>
        /// <returns></returns>
	    protected virtual long GetBoxSize()
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

	    public virtual bool CanHaveChildren
	    {
            get {  return false; }
	    }

        /// <summary>
        /// Write the content of the box to a writer.
        /// </summary>
        public void Write(BoxWriter writer)
        {
            WriteBoxHeader(writer);
            if (CanHaveChildren)
            {
                WriteChildren(writer);
            }
            else
            {
                WriteBoxContent(writer);
            }
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

	    protected virtual void WriteBoxContent(BoxWriter writer)
	    {
	        if (BoxBody != null)
	        {
	            BoxBody.CopyTo(writer.BaseStream);
	        }
	    }

	    private void WriteChildren(BoxWriter writer)
	    {
	        foreach(var child in Children)
                child.Write(writer);
	    }

        public long UpdateSize()
        {
            return GetBoxSize() + Children.Sum(child => child.UpdateSize());
        }

    }
}
