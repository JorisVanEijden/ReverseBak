namespace ResourceExtraction.Extractors;

using GameData.Resources.Location;
using ResourceExtraction.Imaging;
using System.IO;
using System.Text;

/// <summary>
/// Parses FMAP_TWN.DAT — the world-map town labels. See <see cref="FullMapTowns"/>
/// for the layout and the IDA references (Load_fmap_twn.dat @ ovr183:0x713cd).
///
/// Layout: four u16 geometry constants, a u16 town count, then for each town a
/// u16 name length, that many ASCII bytes (NUL-terminated in the file), and the
/// label's (x, y) as s16 fullmap pixel coordinates.
/// </summary>
public class FullMapTownExtractor : ExtractorBase<FullMapTowns> {
    public override FullMapTowns Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        // On-disk values are mode-13h pixels (X/W over 320, Y/H over 200); scale
        // into the canonical 1600x1200 space (x5 / x6) so positions match the
        // FULLMAP.SCX background and REQ/GDS/Label coords.
        var towns = new FullMapTowns(id) {
            IconAnchorX = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
            IconAnchorY = AspectCorrection.ScaleVgaY(reader.ReadUInt16()),
            IconWidth = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
            IconHeight = AspectCorrection.ScaleVgaY(reader.ReadUInt16())
        };

        int count = reader.ReadUInt16();
        for (int i = 0; i < count; i++) {
            int len = reader.ReadUInt16();
            byte[] raw = reader.ReadBytes(len);
            towns.Towns.Add(new FullMapTown {
                Name = DecodeName(raw),
                X = AspectCorrection.ScaleVgaX(reader.ReadInt16()),
                Y = AspectCorrection.ScaleVgaY(reader.ReadInt16())
            });
        }
        return towns;
    }

    private static string DecodeName(byte[] raw) {
        int end = System.Array.IndexOf(raw, (byte)0);
        int length = end >= 0 ? end : raw.Length;
        return Encoding.ASCII.GetString(raw, 0, length);
    }
}

/// <summary>
/// Parses FMAP_XY.DAT — the per-tile "you are here" marker positions. See
/// <see cref="FullMapPositions"/> for the layout and the IDA references
/// (Load_fmap_xy.dat @ ovr183:0x71634).
///
/// Layout: exactly 12 zones, each a u16 marker count followed by that many
/// (x, y) s16 pairs in fullmap pixel coordinates ((-1,-1) = no marker).
/// </summary>
public class FullMapPositionExtractor : ExtractorBase<FullMapPositions> {
    // On disk, a tile with no map marker stores (-1, -1).
    private const int NoMarkerSentinel = -1;

    public override FullMapPositions Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var positions = new FullMapPositions(id);

        for (int zone = 0; zone < FullMapPositions.ZoneCount; zone++) {
            int tileCount = reader.ReadUInt16();
            var zoneData = new FullMapZone();
            for (int tile = 0; tile < tileCount; tile++) {
                int x = reader.ReadInt16();
                int y = reader.ReadInt16();
                // No-marker tiles surface as null; real positions are scaled into
                // canonical 1600x1200 space (X×5, Y×6).
                bool hasMarker = x != NoMarkerSentinel || y != NoMarkerSentinel;
                zoneData.Markers.Add(hasMarker
                    ? new MapMarker {
                        X = AspectCorrection.ScaleVgaX(x),
                        Y = AspectCorrection.ScaleVgaY(y)
                    }
                    : null);
            }
            positions.Zones.Add(zoneData);
        }
        return positions;
    }
}
