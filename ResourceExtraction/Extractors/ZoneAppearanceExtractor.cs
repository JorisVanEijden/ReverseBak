namespace ResourceExtraction.Extractors;

using GameData.Resources.World;
using System.IO;
using System.Text;

/// <summary>
/// Reads <c>Z##.DAT</c> — <c>ResourceLoad_Z__.DAT</c> (IDA 0x6d3a0).
/// </summary>
/// <remarks>
/// Three 16-bit pens, a 16-bit count, then that many (pen, replacement) byte pairs. Only the
/// changes are stored — the original fills a 256-entry table with identity first, which is why a
/// file with no pairs (the underground zones ship one) still leaves a usable table.
/// </remarks>
public class ZoneAppearanceExtractor : ExtractorBase<ZoneAppearance> {
    public override ZoneAppearance Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var appearance = new ZoneAppearance(id) {
            SkyPen = reader.ReadUInt16(),
            GroundPen = reader.ReadUInt16(),
            UnusedPen = reader.ReadUInt16(),
        };

        int pairs = reader.ReadUInt16();
        var remaps = new PenRemap[pairs];
        for (var i = 0; i < pairs; i++) {
            remaps[i] = new PenRemap { Pen = reader.ReadByte(), DrawnAs = reader.ReadByte() };
        }

        appearance.Remaps = remaps;
        return appearance;
    }
}
