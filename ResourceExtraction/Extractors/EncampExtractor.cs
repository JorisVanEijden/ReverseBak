namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using ResourceExtraction.Imaging;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Parses ENCAMP.DAT — the encampment / rest-screen geometry (icon hit-box + clock and
/// needle point arrays). See <see cref="EncampData"/> for the layout and the IDA references
/// (Load_encamp @ ovr182:0x7087e).
///
/// On-disk values are mode-13h pixels (X over 320, Y over 200); they are scaled into the
/// canonical 1600×1200 space (×5 / ×6) so positions match ENCAMP.SCX and the REQ/GDS/Label
/// coordinate space — the same treatment as FMAP.
/// </summary>
public class EncampExtractor : ExtractorBase<EncampData> {
    public override EncampData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new EncampData(id) {
            IconAnchorX = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
            IconAnchorY = AspectCorrection.ScaleVgaY(reader.ReadUInt16()),
            IconWidth = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
            IconHeight = AspectCorrection.ScaleVgaY(reader.ReadUInt16()),
        };

        data.ClockEntries = ReadPoints(reader);
        data.NeedleEntries = ReadPoints(reader);
        return data;
    }

    // A u16 count followed by that many (x, y) u16 pairs, each scaled to canonical space.
    private static List<EncampPoint> ReadPoints(BinaryReader reader) {
        int count = reader.ReadUInt16();
        var points = new List<EncampPoint>(count);
        for (int i = 0; i < count; i++) {
            points.Add(new EncampPoint {
                X = AspectCorrection.ScaleVgaX(reader.ReadUInt16()),
                Y = AspectCorrection.ScaleVgaY(reader.ReadUInt16()),
            });
        }
        return points;
    }
}
