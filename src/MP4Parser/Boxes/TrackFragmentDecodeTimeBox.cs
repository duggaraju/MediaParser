using System;

namespace Media.ISO.Boxes
{
    [BoxType(BoxConstants.TfdtBox)]
    internal class TrackFragmentDecodeTimeBox : FullBox
    {
        public ulong BaseMediaDecodeTime { get; set; }   

        public TrackFragmentDecodeTimeBox() : base(BoxConstants.TfdtBox)
        {
        }

        protected override void ParseContent(BoxReader reader, long boxEnd)
        {
            BaseMediaDecodeTime = Version == 1 ? reader.ReadUInt64() : reader.ReadUInt32();
            base.ParseContent(reader, boxEnd);
        }
    }
}
