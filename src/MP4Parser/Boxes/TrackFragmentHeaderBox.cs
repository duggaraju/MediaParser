namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TrackFragmentHeaderBox)]
    public partial class TrackFragmentHeaderBox
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

        public uint TrackId { get; set; }

        [FlagDependent(SampleDescriptionIndexPresent)]
        public uint SampleDescriptionIndex
        {
            get => _sampleDescriptionIndex;
            set
            {
                Flags |= SampleDescriptionIndexPresent;
                _sampleDescriptionIndex = value;
            }
        }

        [FlagDependent(BaseDataOffsetPresent)]
        public ulong BaseDataOffset
        {
            get => _baseDataOffset;
            set
            {
                Flags |= BaseDataOffsetPresent;
                _baseDataOffset = value;
            }
        }

        [FlagDependent(DefaultSampleDurationPresent)]
        public uint DefaultSampleDuration
        {
            get => _defaultSampleDuration;
            set
            {
                Flags |= DefaultSampleDurationPresent;
                _defaultSampleDuration = value;
            }
        }

        [FlagDependent(DefaultSampleSizePresent)]
        public uint DefaultSampleSize
        {
            get => _defaultSampleSize;
            set
            {
                Flags |= DefaultSampleSizePresent;
                _defaultSampleSize = value;
            }
        }

        [FlagDependent(DefaultSampleFlagsPresent)]
        public uint DefaultSampleFlags
        {
            get => _defaultSampleFlags;
            set
            {
                Flags |= DefaultSampleFlagsPresent;
                _defaultSampleFlags = value;
            }
        }
    }
}
