namespace Media.ISO.Boxes
{
    [FullBox(BoxType.MovieFragmentHeaderBox)]
    public partial class MovieFragmentHeaderBox
    {
        public uint SequenceNumber { get; set; }
    }
}
