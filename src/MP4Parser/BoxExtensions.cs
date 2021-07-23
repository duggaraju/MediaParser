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
        public static uint GetBoxType(this string boxName)
        {
            if(string.IsNullOrEmpty(boxName))
            {
                throw new ArgumentNullException("boxName");
            }
            if(boxName.Length != 4)
            {
                throw new ArgumentException("Box name must be only 4 characters", paramName: "boxName");
            }

            Span<byte> bytes = stackalloc byte[4];
            Encoding.ASCII.GetBytes(boxName, bytes);
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        /// <summary>
        /// Convert a box type to a friend string name.
        /// </summary>
        public static string GetBoxName(this uint type)
        {
            return GetBoxName((int) type);
        }

        public static string GetBoxName(this int type)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, type);
            return Encoding.ASCII.GetString(buffer);            
        }

        public static uint GetBoxType<T>() where T : Box
        {
            return typeof(T).GetCustomAttribute<BoxTypeAttribute>().Type.GetBoxType();
        }
    }
}
