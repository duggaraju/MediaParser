namespace Media.ISO.Boxes
{
    [FullBox(BoxType.SampleDependencyTypeBox)]
    public partial class SampleDependencyTypeBox
    {
        public byte[] SampleDependencies { get; set; } = Array.Empty<byte>();

    }
}
