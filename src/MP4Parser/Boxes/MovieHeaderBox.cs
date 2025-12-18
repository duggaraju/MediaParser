namespace Media.ISO.Boxes
{
    [FullBox(BoxType.MovieHeaderBox)]
    public partial class MovieHeaderBox
    {
        [VersionDependentSize]
        public ulong CreationTime { get; set; }

        [VersionDependentSize]
        public ulong ModificationTime { get; set; }

        public uint Timescale { get; set; }

        [VersionDependentSize]
        public ulong Duration { get; set; }

        public uint Rate { get; set; }

        public ushort Volume { get; set; } = 0x0100;

        [Reserved(10)]
        public byte Reserved1 { get; }

        public uint[] Matrix { get; } = new uint[9];

        [Reserved(24)]
        public byte Reserved2 { get; }

        public uint NextTrackId { get; set; }
    }
}
