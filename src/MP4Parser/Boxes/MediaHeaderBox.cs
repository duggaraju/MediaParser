namespace Media.ISO.Boxes
{
    [BoxType(BoxType.MediaHeaderBox)]
    public class MediaHeaderBox : FullBox
    {
        public ulong CreationTime { get; set; }

        public ulong ModificationTime { get; set; }
        public uint Timescale { get; set; }

        public ulong Duration { get; set; }

        public uint Language { get; set; }

        public string LanguageString
        {
            get => Language.GetFourCC();
            set => Language = BoxExtensions.FromFourCC(value);
        }
    }
}
