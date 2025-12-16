namespace Media.ISO.Boxes
{
    [BoxType(BoxType.TrackFragmentDecodeTimeBox)]
    public class TrackFragmentDecodeTimeBox : FullBox
    {
        public ulong BaseMediaDecodeTime { get; set; }

        protected override int ContentSize => Version == 1 ? sizeof(ulong) : sizeof(uint);

        protected override void ParseBoxContent(BoxReader reader)
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
                writer.WriteUInt32((uint)BaseMediaDecodeTime);
            }
        }
    }
}
