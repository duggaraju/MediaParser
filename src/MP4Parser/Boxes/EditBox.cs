namespace Media.ISO.Boxes
{
    [Container(BoxType.EditBox)]
    public partial class EditBox
    {
    }

    [FullBox(BoxType.EditListBox)]
    public partial class EditListBox
    {
        public byte[] BoxContent { get; set; } = Array.Empty<byte>();
    }
}
