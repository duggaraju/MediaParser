namespace Media.ISO.Boxes
{
    [FullBox(BoxType.HandlerBox)]
    public partial class HandlerBox
    {
        [Reserved(4)]
        public byte PreDefined { get; }

        public uint Handler { get; set; }
        public string HandlerName => Handler.GetFourCC();

        [Reserved(12)]
        public byte Reserverd { get; }

        public string HandlerDescription { get; set; } = string.Empty;
    }
}
