
namespace Media.ISO.Boxes
{
    [BoxType(BoxType.EmsgBox)]
    public class EventMessageBox : FullBox
    {
        public override bool CanHaveChildren => false;

        public string Scheme { get; set; }

        public uint Id { get; set; }

        public string Value { get; set; }

        public uint TimeScale { get; set; }

        public ulong PresentationTime { get; set; }

        public uint Duration { get; set; }

        public EventMessageBox(): base(BoxType.EmsgBox)
        {
        }

        protected override void ParseContent(BoxReader reader, long boxEnd)
        {
            if (Version == 0)
            {
                Scheme = reader.ReadString();
                Value = reader.ReadString();
                TimeScale = reader.ReadUInt32();
                PresentationTime = reader.ReadUInt32();
                Duration = reader.ReadUInt32();
                Id = reader.ReadUInt32();
            }
            else if (Version == 1)
            {
                TimeScale = reader.ReadUInt32();
                PresentationTime = reader.ReadUInt64();
                Duration = reader.ReadUInt32();
                Id = reader.ReadUInt32();
                Scheme = reader.ReadString();
                Value = reader.ReadString();
            }
            base.ParseContent(reader, boxEnd);
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            base.WriteBoxContent(writer);
        }
    }
}
