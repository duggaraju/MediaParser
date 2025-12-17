
using System;
using System.Text;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.HandlerBox)]
    public partial class HandlerBox : FullBox
    {
        public uint PreDefined { get; set; }

        public uint Handler { get; set; }

        public string HandlerDescription { get; set; } = string.Empty;

        public string HandlerName => Handler.GetFourCC();

        protected override void ParseBoxContent(BoxReader reader)
        {
            PreDefined = reader.ReadUInt32();
            Handler = reader.ReadUInt32();
            reader.SkipBytes(12);
            Span<byte> buffer = stackalloc byte[(int)Size - 32];
            reader.BaseStream.ReadExactly(buffer);
            HandlerDescription = Encoding.UTF8.GetString(buffer);
        }

        protected override int ContentSize => 20 + HandlerDescription.Length;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32(PreDefined);
            writer.WriteUInt32(Handler);
            writer.SkipBytes(12);
            Span<byte> buffer = stackalloc byte[HandlerDescription.Length];
            Encoding.ASCII.TryGetBytes(HandlerDescription, buffer, out var written);
            writer.Write(buffer);
        }
    }
}
