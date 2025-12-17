using System;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.MediaHeaderBox)]
    public partial class MediaHeaderBox : FullBox
    {
        public ulong CreationTime { get; set; }

        public ulong ModificationTime { get; set; }

        public uint Timescale { get; set; }

        public ulong Duration { get; set; }

        public ushort Language { get; set; }

        public ushort PreDefined { get; set; }

        public string LanguageString
        {
            get => DecodeLanguage(Language);
            set => Language = EncodeLanguage(value);
        }

        private static string DecodeLanguage(ushort value)
        {
            Span<char> buffer = stackalloc char[3];
            buffer[0] = (char)(((value >> 10) & 0x1F) + 0x60);
            buffer[1] = (char)(((value >> 5) & 0x1F) + 0x60);
            buffer[2] = (char)((value & 0x1F) + 0x60);
            return new string(buffer);
        }

        private static ushort EncodeLanguage(ReadOnlySpan<char> value)
        {
            if (value.Length != 3)
            {
                throw new ArgumentException("Language codes must be exactly 3 letters", nameof(value));
            }

            var encoded = 0;
            for (int i = 0; i < 3; i++)
            {
                var ch = char.ToLowerInvariant(value[i]);
                if (ch < 'a' || ch > 'z')
                {
                    throw new ArgumentException("Language codes must only contain letters a-z", nameof(value));
                }
                encoded = (encoded << 5) | (ch - 0x60);
            }

            return (ushort)encoded;
        }

        protected override void ParseBoxContent(BoxReader reader)
        {
            if (Version == 1)
            {
                CreationTime = reader.ReadUInt64();
                ModificationTime = reader.ReadUInt64();
                Timescale = reader.ReadUInt32();
                Duration = reader.ReadUInt64();
            }
            else
            {
                CreationTime = reader.ReadUInt32();
                ModificationTime = reader.ReadUInt32();
                Timescale = reader.ReadUInt32();
                Duration = reader.ReadUInt32();

            }
            Language = reader.ReadUInt16();
            PreDefined = reader.ReadUInt16();
        }

        protected override int ContentSize => Version == 1 ? 32: 20;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            if (Version == 1)
            {
                writer.WriteUInt64(CreationTime);
                writer.WriteUInt64(ModificationTime);
                writer.WriteUInt32(Timescale);
                writer.WriteUInt64(Duration);
            }
            else
            {
                writer.WriteUInt32((uint)CreationTime);
                writer.WriteUInt32((uint)ModificationTime);
                writer.WriteUInt32(Timescale);
                writer.WriteUInt32((uint)Duration);
            }
            writer.WriteUInt16(Language);
            writer.WriteUInt16(PreDefined);
        }
    }
}
