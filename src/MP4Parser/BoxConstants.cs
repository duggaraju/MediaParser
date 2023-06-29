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
using System.Linq;

namespace Media.ISO
{
    /// <summary>
    /// A class to define all the box names.
    /// </summary>
    public static class BoxConstants
    {
        public const string TrackFragmentExtendedHeaderBox =
            "aff757b2-141d-80e2-e644-d542059b1d6d";

        public static IEnumerable<Guid> UuidBoxNames => typeof(BoxConstants).GetFields()
            .Where(field => field.IsLiteral && field.IsPublic)
            .Select(field => field.GetValue(null))
            .Cast<string>()
            .Select(Guid.Parse);
    }
}
