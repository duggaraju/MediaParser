namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TrackFragmentDecodeTimeBox)]
    public partial class TrackFragmentDecodeTimeBox
    {
        [VersionDependentSize]
        public ulong BaseMediaDecodeTime { get; set; }
    }
}
