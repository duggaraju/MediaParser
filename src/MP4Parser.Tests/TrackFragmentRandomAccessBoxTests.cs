using Media.ISO;
using Media.ISO.Boxes;
using Xunit;

namespace Media.ISO.MP4Parser.Tests;

public sealed class TrackFragmentRandomAccessBoxTests
{
    private static readonly byte[] VersionOneBuffer =
    {
        0x00, 0x00, 0x00, 0x3E, // size = 62 bytes
        0x74, 0x66, 0x72, 0x61, // "tfra"
        0x01, 0x00, 0x00, 0x00, // version = 1, flags = 0
        0x11, 0x22, 0x33, 0x44, // TrackId
        0x00, 0x00, 0x00, 0x00, // entry size flags
        0x00, 0x00, 0x00, 0x02, // entry count
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, // entry[0].Time
        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, // entry[0].MoofOffset
        0x09, // entry[0].TrackFragmentNumber
        0x0A, // entry[0].TrackRunNumber
        0x0B, // entry[0].SampleNumber
        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, // entry[1].Time
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, // entry[1].MoofOffset
        0x1A, // entry[1].TrackFragmentNumber
        0x1B, // entry[1].TrackRunNumber
        0x1C  // entry[1].SampleNumber
    };

    [Fact]
    public void ParseVersionOneEntries()
    {
        var box = ParseBox(VersionOneBuffer);

        Assert.Equal(BoxType.TrackFragmentRandomAccessBox, box.Type);
        Assert.Equal((byte)1, box.Version);
        Assert.Equal(0u, box.Flags);
        Assert.Equal(0x11223344u, box.TrackId);
        Assert.Equal(0u, box.EntrySize);
        Assert.Equal(1, TrackFragmentRandomAccessBox.Entry.TrackFragmentNumberSize(box.EntrySize));
        Assert.Equal(1, TrackFragmentRandomAccessBox.Entry.TrackRunNumberSize(box.EntrySize));
        Assert.Equal(1, TrackFragmentRandomAccessBox.Entry.SampleNumberSize(box.EntrySize));
        Assert.Equal(2, box.Entries.Count);

        var first = box.Entries[0];
        Assert.Equal(0x0102030405060708ul, first.Time);
        Assert.Equal(0x1112131415161718ul, first.MoofOffset);
        Assert.Equal(0x09u, first.TrackFragmentNumber);
        Assert.Equal(0x0Au, first.TrackRunNumber);
        Assert.Equal(0x0Bu, first.SampleNumber);

        var second = box.Entries[1];
        Assert.Equal(0x2122232425262728ul, second.Time);
        Assert.Equal(0x3132333435363738ul, second.MoofOffset);
        Assert.Equal(0x1Au, second.TrackFragmentNumber);
        Assert.Equal(0x1Bu, second.TrackRunNumber);
        Assert.Equal(0x1Cu, second.SampleNumber);
    }

    [Fact]
    public void SerializeRoundTripMatchesBuffer()
    {
        var box = ParseBox(VersionOneBuffer);
        box.ComputeSize();

        using var stream = new MemoryStream();
        box.Write(stream);

        Assert.Equal(VersionOneBuffer, stream.ToArray());
    }

    private static TrackFragmentRandomAccessBox ParseBox(byte[] buffer)
    {
        using var stream = new MemoryStream(buffer, writable: false);
        var reader = new BoxReader(stream);
        var box = BoxFactory.Parse<TrackFragmentRandomAccessBox>(reader);
        Assert.NotNull(box);
        return box!;
    }
}
