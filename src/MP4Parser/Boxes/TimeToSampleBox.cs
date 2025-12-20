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

            public int ComputeSize() => sizeof(uint) + sizeof(uint);
        }

        [CollectionLengthPrefix(typeof(uint))]
        public List<Entry> Entries { get; set; } = new();
    }
}
