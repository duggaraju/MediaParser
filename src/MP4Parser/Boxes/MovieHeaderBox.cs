
using System;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.MovieHeaderBox)]
    public partial class MovieHeaderBox : FullBox
    {
        public uint Timescale { get; set; }
        public ulong Duration { get; set; }

        public ulong CreationTime { get; set; }

        public ulong ModificationTime { get; set; }

        public uint Rate { get; set; }

        public ushort Volume { get; set; } = 0x0100;

        public uint[,] Matrix { get; } = new uint[3, 3];

        public uint NextTrackId { get; set; }

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
            Rate = reader.ReadUInt32();
            Volume = reader.ReadUInt16();
            reader.SkipBytes(10); // reserved
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Matrix[i,j] = reader.ReadUInt32();
                }
            }
            reader.SkipBytes(24);
            NextTrackId = reader.ReadUInt32();
        }

        protected override int ContentSize => (Version == 1 ? 28 : 16) + 6 + 36 + 34 + 4;

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
            writer.WriteUInt32(Rate);
            writer.WriteUInt16(Volume);
            writer.SkipBytes(10); // reserved
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    writer.WriteUInt32(Matrix[i,j]);
                }
            }
            writer.SkipBytes(24);
            writer.WriteUInt32(NextTrackId);
        }
    }
}
