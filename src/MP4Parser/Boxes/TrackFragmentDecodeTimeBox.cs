namespace Media.ISO.Boxes
{
    [BoxType(BoxType.TrackFragmentDecodeTimeBox)]
    public class TrackFragmentDecodeTimeBox : FullBox
    {
        public ulong BaseMediaDecodeTime { get; set; }   

        public TrackFragmentDecodeTimeBox() : base(BoxType.TrackFragmentDecodeTimeBox)
        {
        }

        protected override int BoxContentSize => Version == 1 ? sizeof(ulong) : sizeof(uint);

        protected override void ParseContent(BoxReader reader)
        {
            BaseMediaDecodeTime = Version == 1 ? reader.ReadUInt64() : reader.ReadUInt32();
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            base.WriteBoxContent(writer);
            if (Version == 1)
            {
                writer.WriteUInt64(BaseMediaDecodeTime);
            }
            else
            {
                writer.WriteUInt32((uint) BaseMediaDecodeTime);
            }
        }
    }
}
