namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TrackFragmentRunBox)]
    public partial class TrackFragmentRunBox
    {
        public const uint DataOffsetPresent = 0x1;
        public const uint FirstSampleFlagsPresent = 0x4;
        public const uint SampleDurationPresent = 0x100;
        public const uint SampleSizePresent = 0x200;
        public const uint SampleFlagsPresent = 0x400;
        public const uint SampleCompositionOffsetPresent = 0x800;

        [VersionDependentSize]
        public struct SampleInfo
        {

            public uint? Size { get; set; }

            public uint? Duration { get; set; }

            public uint? Flags { get; set; }

            public int? CompositionOffset { get; set; }

            public int ComputeSize(VersionAndFlags versionAndFlags)
            {
                var flags = versionAndFlags.Flags;
                return
                    ((flags & SampleDurationPresent) != 0 ? sizeof(uint) : 0) +
                    ((flags & SampleSizePresent) != 0 ? sizeof(uint) : 0) +
                    ((flags & SampleFlagsPresent) != 0 ? sizeof(uint) : 0) +
                    ((flags & SampleCompositionOffsetPresent) != 0 ? sizeof(int) : 0);
            }

            public void Write(BoxWriter writer, VersionAndFlags versionAndFlags)
            {
                var flags = versionAndFlags.Flags;
                if ((flags & SampleDurationPresent) != 0)
                {
                    writer.WriteUInt32(Duration ?? 0);
                }
                if ((flags & SampleSizePresent) != 0)
                {
                    writer.WriteUInt32(Size ?? 0);
                }
                if ((flags & SampleFlagsPresent) != 0)
                {
                    writer.WriteUInt32(Flags ?? 0);
                }
                if ((flags & SampleCompositionOffsetPresent) != 0)
                {
                    writer.WriteInt32(CompositionOffset ?? 0);
                }
            }

            public void Read(BoxReader reader, VersionAndFlags versionAndFlags)
            {
                var flags = versionAndFlags.Flags;
                if ((flags & SampleDurationPresent) != 0)
                {
                    Duration = reader.ReadUInt32();
                }
                if ((flags & SampleSizePresent) != 0)
                {
                    Size = reader.ReadUInt32();
                }
                if ((flags & SampleFlagsPresent) != 0)
                {
                    Flags = reader.ReadUInt32();
                }
                if ((flags & SampleCompositionOffsetPresent) != 0)
                {
                    CompositionOffset = reader.ReadInt32();
                }
            }
        }

        [CollectionLengthPrefix(typeof(uint))]
        public IList<SampleInfo> Samples { get; set; } = Array.Empty<SampleInfo>();

        public int SampleCount => Samples?.Count ?? 0;

        [FlagOptional(DataOffsetPresent)]
        public int DataOffset { get; set; }

        [FlagOptional(FirstSampleFlagsPresent)]
        public uint FirstSampleFlags { get; set; }
    }
}
