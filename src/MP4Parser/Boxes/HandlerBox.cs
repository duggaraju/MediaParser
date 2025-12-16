
namespace Media.ISO.Boxes
{
    [BoxType(BoxType.HandlerBox)]
    public class HandlerBox : FullBox
    {
        public uint Handler { get; set; }

        public string HandlerName => Handler.GetFourCC();
    }
}
