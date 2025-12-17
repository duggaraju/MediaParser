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

using Media.ISO.Boxes;
using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Text;

namespace Media.ISO
{
    /// <summary>
    /// Extensions methods for boxes.
    /// </summary>
    public static class BoxExtensions
    {
        /// <summary>
        /// Convert a friendly string box name to a box type .
        /// </summary>
        public static BoxType GetBoxType(this string boxName)
        {
            if (boxName.Length != 4)
            {
                throw new ArgumentException("Box name must be only 4 characters", paramName: nameof(boxName));
            }
            return (BoxType)FromFourCC(boxName);
        }

        public static uint FromFourCC(this ReadOnlySpan<char> fourCC)
        {
            if (fourCC.Length != 4)
            {
                throw new ArgumentException("FourCC name must be only 4 characters", paramName: nameof(fourCC));
            }

            Span<byte> bytes = stackalloc byte[4];
            Encoding.ASCII.GetBytes(fourCC, bytes);
            var value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            return value;
        }

        /// <summary>
        /// Convert a box type to a friend string name.
        /// </summary>
        public static string GetBoxName(this BoxType type)
        {
            return GetFourCC((uint)type);
        }

        public static string GetFourCC(this uint type)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, type);
            return Encoding.ASCII.GetString(buffer);
        }

        public static BoxType GetBoxType<T>() where T : Box
        {
            return typeof(T).GetCustomAttribute<BoxAttribute>()?.Type ?? throw new ParseException("No BoxTypeAttribute found");
        }

        public static bool TryParseBox(BoxReader reader, out Box? box)
        {
            if (Box.TryParseHeader(reader, out var header))
            {
                box = BoxFactory.Create(header);
                return true;
            }
            box = default;
            return false;
        }
    }
}
