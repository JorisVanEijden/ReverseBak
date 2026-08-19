namespace GameData.Resources.Font;

using System.Collections.Generic;

/// <summary>
/// A <c>.FNT</c> font: its metrics and one bitmap per glyph.
/// </summary>
/// <remarks>
/// <b>Not every .FNT is letters.</b> GAME.FNT and BOOK.FNT are text, but SPELL.FNT's glyphs are the
/// casting SYMBOLS — the shapes on the ring and in the active-effects strip — which are drawn as
/// pictures rather than typed. That is why this models glyphs as bitmaps: a consumer that only ever
/// wanted a typeface could ignore them, but one that wants the symbols cannot get them any other
/// way.
/// </remarks>
public class FontResource : IResource {
    public FontResource(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.FNT;

    /// <summary>Rows per glyph — every glyph in a font is the same height.</summary>
    public int Height { get; set; }

    /// <summary>Rows from the top to the baseline.</summary>
    public int Baseline { get; set; }

    /// <summary>The character code of <see cref="Glyphs"/>[0].</summary>
    /// <remarks>
    /// Fonts do not start at zero — the text ones start at space (32) — so a consumer indexing by
    /// character has to subtract this. For a symbol font the "character" is just an index into the
    /// set, and the offset is what maps a symbol number onto its glyph.
    /// </remarks>
    public int FirstCharacter { get; set; }

    /// <summary>The glyphs, in file order, starting at <see cref="FirstCharacter"/>.</summary>
    public List<FontGlyph> Glyphs { get; set; } = new List<FontGlyph>();

    /// <summary>The glyph for a character code, or null when the font does not carry it.</summary>
    public FontGlyph GlyphFor(int character) {
        int index = character - FirstCharacter;

        return index >= 0 && index < Glyphs.Count ? Glyphs[index] : null;
    }
}

/// <summary>One glyph: how wide it is, and which pixels are set.</summary>
/// <remarks>
/// <b>One bit per pixel, most significant bit leftmost, and CEIL(width / 8) bytes per row.</b>
/// Every shipped font happens to fit in one or two bytes — the widest glyph in any of them is 15
/// (BOOK.FNT), and SPELL.FNT's symbols reach only 10 — so the rule is stated as the ceiling rather
/// than as "one byte, or two when wider than eight", which is the same thing for this data and
/// stops being the same thing for a mod's font.
/// </remarks>
public class FontGlyph {
    /// <summary>How far the pen advances, and how many bits of each row belong to this glyph.</summary>
    public int Width { get; set; }

    /// <summary>Bytes per row for this glyph's width.</summary>
    public int BytesPerRow => (Width + 7) / 8;

    /// <summary>One entry per row, each <see cref="BytesPerRow"/> bytes, most significant bit first.</summary>
    public List<byte[]> Rows { get; set; } = new List<byte[]>();

    /// <summary>Whether a pixel is set.</summary>
    /// <param name="x">Column from the left, 0..<see cref="Width"/>-1.</param>
    /// <param name="y">Row from the top.</param>
    public bool IsSet(int x, int y) {
        if (y < 0 || y >= Rows.Count || x < 0 || x >= Width) {
            return false;
        }
        byte[] row = Rows[y];
        int index = x / 8;

        return index < row.Length && (row[index] & (0x80 >> (x % 8))) != 0;
    }
}
