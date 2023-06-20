
using System;

namespace Media.ISO.Boxes
{
    [BoxType(BoxConstants.TrackFragmentExtendedHeaderBox)]
    public class TrackFragmentExtendedHeaderBox : FullBox
    {
        public const string BoxGuid = "";

        public ulong Time { get; set; }

        public ulong Duration { get; set; }

        public TrackFragmentExtendedHeaderBox() 
            : base(BoxConstants.UuidBoxType, new Guid(BoxConstants.TrackFragmentExtendedHeaderBox))
        {
        }

        protected override void ParseHeader(BoxReader reader)
        {
            base.ParseHeader(reader);
            if (Version == 1)
            {
                Time = reader.ReadUInt64();
                Duration = reader.ReadUInt64();
            }
            else
            {
                Time = reader.ReadUInt32();
                Duration = reader.ReadUInt32();
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            base.WriteBoxContent(writer);
            if (Version == 1)
            {
                writer.WriteUInt64(Time);
                writer.WriteUInt64(Duration);
            }
            else
            {
                writer.WriteUInt32((uint)Time);
                writer.WriteUInt32((uint)Duration);
            }
        }
    }
}
