namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Location;
using ResourceExtraction.Extractors;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class FullMapExtractorTests {
    [Fact]
    public void TownExtractor_ParsesHeaderAndTowns() {
        // Reproduces the head of the shipping FMAP_TWN.DAT: header 3,3,9,9,
        // count, then "LaMut\0" @ (106,86) and "Zun\0" @ (112,95).
        var bytes = new List<byte>();
        AddU16(bytes, 3); AddU16(bytes, 3); AddU16(bytes, 9); AddU16(bytes, 9);
        AddU16(bytes, 2);                                    // town count
        AddU16(bytes, 6); bytes.AddRange("LaMut\0"u8.ToArray()); AddU16(bytes, 106); AddU16(bytes, 86);
        AddU16(bytes, 4); bytes.AddRange("Zun\0"u8.ToArray()); AddU16(bytes, 112); AddU16(bytes, 95);
        using var stream = new MemoryStream(bytes.ToArray());

        FullMapTowns towns = new FullMapTownExtractor().Extract("FMAP_TWN.DAT", stream);

        // Coordinates/sizes are scaled into canonical 1600×1200 space (X×5, Y×6).
        Assert.Equal(3 * 5, towns.IconAnchorX);
        Assert.Equal(3 * 6, towns.IconAnchorY);
        Assert.Equal(9 * 5, towns.IconWidth);
        Assert.Equal(9 * 6, towns.IconHeight);
        Assert.Equal(2, towns.Towns.Count);
        Assert.Equal("LaMut", towns.Towns[0].Name);   // NUL terminator stripped
        Assert.Equal(106 * 5, towns.Towns[0].X);
        Assert.Equal(86 * 6, towns.Towns[0].Y);
        Assert.Equal("Zun", towns.Towns[1].Name);
        Assert.Equal(112 * 5, towns.Towns[1].X);
        Assert.Equal(95 * 6, towns.Towns[1].Y);
    }

    [Fact]
    public void PositionExtractor_ReadsExactlyTwelveZones() {
        // 12 zones: zone 0 has two markers, the rest are empty.
        var bytes = new List<byte>();
        AddU16(bytes, 2); AddU16(bytes, 116); AddU16(bytes, 75); AddU16(bytes, 0xFFFF); AddU16(bytes, 0xFFFF);
        for (int z = 1; z < FullMapPositions.ZoneCount; z++) {
            AddU16(bytes, 0);
        }
        using var stream = new MemoryStream(bytes.ToArray());

        FullMapPositions positions = new FullMapPositionExtractor().Extract("FMAP_XY.DAT", stream);

        Assert.Equal(FullMapPositions.ZoneCount, positions.Zones.Count);
        Assert.Equal(2, positions.Zones[0].Markers.Count);
        // Real positions scaled into canonical space (X×5, Y×6).
        MapMarker? marker = positions.Zones[0].Markers[0];
        Assert.NotNull(marker);
        Assert.Equal(116 * 5, marker!.X);
        Assert.Equal(75 * 6, marker.Y);
        // The (-1,-1) sentinel surfaces as a null entry, keeping the per-tile index.
        Assert.Null(positions.Zones[0].Markers[1]);
        Assert.Empty(positions.Zones[11].Markers);
    }

    private static void AddU16(List<byte> bytes, int value) {
        bytes.Add((byte)(value & 0xFF));
        bytes.Add((byte)((value >> 8) & 0xFF));
    }
}
