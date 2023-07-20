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
using System.IO;
using System.Linq;
using System.Reflection;
using Media.ISO.Boxes;

namespace Media.ISO
{
    /// <summary>
    /// A factory class for creating and parsing boxes.
    /// </summary>
    public static class BoxFactory
    {
        private static readonly Dictionary<Guid, Type> UuidBoxes = new ();
        private static readonly Dictionary<BoxType, Type> Boxes = new ();

        static BoxFactory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Box))))
            {
                var attribute = type.GetCustomAttribute<BoxTypeAttribute>();
                if (attribute != null)
                {
                    var boxType = attribute.Type;
                    Trace.TraceInformation("Declared box {0}/{1:x} Type:{2}", attribute.Type, boxType, type);
                    if (attribute.ExtendedType == null)
                    {
                        Boxes.Add(attribute.Type, type);
                    }
                    else
                    {
                        UuidBoxes.Add(attribute.ExtendedType.Value, type);
                    }
                }
            }
        }

        /// <summary>
        /// Get the Type where the box is declared.
        /// </summary>
        public static Type GetDeclaringType(BoxType type, Guid? extendedType = null)
        {
            Type declaringType;
            if (type == BoxType.UuidBox)
            {
                if (extendedType == null || !UuidBoxes.TryGetValue(extendedType.Value, out declaringType))
                {
                    declaringType = typeof(Box);
                }
            }
            else
            {
                Boxes.TryGetValue(type, out declaringType);
            }

            if (declaringType == null)
            {
                declaringType = typeof (Box);
                Trace.TraceWarning("No declared type for box {0}/{1:x}. Using generic Box class", type.GetBoxName(), type);
            }

            return declaringType;
        }

        public static Type GetDeclaringType(string boxName)
        {
            Type declaringType = null;
            if (Guid.TryParse(boxName, out var guid))
            {
                UuidBoxes.TryGetValue(guid, out declaringType);
            }
            else
            {
                Boxes.TryGetValue(boxName.GetBoxType(), out declaringType);
            }
            
            if (declaringType == null)
            {
                declaringType = typeof(Box);
                Trace.TraceWarning("No declared type for box {0}. Using generic Box class", boxName);
            }
            return declaringType;
        }

        /// <summary>
        /// Parses a stream of bytes in to a collection of boxes.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static IEnumerable<Box> Parse(Stream stream)
        {
            var reader = new BoxReader(stream);
            while (stream.Position < stream.Length)
            {
                yield return Parse(reader);
            }
        }

        public static BoxHeader LookupBox(ReadOnlySpan<byte> buffer)
        {
            long size = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            buffer = buffer.Slice(4);
            var type = (BoxType)BinaryPrimitives.ReadUInt32BigEndian(buffer);
            Guid? extendedType = null;
            if (size == 0 && buffer.Length >= 8)
            {
                size = BinaryPrimitives.ReadInt64BigEndian(buffer);
                buffer = buffer.Slice(8);
            }
            if (type == BoxType.UuidBox && buffer.Length >= 16)
            {
                extendedType = new Guid(buffer);
            }
            return new BoxHeader(size, type, extendedType);
        }

        public static Box Parse(ReadOnlySpan<byte> buffer)
        {
            var (size, type, extendedType) = LookupBox(buffer);
            var box = Create(type, extendedType);
            box.Size = size;
            return box;
        }

        public static T Parse<T>(ReadOnlySpan<byte> buffer) where T : Box
        {
            return (T) Parse(buffer);
        }

        /// <summary>
        /// Parses a single box from the stream 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Box Parse(BoxReader reader, int depth = int.MaxValue)
        {
            long offset = reader.BaseStream.Position;
            var (size, type, extendedType) = Box.ParseBoxHeader(reader);
            Trace.TraceInformation("Found Box:{0} Size:{1} at Offset:{2:x}", type.GetBoxName(), size, offset);
            var box = Create(type, extendedType);
            box.Size = size;
            box.Parse(reader, offset, depth);
            return box;
        }

        public static T Parse<T>(BoxReader reader) where T: Box
        {
            return (T) Parse(reader);
        }

        public static Box Create(string boxName, Guid? extendedType = null)
        {
            return Create(boxName.GetBoxType(), extendedType);
        }

        /// <summary>
        /// Create an instance of the box for the given box type.
        /// </summary>
        public static Box Create(BoxType type, Guid? extendedType = null)
        {
            Type declaringType = GetDeclaringType(type, extendedType);
            var boxName = type.GetBoxName();
            try
            {
                ConstructorInfo constructor;
                object[] args;
                if (declaringType == typeof(Box))
                {
                    var argTypes = new[] { typeof(BoxType), typeof(Guid?) };
                    constructor = declaringType.GetConstructor(argTypes);
                    args = new object[] { type, extendedType };
                }
                else
                {
                    constructor = declaringType.GetConstructor(new Type[0]);
                    args = Array.Empty<object>();
                }
                return (Box)constructor.Invoke(args);
            }
            catch (Exception ex)
            {
                throw new ParseException("Did not find matching constructor in box.", ex);

            }

        }

        /// <summary>
        /// Serialize a box to a stream.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="stream"></param>
        public static void Write(this Box box, Stream stream)
        {
            BoxWriter writer = new BoxWriter(stream);
            box.Write(writer);
        }
    }
}
