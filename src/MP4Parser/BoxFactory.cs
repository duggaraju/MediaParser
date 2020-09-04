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
using System.Reflection;
using Media.ISO.Boxes;

namespace Media.ISO
{
    /// <summary>
    /// A factory class for creating and parsing boxes.
    /// </summary>
    public static class BoxFactory
    {
        private static readonly Dictionary<Guid, Type> UuidBoxes = new Dictionary<Guid, Type>();
        private static readonly Dictionary<uint, Type> Boxes = new Dictionary<uint, Type>();

        static BoxFactory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(BoxTypeAttribute)).Cast<BoxTypeAttribute>();
                foreach (var attribute in attributes)
                {
                    var boxType = attribute.Type.GetBoxType();
                    Trace.TraceInformation("Declared box {0}/{1:x} Type:{2}", attribute.Type, boxType, type);
                    if (attribute.Type != "uuid")
                    {
                        Boxes.Add(attribute.Type.GetBoxType(), type);
                    }
                    else
                    {
                        UuidBoxes.Add(attribute.ExtendedType, type);
                    }
                }
            }
        }

        /// <summary>
        /// Get the Type where the box is declared.
        /// </summary>
        public static Type GetDeclaringType(uint type, Guid? extendedType = null)
        {
            Type declaringType;
            if (type == BoxConstants.UuidBoxType)
            {
                if (extendedType == null)
                {
                    throw new ArgumentException("Extended type cannot be null for uuid box", "extendedType");
                }
                UuidBoxes.TryGetValue(extendedType.Value, out declaringType);
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

        /// <summary>
        /// Parses a single box from the stream 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Box Parse(BoxReader reader, int depth = int.MaxValue)
        {
            long size;
            Guid? extendedType;
            long boxOffset = reader.BaseStream.Position;
            Box.ParseHeader(reader, out uint type, out size, out extendedType);
            Trace.TraceInformation("Found Box:{0} Size:{1} at Offset:{2:x}", type.GetBoxName(), size, boxOffset);
            var box = Create(type, extendedType);
            box.Size = size;
            box.Parse(reader, boxOffset, depth);
            return box;
        }

        public static Box Create(string boxName, Guid? extendedType)
        {
            return Create(boxName.GetBoxType(), extendedType);
        }

        /// <summary>
        /// Create an instance of the box for the given box type.
        /// </summary>
        public static Box Create(uint type, Guid? extendedType)
        {
            Type declaringType = GetDeclaringType(type, extendedType);

            ConstructorInfo constructor;
            object[] args;
            if (declaringType == typeof(Box))
            {
                var argTypes = new[] { typeof(uint), typeof(Guid?) };
                constructor = declaringType.GetConstructor(argTypes);
                args = new object[] { type, extendedType };
            }
            else
            {
                constructor = declaringType.GetConstructor(new Type[0]);
                args = new object[0];
            }
            if (constructor == null)
            {
                throw new ParseException("Did not find matching constructor in box.");
            }

            return constructor.Invoke(args) as Box;
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
