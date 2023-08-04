namespace Media.ISO.Boxes
{
    [BoxType(BoxType.MovieFragmentHeaderBox)]
    public class MovieFragmentHeaderBox : FullBox
    {
        public uint SequenceNumber { get; set; }

        public MovieFragmentHeaderBox() : base(BoxType.MovieFragmentHeaderBox)
        { }

        protected override int BoxContentSize => sizeof(uint);

        protected override void ParseBoxContent(BoxReader reader)
        {
            SequenceNumber = reader.ReadUInt32();
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32(SequenceNumber);
        }
    }
}
