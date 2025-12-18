namespace Media.ISO.Boxes
{
    /// <summary>
    ///  An unknown box that contains raw data.
    /// </summary>
    public class RawBox : Box
    {
        public Memory<byte> Body = Array.Empty<byte>();

        public RawBox(BoxHeader header) : base(header)
        {
        }
        protected RawBox() : base()
        {
        }

        protected override long ComputeBodySize() => Body.Length;

        protected override long ParseBody(BoxReader reader, int _)
        {
            var boxBody = Size - HeaderSize;
            if (boxBody > 0)
            {
                Body = new byte[boxBody];
                reader.BaseStream.ReadExactly(Body.Span);
            }
            return boxBody;
        }

        protected override long WriteBoxBody(BoxWriter writer)
        {
            writer.Write(Body.Span);
            return Body.Length;
        }

    }
}
