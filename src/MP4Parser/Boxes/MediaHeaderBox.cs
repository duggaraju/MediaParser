namespace Media.ISO.Boxes
{
    [BoxType(BoxType.MediaHeaderBox)]
    public class MediaHeaderBox : Box
    {
        public MediaHeaderBox() : base(BoxType.MediaHeaderBox)
        {
        }
    }
}