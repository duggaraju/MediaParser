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
    // A genric container box that only has child boxes and no box specific content.
    public abstract class ContainerBox : Box
    {
        public List<Box> Children { get; } = new List<Box>();

        public ContainerBox() : base()
        {
        }

        protected ContainerBox(BoxType boxType) : base(boxType)
        {
        }

        protected override sealed long ComputeBodySize()
        {
            return Children.Sum(child => child.ComputeSize());
        }


        protected override sealed long ParseBody(BoxReader reader, int depth)
        {
            long bytes = 0;
            while (bytes < Size - HeaderSize)
            {
                var box = BoxFactory.Parse(reader, --depth);
                Children.Add(box);
                bytes += box.Size;
            }

            return bytes;
        }

        protected override long WriteBoxBody(BoxWriter writer)
        {
            return Children.Sum(child => child.Write(writer));
        }

        public IEnumerable<T> GetChildren<T>() where T : Box
        {
            return Children.Where(child => child is T).Cast<T>();
        }

        public T GetSingleChild<T>() where T : Box
        {
            return GetChildren<T>().Single();
        }
    }

}
