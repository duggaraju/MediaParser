
namespace Media.ISO.Boxes
{
    [BoxType(BoxType.EmsgBox)]
    public class EventMessageBox : FullBox
    {
        public override bool CanHaveChildren => false;

        public string Scheme { get; set; } = string.Empty;

        public uint Id { get; set; }

        public string Value { get; set; } = string.Empty;

        public uint TimeScale { get; set; }

        public ulong PresentationTime { get; set; }

        public uint Duration { get; set; }

        public EventMessageBox(): base(BoxType.EmsgBox)
        {
        }

        protected override int BoxContentSize =>
            Scheme.Length + 1 +
            Value.Length + 1 +
            sizeof(uint) * 3 +
            (Version == 1 ? sizeof(ulong) : sizeof(uint));

        protected override void ParseBoxContent(BoxReader reader)
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
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            if (Version == 0)
            {
                writer.WriteString(Scheme);
                writer.WriteString(Value);
                writer.WriteUInt32(TimeScale);
                writer.WriteUInt32((uint)PresentationTime);
                writer.WriteUInt32(Duration);
                writer.WriteUInt32(Id);
            }
            else if (Version == 1)
            {
                writer.WriteUInt32(TimeScale);
                writer.WriteUInt64(PresentationTime);
                writer.WriteUInt32(Duration);
                writer.WriteUInt32(Id);
                writer.WriteString(Scheme);
                writer.WriteString(Value);
            }
        }
    }
}
