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

namespace Media.ISO.Boxes
{
    /// <summary>
    /// A class to implement the 'free' box to pad space.
    /// </summary>
    [BoxType(BoxConstants.FreeBox)]
    [BoxType(BoxConstants.SkipBox)]
    public class FreeBox : Box
    {
        public FreeBox() : this(false)
        {
        }

        public FreeBox(bool useSkip) : base(useSkip ? BoxConstants.SkipBox : BoxConstants.FreeBox)
        {
        }

        public override bool CanHaveChildren => false;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            long remaining = Size - HeaderSize;
            writer.SkipBytes(remaining);
        }
    }
}
