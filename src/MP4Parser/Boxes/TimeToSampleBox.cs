using System.Formats.Asn1;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TimeToSampleBox)]
    public partial class TimeToSampleBox
    {
        public record struct Entry(uint SampleCount, uint SampleDelta)
        {
            public void Read(BoxReader reader)
            {
                SampleCount = reader.ReadUInt32();
                SampleDelta = reader.ReadUInt32();
            }

            public void Write(BoxWriter writer)
            {
                writer.WriteUInt32(SampleCount);
                writer.WriteUInt32(SampleDelta);
            }
        }

        public List<Entry> Entries { get; set; } = new();

        protected override void ParseBoxContent(BoxReader reader)
        {
            var count = reader.ReadUInt32();
            for (var i = 0; i < count; i++)
            {
                var entry = new Entry();
                entry.Read(reader);
                Entries.Add(entry);
            }
        }

        protected override int ContentSize => 4 + Entries.Count * 8;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32((uint) Entries.Count);
            foreach(var entry in Entries)
            {
                entry.Write(writer);
            }
        }
    }
}
