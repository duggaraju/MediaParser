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
using System.Net;
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

            var bytes = Encoding.UTF8.GetBytes(boxName);
            return (uint) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));
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
            var bytes = (BitConverter.GetBytes(IPAddress.NetworkToHostOrder(type)));
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);            
        }
    }
}
