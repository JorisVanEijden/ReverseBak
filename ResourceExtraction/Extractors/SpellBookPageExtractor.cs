namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.Spells;
using System.IO;
using System.Text;

/// <summary>
/// Parses INVSPELL.DAT — the character sheet's spellbook page.
///
/// On-disk: six groups, each <c>{ u16 icon; u16 count; count * { char name[0x18]; u16 spellId } }</c>.
/// Verified against <c>charscreen_draw_spell_book_actor</c> (canassa <c>SRC/CHAR/CHARSCRN.C</c>),
/// which reads exactly this shape — two u16s then <c>count</c> reads of 0x1a bytes — and against
/// the shipped bytes (first group: icon 0x25, 6 spells, "Bane of Black Slayers" then id 9).
///
/// <para>The loader is reached only for a spellcasting character. Its filename in the executable is
/// <c>"InvSpell.dat"</c> — mixed case, which is neither the lowercase fopen convention nor the
/// uppercase dedicated-loader one, and is why earlier single-case searches concluded the file had
/// no loader at all.</para>
/// </summary>
public class SpellBookPageExtractor : ExtractorBase<SpellBookPage> {
    private const int GroupCount = 6;
    private const int NameLength = 0x18;

    public override SpellBookPage Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var page = new SpellBookPage(id);

        for (var group = 0; group < GroupCount; group++) {
            var row = new SpellBookGroup { Icon = reader.ReadUInt16() };
            int count = reader.ReadUInt16();
            for (var i = 0; i < count; i++) {
                string name = ReadFixedName(reader);
                int spellId = reader.ReadUInt16();
                row.Spells.Add(new SpellBookEntry {
                    Name = name,
                    SpellId = spellId,
                    SpellKey = ContentKey.ForBase("spell", spellId),
                });
            }
            page.Groups.Add(row);
        }
        return page;
    }

    // Fixed 24-byte field, NUL-padded; the engine treats it as a C string, so stop at the first NUL.
    private static string ReadFixedName(BinaryReader reader) {
        byte[] raw = reader.ReadBytes(NameLength);
        int length = 0;
        while (length < raw.Length && raw[length] != 0) {
            length++;
        }
        return Encoding.GetEncoding(DosCodePage).GetString(raw, 0, length);
    }
}
