
using System;

namespace Media.ISO.Boxes
{
    [FullBox(BoxType.TimeToSampleBox)]
    public partial class TimeToSampleBox : FullBox
    {
        public TimeToSampleBox() : base()
        {
        }

        public uint[] SampleCounts { get; set; } = Array.Empty<uint>();

        public uint[] SampleDeltas { get; set; } = Array.Empty<uint>();

        protected override void ParseBoxContent(BoxReader reader)
        {
            var count = reader.ReadUInt32();
            SampleCounts = new uint[count];
            SampleDeltas = new uint[count];
            for (var i = 0; i < count; i++)
            {
                var sampleCount = reader.ReadUInt32();
                var sampleDelta = reader.ReadUInt32();
                SampleCounts[i] = sampleCount;
                SampleDeltas[i] = sampleDelta;
            }
        }

        protected override int ContentSize => 4 + SampleCounts.Length * 8;

        protected override void WriteBoxContent(BoxWriter writer)
        {
            writer.WriteUInt32((uint)SampleCounts.Length);
            for (var i = 0; i < SampleCounts.Length; i++)
            {
                writer.WriteUInt32(SampleCounts[i]);
                writer.WriteUInt32(SampleDeltas[i]);
            }
        }
    }
}
