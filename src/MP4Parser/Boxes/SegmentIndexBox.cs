
using System.Collections.Generic;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.SegmentIndexBox)]
    public partial class SegmentIndexBox : FullBox
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

            internal void Read(BoxReader reader)
            {
                _referenceSize = reader.ReadUInt32();
                SegmentDuration = reader.ReadUInt32();
                _flags = reader.ReadUInt32();
            }

            internal void Write(BoxWriter writer)
            {
                writer.WriteUInt32(_referenceSize);
                writer.WriteUInt32(SegmentDuration);
                writer.WriteUInt32(_flags);
            }
        }

        public List<SegmentEntry> Entries { get; set; } = new List<SegmentEntry>();

        public uint ReferenceId { get; set; }

        public uint TimeScale { get; set; }

        public ulong EarliestPresentationTime { get; set; }

        public ulong FirstOffset { get; set; }

        protected override int ContentSize =>
            2 * sizeof(uint) +
            (Version == 1 ? 2 * sizeof(ulong) : 2 * sizeof(uint)) +
            sizeof(uint) +
            Entries.Count * 3 * sizeof(uint);

        protected override void ParseBoxContent(BoxReader reader)
        {
            ReferenceId = reader.ReadUInt32();
            TimeScale = reader.ReadUInt32();
            if (Version == 0)
            {
                EarliestPresentationTime = reader.ReadUInt32();
                FirstOffset = reader.ReadUInt32();
            }
            else
            {
                EarliestPresentationTime = reader.ReadUInt64();
                FirstOffset = reader.ReadUInt64();
            }
            reader.SkipBytes(2);
            var count = reader.ReadUInt16();
            for (var i = 0; i < count; i++)
            {
                SegmentEntry entry = new SegmentEntry();
                entry.Read(reader);
                Entries.Add(entry);
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32(ReferenceId);
            writer.WriteUInt32(TimeScale);
            if (Version == 0)
            {
                writer.WriteUInt32((uint)EarliestPresentationTime);
                writer.WriteUInt32((uint)FirstOffset);
            }
            else
            {
                writer.WriteUInt64(EarliestPresentationTime);
                writer.WriteUInt64(FirstOffset);
            }
            writer.SkipBytes(2);
            writer.WriteUInt16((ushort)Entries.Count);
            foreach (var entry in Entries)
            {
                entry.Write(writer);
            }
        }
    }
}
