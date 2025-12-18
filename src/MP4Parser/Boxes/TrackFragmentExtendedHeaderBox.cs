namespace Media.ISO.Boxes
{
    [FullBox(BoxConstants.TrackFragmentExtendedHeaderBox)]
    public partial class TrackFragmentExtendedHeaderBox
    {
        public const string BoxGuid = "";

        [VersionDependentSize]
        public ulong Time { get; set; }

        [VersionDependentSize]
        public ulong Duration { get; set; }
    }
}
