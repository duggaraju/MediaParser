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

using System.Collections.Generic;
using System.Linq;

namespace Media.ISO
{
    /// <summary>
    /// A class to define all the box names.
    /// </summary>
    public static class BoxConstants
    {
        public const string FileBox = "ftyp";
        public const string MovieBox = "moov";
        public const string MovieHeaderBox = "mvhd";
        public const string TrackBox = "trak";
        public const string TrackHeaderBox = "tkhd";

        public const string MediaDataBox = "mdat";

        //misc boxes.
        public const string FreeBox = "free";
        public const string SkipBox = "skip";


        // Fragmented MP4 file boxes.
        public const string MovieFragmentBox = "moof";
        public const string MovieFragmentHeaderBox = "mfhd";
        public const string TrackFragmentBox = "traf";
        public const string TrackFragmentHeaderBox = "tfhd";
        public const string MovieFragmentRandomAccessBox = "mfra";
        public const string TrackFragmentRandomAccessBox = "tfra";
        public const string MovieFragmentRandomOffsetBox = "mfro";

        private const string UuidBox = "uuid";
        public static readonly uint UuidBoxType = UuidBox.GetBoxType();

        public static IEnumerable<string> BoxNames => typeof(BoxConstants).GetFields()
                    .Where(field => field.IsLiteral && field.IsPublic).
                    Select(field => field.GetValue(null) as string);
    }
}
