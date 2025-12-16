using System;
using System.Collections.Generic;

namespace Media.ISO.Boxes
{
    [BoxType(BoxType.TrackFragmentRunBox)]
    public class TrackFragmentRunBox : FullBox
    {
        public const uint DataOffsetPresent = 0x1;
        public const uint FirstSampleFlagsPresent = 0x4;
        public const uint SampleDurationPresent = 0x100;
        public const uint SampleSizePresent = 0x200;
        public const uint SampleFlagsPresent = 0x400;
        public const uint SampleCompositionOffsetPresent = 0x800;

        public struct SampleInfo
        {
            public uint? Size { get; set; }

            public uint? Duration { get; set; }

            public uint? Flags { get; set; }

            public int? CompositionOffset { get; set; }
        }

        public IList<SampleInfo> Samples { get; set; } = Array.Empty<SampleInfo>();

        public int SampleCount => Samples?.Count ?? 0;

        public int DataOffset { get; set; }

        public uint FirstSampleFlags { get; set; }

        protected override int ContentSize =>
            sizeof(uint) +
            ((Flags & DataOffsetPresent) != 0 ? sizeof(uint) : 0) +
            ((Flags & FirstSampleFlagsPresent) != 0 ? sizeof(uint) : 0) +
            SampleCount * GetSampleSize(Flags);


        private static int GetSampleSize(uint flags)
        {
            return
                ((flags & SampleDurationPresent) != 0 ? sizeof(uint) : 0) +
                ((flags & SampleSizePresent) != 0 ? sizeof(uint) : 0) +
                ((flags & SampleFlagsPresent) != 0 ? sizeof(uint) : 0) +
                ((flags & SampleCompositionOffsetPresent) != 0 ? sizeof(int) : 0);
        }

        protected override void ParseBoxContent(BoxReader reader)
        {
            var count = reader.ReadUInt32();
            Samples = new List<SampleInfo>((int)count);
            if ((Flags & DataOffsetPresent) != 0)
            {
                DataOffset = reader.ReadInt32();
            }
            if ((Flags & FirstSampleFlagsPresent) != 0)
            {
                FirstSampleFlags = reader.ReadUInt32();
            }

            for (var i = 0u; i < count; ++i)
            {
                var info = new SampleInfo();
                if ((Flags & SampleDurationPresent) != 0)
                {
                    info.Duration = reader.ReadUInt32();
                }
                if ((Flags & SampleSizePresent) != 0)
                {
                    info.Size = reader.ReadUInt32();
                }
                if ((Flags & SampleFlagsPresent) != 0)
                {
                    info.Flags = reader.ReadUInt32();
                }
                if ((Flags & SampleCompositionOffsetPresent) != 0)
                {
                    info.CompositionOffset = reader.ReadInt32();
                }
                Samples.Add(info);
            }
        }

        protected override void WriteBoxContent(BoxWriter writer)
        {
            var count = SampleCount;
            writer.WriteUInt32((uint)count);
            if ((Flags & DataOffsetPresent) != 0)
            {
                writer.WriteInt32(DataOffset);
            }
            if ((Flags & FirstSampleFlagsPresent) != 0)
            {
                writer.WriteUInt32(FirstSampleFlags);
            }

            foreach (var sample in Samples)
            {
                if ((Flags & SampleDurationPresent) != 0)
                {
                    writer.WriteUInt32(sample.Duration ?? 0);
                }
                if ((Flags & SampleSizePresent) != 0)
                {
                    writer.WriteUInt32(sample.Size ?? 0);
                }
                if ((Flags & SampleFlagsPresent) != 0)
                {
                    writer.WriteUInt32(sample.Flags ?? 0);
                }
                if ((Flags & SampleCompositionOffsetPresent) != 0)
                {
                    writer.WriteInt32(sample.CompositionOffset ?? 0);
                }
            }
        }
    }
}
