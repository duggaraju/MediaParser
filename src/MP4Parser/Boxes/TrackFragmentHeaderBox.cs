namespace Media.ISO.Boxes
{
    [BoxType("tfhd")]
    public class TrackFragmentHeaderBox : FullBox
    {
        public const uint BaseDataOffsetPresent = 0x1;
        public const uint SampleDescriptionIndexPresent = 0x2;
        public const uint DefaultSampleDurationPresent = 0x8;
        public const uint DefaultSampleSizePresent = 0x10;
        public const uint DefaultSampleFlagsPresent = 0x20;

        private uint _defaultSampleFlags;
        private uint _defaultSampleSize;
        private uint _defaultSampleDuration;
        private ulong _baseDataOffset;
        private uint _sampleDescriptionIndex;

        protected override int BoxContentSize =>
            sizeof(uint) +
            ((Flags & BaseDataOffsetPresent) == 0 ? 0 : sizeof(ulong)) +
            ((Flags & SampleDescriptionIndexPresent) == 0 ? 0 : sizeof(uint)) +
            ((Flags & DefaultSampleDurationPresent) == 0 ? 0 : sizeof(uint)) +
            ((Flags & DefaultSampleSizePresent) == 0 ? 0 : sizeof(uint)) +
            ((Flags & DefaultSampleFlagsPresent) == 0 ? 0 : sizeof(uint));

        public uint TrackId { get; set; }

        public uint SampleDescriptionIndex
        {
            get => _sampleDescriptionIndex;
            set
            {
                Flags |= SampleDescriptionIndexPresent;
                _sampleDescriptionIndex = value;
            }
        }

        public ulong BaseDataOffset 
        {
            get => _baseDataOffset;
            set
            {
                Flags |= BaseDataOffsetPresent;
                _baseDataOffset = value;
            }
        }

        public uint DefaultSampleDuration 
        {
            get => _defaultSampleDuration;
            set
            {
                Flags |= DefaultSampleDurationPresent;
                _defaultSampleDuration = value; 
            }
        }

        public uint DefaultSampleSize 
        {
            get => _defaultSampleSize;
            set
            {
                Flags |= DefaultSampleSizePresent;
                _defaultSampleSize = value;
            }
        }

        public uint DefaultSampleFlags { 
            get => _defaultSampleFlags; 
            set 
            {
                Flags |= DefaultSampleFlagsPresent;
                _defaultSampleFlags = value;
            }
        }

        public TrackFragmentHeaderBox() : base("tfhd")
        {
        }

        protected override void ParseContent(BoxReader reader)
        {
            TrackId = reader.ReadUInt32();
            if ((Flags & BaseDataOffsetPresent) != 0)
            {
                BaseDataOffset = reader.ReadUInt64();
            }
            if ((Flags & SampleDescriptionIndexPresent) != 0)
            {
                SampleDescriptionIndex = reader.ReadUInt32();
            }
            if ((Flags & DefaultSampleDurationPresent) != 0)
            {
                DefaultSampleDuration = reader.ReadUInt32();
            }
            if ((Flags & DefaultSampleSizePresent) != 0)
            {
                DefaultSampleSize = reader.ReadUInt32();
            }
            if ((Flags & DefaultSampleFlagsPresent) != 0)
            {
                DefaultSampleFlags = reader.ReadUInt32();
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32(TrackId);
            if ((Flags & BaseDataOffsetPresent) != 0)
            {
                writer.WriteUInt64(BaseDataOffset);
            }
            if ((Flags & SampleDescriptionIndexPresent) != 0)
            {
                writer.WriteUInt32(SampleDescriptionIndex);
            }
            if ((Flags & DefaultSampleDurationPresent) != 0)
            {
                writer.WriteUInt32(DefaultSampleDuration);
            }
            if ((Flags & DefaultSampleSizePresent) != 0)
            {
                writer.WriteUInt32(DefaultSampleSize);
            }
            if ((Flags & DefaultSampleFlagsPresent) != 0)
            {
                writer.WriteUInt32(DefaultSampleFlags);
            }
        }
    }
}
