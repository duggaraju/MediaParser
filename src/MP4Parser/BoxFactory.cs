﻿//Copyright 2015 Prakash Duggaraju
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
using System.Diagnostics.CodeAnalysis;
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
                    Trace.TraceInformation("Declared box {0}/{1:x} Type:{2}", attribute.Type, boxType.GetBoxName(), type);
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
            Type? declaringType = default;
            if (boxName.Length == 4)
            {
                Boxes.TryGetValue(boxName.GetBoxType(), out declaringType);
            }
            else if (Guid.TryParse(boxName, out var guid))
            {
                UuidBoxes.TryGetValue(guid, out declaringType);
            }
            
            if (declaringType == default)
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
        public static IEnumerable<Box> ParseBoxes(this Stream stream)
        {
            var reader = new BoxReader(stream);
            while (reader.TryParseBox(out var box))
            {
                yield return box;
            }
        }

        public static BoxHeader LookupBox(ReadOnlySpan<byte> buffer)
        {
            var header = new BoxHeader();
            header.Parse(buffer);
            return header;
        }

        public static Box Parse(ReadOnlySpan<byte> buffer)
        {
            var header = LookupBox(buffer);
            return Create(header);
        }

        public static T Parse<T>(ReadOnlySpan<byte> buffer) where T : Box
        {
            return (T) Parse(buffer);
        }

        public static bool TryParseBox(this BoxReader reader, [MaybeNullWhen(returnValue: false)]out Box box, int depth = int.MaxValue)
        {
            var offset = reader.BaseStream.CanSeek ? reader.BaseStream.Position : 0;
            if (Box.TryParseHeader(reader, out var header))
            {
                Trace.TraceInformation("Found Box:{0} Size:{1} at Offset:{2:x}", header.Type.GetBoxName(), header.Size, offset);
                box = Create(header);
                box.Parse(reader, depth);
                return true;
            }
            box = default;
            return false;
        }

        /// <summary>
        /// Parses a single box from the stream 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Box Parse(this BoxReader reader, int depth = int.MaxValue)
        {
            return reader.TryParseBox(out var box, depth) ? box : throw new InvalidDataException();
        }

        public static T? Parse<T>(BoxReader reader) where T: Box
        {
            return (T?) Parse(reader);
        }

        /// <summary>
        /// Create an instance of the box for the given box type.
        /// </summary>
        public static Box Create(BoxHeader header)
        {
            Type declaringType = GetDeclaringType(header.Type, header.ExtendedType);
            Box box;
            if (declaringType == typeof(Box))
            {
                box = new Box(header);
            }
            else
            {
                var constructor = declaringType.GetConstructor(new Type[0]);
                var args = Array.Empty<object>();
                box = (Box)constructor.Invoke(args);
            }
            box.Size = header.Size;
            box._forceLongSize = header.LongSize;
            return box;
        }

        /// <summary>
        /// Serialize a box to a stream.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="stream"></param>
        public static void Write(this Box box, Stream stream)
        {
            box.Write(new BoxWriter(stream));
        }
    }
}
