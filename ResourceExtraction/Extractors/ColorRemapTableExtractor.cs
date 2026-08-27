namespace ResourceExtraction.Extractors;

using GameData.Resources.Palette;
using ResourceExtraction.Imaging;
using System.IO;

/// <summary>
/// Extracts <c>CS&lt;n&gt;.DAT</c> — a flat 256-byte index→index remap — into
/// <see cref="ColorRemapTable"/>.
/// </summary>
/// <remarks>
/// No header, no count: the file IS the table, and all ten ship at exactly 256 bytes. The read
/// itself is <see cref="CreatureColorSet.ReadLut"/>, which was already here and had no production
/// consumer; this only gives it a resource type so the game can ask for one by name.
/// </remarks>
public class ColorRemapTableExtractor : ExtractorBase<ColorRemapTable> {
    public override ColorRemapTable Extract(string id, Stream resourceStream) =>
        new ColorRemapTable(id) { Lut = CreatureColorSet.ReadLut(resourceStream) };
}
