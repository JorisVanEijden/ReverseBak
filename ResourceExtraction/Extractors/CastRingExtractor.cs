namespace ResourceExtraction.Extractors;

using GameData.Resources.Spells;
using System.IO;
using System.Text;

/// <summary>
/// Parses RING.DAT — the 30 positions of the spell-casting ring.
/// On-disk: u16 x[30] then u16 y[30] (two parallel arrays).
/// Coordinates are scaled from 320x200 VGA to the canonical 1600x1200 space (x*5, y*6),
/// and every 5th position is flagged as a spell-category anchor.
/// Verified against IDA ovr173 ReadSymbolsAndRingDat @0x6900c.
/// </summary>
public class CastRingExtractor : ExtractorBase<CastRing> {
    private const int PositionCount = 30;
    // 320x200 VGA -> canonical 1600x1200 display space.
    private const int ScaleX = 1600 / 320; // 5
    private const int ScaleY = 1200 / 200; // 6

    public override CastRing Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var ring = new CastRing(id);

        var xs = new ushort[PositionCount];
        for (int i = 0; i < PositionCount; i++)
            xs[i] = reader.ReadUInt16();

        for (int i = 0; i < PositionCount; i++) {
            ushort y = reader.ReadUInt16();
            ring.Positions.Add(new RingPosition {
                X = xs[i] * ScaleX,
                Y = y * ScaleY,
                IsCategoryAnchor = (i + 1) % 5 == 0, // indices 4, 9, 14, 19, 24, 29
            });
        }

        return ring;
    }
}
