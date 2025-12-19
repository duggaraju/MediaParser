namespace Media.ISO.Boxes
{
    [FullBox(BoxType.SegmentIndexBox)]
    public partial class SegmentIndexBox
    {
        public record struct SegmentEntry()
        {
            const uint FirstBitMask = 0x7FFFFFFF;
            private uint _referenceSize;
            private uint _flags;
            private bool isSap;

            public uint ReferenceSize
            {
                get => _referenceSize & FirstBitMask;
                set => _referenceSize |= value & FirstBitMask;
            }

            public uint SegmentDuration { get; set; }

            public uint SapDeltaTime { get => _flags; set => _flags = value; }

            public bool IsSegmentIndex
            {
                get => (ReferenceSize & ~FirstBitMask) == 1;
                set => ReferenceSize |= (value ? FirstBitMask : 0);
            }

            public bool StartsWithSap
            {
                get => isSap;
                set => isSap = value;
            }

            public void Read(BoxReader reader)
            {
                _referenceSize = reader.ReadUInt32();
                SegmentDuration = reader.ReadUInt32();
                _flags = reader.ReadUInt32();
            }

            public void Write(BoxWriter writer)
            {
                writer.WriteUInt32(_referenceSize);
                writer.WriteUInt32(SegmentDuration);
                writer.WriteUInt32(_flags);
            }

            public int ComputeSize() => 3 * sizeof(uint);
        }

        public uint ReferenceId { get; set; }

        public uint TimeScale { get; set; }

        [VersionDependentSize]
        public ulong EarliestPresentationTime { get; set; }

        [VersionDependentSize]
        public ulong FirstOffset { get; set; }

        [CollectionLengthPrefix(typeof(uint))]
        public List<SegmentEntry> Entries { get; set; } = new List<SegmentEntry>();
    }
}
