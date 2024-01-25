namespace Media.ISO.Boxes
{
    [BoxType(BoxType.MediaBox)]
    public class MediaBox : Box
    {
        public MediaBox() : base(BoxType.MediaBox)
        {
        }

        public override bool CanHaveChildren => true;

        public MediaHeaderBox Header => GetSingleChild<MediaHeaderBox>();
    }
}