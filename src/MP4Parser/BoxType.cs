
namespace Media.ISO
{
    // https://www.rapidtables.com/code/text/ascii-table.html
    public enum BoxType : uint
    {
        FileBox = 0x66747970,
        MovieBox = 0x6d6f6f76,
        MovieHeaderBox = 0x6d766864,
        TrackBox = 0x7472616b,
        TrackHeaderBox = 0x746b6864,

        MediaBox = 0x6d646961,
        MediaHeaderBox = 0x6d646864,
        VideoMediaHeaderBox = 0x766d6864,
        MediaDataBox = 0x6d646174,
        MediaInformationBox = 0x6D696E66,
        DataInformationBox = 0x75756967,
        HandlerBox = 0x68646C72,

        UserDataBox = 0x75647461,
        MetaBox = 0x6d657461,
        MovieExtendsBox = 0x6d657874,
        TrackExtendsBox = 0x75756965,
        EditBox = 0x65647473,
        EditListBox = 0x65637374,

        SampleTableBox = 0x7374626c,
        SampleDescriptionBox = 0x73747364,
        TimeToSampleBox = 0x73747473,
        SampleToChunkBox = 0x73746368,
        SampleSizeBox = 0x7374737a,
        ChunkOffsetBox = 0x7374636f,

        //misc boxes.
        FreeBox = 0x66726565,
        SkipBox = 0x736b6970,
        UuidBox = 0x75756964,

        //CMAF boxes.
        EmsgBox = 0x656d7367,
        TrackFragmentDecodeTimeBox = 0x74666474,
        PrftBox = 0x50726674,
        SegmentIndexBox = 0x73696478,

        // Fragmented MP4 file boxes.
        MovieFragmentBox = 0x6d6f6f66,
        MovieFragmentHeaderBox = 0x6d666864,
        TrackFragmentBox = 0x74726166,
        TrackFragmentHeaderBox = 0x74666864,
        TrackFragmentRunBox = 0x7472756e,
        SampleDependencyTypeBox = 0x73647470,
        MovieFragmentRandomAccessBox = 0x6d667261,
        TrackFragmentRandomAccessBox = 0x74667261,
        MovieFragmentRandomOffsetBox = 0x6d66726f,
    }
}
