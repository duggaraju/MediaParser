namespace Media.ISO.Boxes
{
    [FullBox("rsvb")]
    public partial class ReservedFieldsBox
    {
        public const int ReservedFieldLength = 4;

        public uint LeadingValue { get; set; }

        [Reserved(ReservedFieldLength)]
        public int ReservedBytes => ReservedFieldLength;

        public uint TrailingValue { get; set; }
    }
}
