namespace Media.ISO.Boxes
{
    [FullBox("meta")]
    public partial class MetaBox
    {
        public byte[] BoxContent { get; set; } = Array.Empty<byte>();
    }
}
