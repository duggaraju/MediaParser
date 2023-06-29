
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
        MediaDataBox = 0x6d646174,

        //misc boxes.
        FreeBox = 0x66726565,
        SkipBox = 0x736b6970,
        UuidBox = 0x75756964,

        //CMAF boxes.
        EmsgBox = 0x656d7367,
        TfdtBox = 0x74666474,
        PrftBox = 0x50726674,
        SidxBox = 0x73696478,

        // Fragmented MP4 file boxes.
        MovieFragmentBox = 0x6d6f6f66,
        MovieFragmentHeaderBox = 0x6d666864,
        TrackFragmentBox = 0x74726166,
        TrackFragmentHeaderBox = 0x74666864,
        MovieFragmentRandomAccessBox  = 0x6d667261,
        TrackFragmentRandomAccessBox = 0x74667261,
        MovieFragmentRandomOffsetBox = 0x6d66726f,
    }
}
