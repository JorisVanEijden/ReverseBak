namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class WorldItemExtractorTests
{
    [Fact]
    public void Extract_ReadsSingleItem()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)42);    // TypeId
        writer.Write((ushort)100);   // Rotation.X
        writer.Write((ushort)200);   // Rotation.Y
        writer.Write((ushort)300);   // Rotation.Z
        // Inside T010203's own square (tile 2,3 spans x 128000..192000, y 192000..256000) —
        // a record outside it is discarded as unplaceable, see IsPlaceable.
        writer.Write((uint)130000);  // Position.X
        writer.Write((uint)200000);  // Position.Y
        writer.Write((uint)3000);    // Position.Z
        writer.Flush();
        stream.Position = 0;

        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T010203.WLD", stream);

        Assert.Single(result.Items);
        Assert.Equal((ushort)42, result.Items[0].TypeId);
        Assert.Equal((ushort)100, result.Items[0].Rotation.X);
        Assert.Equal(130000u, result.Items[0].Position.X);
        Assert.Equal(1, result.ZoneNumber);
        Assert.Equal(2, result.X);
        Assert.Equal(3, result.Y);
    }

    [Fact]
    public void Extract_CapsAt300Items()
    {
        using var stream = new MemoryStream(new byte[301 * 20]);
        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T010101.WLD", stream);
        Assert.Equal(300, result.Items.Count);
    }

    [Fact]
    public void Extract_ParsesFilenameCoordinates()
    {
        using var stream = new MemoryStream(new byte[20]);
        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T120515.WLD", stream);
        Assert.Equal(12, result.ZoneNumber);
        Assert.Equal(5, result.X);
        Assert.Equal(15, result.Y);
    }

    private static void WriteRecord(BinaryWriter w, ushort typeId, uint x, uint y)
    {
        w.Write(typeId);
        w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0);
        w.Write(x); w.Write(y); w.Write(0u);
    }

    /// <summary>
    /// T091011.WLD ships with 8 corrupt records spliced into the MIDDLE of the file — valid data
    /// resumes after them, still on the 20-byte grid. So the junk must be dropped without the
    /// records after it being lost, which is what makes this a filter and not a truncation.
    /// </summary>
    [Fact]
    public void Extract_DropsCorruptRecordsButKeepsWhatFollowsThem()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteRecord(writer, 141, 130000, 200000);           // in tile 2,3
        WriteRecord(writer, 141, 99045526, 733021675);      // junk: valid id, impossible position
        WriteRecord(writer, 45188, 168858122, 2967010945);  // junk: both
        WriteRecord(writer, 132, 131000, 201000);           // in tile again
        writer.Flush();
        stream.Position = 0;

        var result = new WorldItemExtractor().Extract("T010203.WLD", stream);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(130000u, result.Items[0].Position.X);
        Assert.Equal(131000u, result.Items[1].Position.X);
        Assert.Equal(2, result.DiscardedItems);
    }

    /// <summary>An object anchored on a tile border legitimately overhangs it — the shipped data
    /// has 70 such records, the furthest 32,001 units out — so the filter must not eat them.</summary>
    [Fact]
    public void Extract_KeepsObjectsThatOverhangTheTileBorder()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteRecord(writer, 12, 128000 - 32001, 200000);  // just outside the left edge
        WriteRecord(writer, 13, 192000 + 32001, 200000);  // just outside the right edge
        writer.Flush();
        stream.Position = 0;

        var result = new WorldItemExtractor().Extract("T010203.WLD", stream);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(0, result.DiscardedItems);
    }

    [Fact]
    public void Extract_LeavesTilesWhoseNameDidNotParseAlone()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteRecord(writer, 7, 5_000_000, 5_000_000);
        writer.Flush();
        stream.Position = 0;

        // No Tzzxxyy name, so there is no tile square to judge against — keep everything.
        var result = new WorldItemExtractor().Extract("SCRATCH.WLD", stream);

        Assert.Single(result.Items);
        Assert.Equal(0, result.DiscardedItems);
    }
}
