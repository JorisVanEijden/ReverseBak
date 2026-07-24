namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.Spells;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Parses SYMBOL1.DAT .. SYMBOL6.DAT — the per-category spell-selection layouts.
/// On-disk: u16 count, then count * { u16 spellNumber; u16 x; u16 y; u8 character }.
/// The engine increments `character` by 1 at load time, so the emitted FontGlyph = on-disk + 1.
/// On-disk coordinates are in the original 320x200 VGA casting screen; they are scaled to the
/// canonical 1600x1200 display space (x*5, y*6) so no VGA pixel coords leak onto the model.
/// Verified against IDA ovr173 ReadSymbolsAndRingDat @0x6900c.
/// </summary>
public class SpellSymbolExtractor : ExtractorBase<SpellSymbolLayout> {
    // 320x200 VGA -> canonical 1600x1200 display space.
    private const int ScaleX = 1600 / 320; // 5
    private const int ScaleY = 1200 / 200; // 6

    public override SpellSymbolLayout Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var layout = new SpellSymbolLayout(id);

        // SYMBOL<n>.DAT -> zero-based category n-1.
        Match match = Regex.Match(id, @"(\d+)");
        if (match.Success)
            layout.Category = int.Parse(match.Groups[1].Value) - 1;

        ushort count = reader.ReadUInt16();
        for (int i = 0; i < count; i++) {
            ushort spellId = reader.ReadUInt16();
            ushort x = reader.ReadUInt16();
            ushort y = reader.ReadUInt16();
            byte character = reader.ReadByte();
            layout.Nodes.Add(new SpellSymbolNode {
                SpellId = spellId,
                SpellKey = ContentKey.ForBase("spell", spellId),
                X = x * ScaleX,
                Y = y * ScaleY,
                FontGlyph = character + 1, // engine does character++ at load
            });
        }

        return layout;
    }
}
