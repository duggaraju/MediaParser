using System;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.SampleDependencyTypeBox)]
    public partial class SampleDependencyTypeBox : FullBox
    {
        public byte[] SampleDependencies { get; set; } = Array.Empty<byte>();

    }
}
