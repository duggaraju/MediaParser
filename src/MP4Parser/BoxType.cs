
namespace Media.ISO
{
    public enum BoxType : uint
    {
        FileBox = ('f' << 24) | ('t' << 16) | ('y' << 8) | 'p',
        MovieBox = ('m' << 24) | ('o' << 16) | ('o' << 8) | 'v',
        MovieHeaderBox = ('m' << 24) | ('v' << 16) | ('h' << 8) | 'd',
        TrackBox = ('t' << 24) | ('r' << 16) | ('a' << 8) | 'k',
        TrackHeaderBox = ('t' << 24) | ('k' << 16) | ('h' << 8) | 'd',

        MediaBox = ('m' << 24) | ('d' << 16) | ('i' << 8) | 'a',
        MediaHeaderBox = ('m' << 24) | ('d' << 16) | ('h' << 8) | 'd',
        VideoMediaHeaderBox = ('v' << 24) | ('m' << 16) | ('h' << 8) | 'd',
        MediaDataBox = ('m' << 24) | ('d' << 16) | ('a' << 8) | 't',
        MediaInformationBox = ('m' << 24) | ('i' << 16) | ('n' << 8) | 'f',
        DataInformationBox = ('u' << 24) | ('u' << 16) | ('i' << 8) | 'g',
        HandlerBox = ('h' << 24) | ('d' << 16) | ('l' << 8) | 'r',

        UserDataBox = ('u' << 24) | ('d' << 16) | ('t' << 8) | 'a',
        MetaBox = ('m' << 24) | ('e' << 16) | ('t' << 8) | 'a',
        MovieExtendsBox = ('m' << 24) | ('e' << 16) | ('x' << 8) | 't',
        TrackExtendsBox = ('u' << 24) | ('u' << 16) | ('i' << 8) | 'e',
        EditBox = ('e' << 24) | ('d' << 16) | ('t' << 8) | 's',
        EditListBox = ('e' << 24) | ('c' << 16) | ('s' << 8) | 't',

        SampleTableBox = ('s' << 24) | ('t' << 16) | ('b' << 8) | 'l',
        SampleDescriptionBox = ('s' << 24) | ('t' << 16) | ('s' << 8) | 'd',
        TimeToSampleBox = ('s' << 24) | ('t' << 16) | ('t' << 8) | 's',
        SampleToChunkBox = ('s' << 24) | ('t' << 16) | ('c' << 8) | 'h',
        SampleSizeBox = ('s' << 24) | ('t' << 16) | ('s' << 8) | 'z',
        ChunkOffsetBox = ('s' << 24) | ('t' << 16) | ('c' << 8) | 'o',

        //misc boxes.
        FreeBox = ('f' << 24) | ('r' << 16) | ('e' << 8) | 'e',
        SkipBox = ('s' << 24) | ('k' << 16) | ('i' << 8) | 'p',
        UuidBox = ('u' << 24) | ('u' << 16) | ('i' << 8) | 'd',

        //CMAF boxes.
        EmsgBox = ('e' << 24) | ('m' << 16) | ('s' << 8) | 'g',
        TrackFragmentDecodeTimeBox = ('t' << 24) | ('f' << 16) | ('d' << 8) | 't',
        PrftBox = ('p' << 24) | ('r' << 16) | ('f' << 8) | 't',
        SegmentIndexBox = ('s' << 24) | ('i' << 16) | ('d' << 8) | 'x',

        // Fragmented MP4 file boxes.
        MovieFragmentBox = ('m' << 24) | ('o' << 16) | ('o' << 8) | 'f',
        MovieFragmentHeaderBox = ('m' << 24) | ('f' << 16) | ('h' << 8) | 'd',
        TrackFragmentBox = ('t' << 24) | ('r' << 16) | ('a' << 8) | 'f',
        TrackFragmentHeaderBox = ('t' << 24) | ('f' << 16) | ('h' << 8) | 'd',
        TrackFragmentRunBox = ('t' << 24) | ('r' << 16) | ('u' << 8) | 'n',
        SampleDependencyTypeBox = ('s' << 24) | ('d' << 16) | ('t' << 8) | 'p',
        MovieFragmentRandomAccessBox = ('m' << 24) | ('f' << 16) | ('r' << 8) | 'a',
        TrackFragmentRandomAccessBox = ('t' << 24) | ('f' << 16) | ('r' << 8) | 'a',
        MovieFragmentRandomOffsetBox = ('m' << 24) | ('f' << 16) | ('r' << 8) | 'o',

        // Test/support boxes.
        ReservedFieldsBox = ('r' << 24) | ('s' << 16) | ('v' << 8) | 'b',
    }
}
