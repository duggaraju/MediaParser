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

using System.Diagnostics;

namespace Media.ISO.Boxes
{
    // A genric container box that only has child boxes and no box specific content.
    public abstract class ContainerBox : Box
    {
        protected ContainerBox(string boxName)
            : base(boxName)
        {
        }

        public override sealed bool CanHaveChildren => true;

        /// <summary>
        /// Container boxes do not any box specific content. so helper to accidentally overriding the same.
        /// </summary>
        protected override sealed void ParseBoxContent(BoxReader reader, long boxEnd)
        {
            Debug.Assert(false);
        }

        protected override sealed void WriteBoxContent(BoxWriter writer)
        {
            Debug.Assert(false);
        }
    }
}
