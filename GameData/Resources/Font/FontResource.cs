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

    /// <summary>Whether this font's pixels are packed bits or whole bytes.</summary>
    /// <remarks>
    /// <b>A FONT is one format, and it has to be decided across all its glyphs — never per glyph.</b>
    /// A glyph only proves the format when its row is wider than a bitmask of that width would need,
    /// and the narrow glyphs prove nothing: a one-pixel glyph takes one byte either way. BOOK.FNT
    /// has two of those and PUZZLE.FNT has 155, so a per-glyph rule reads them as palette indices in
    /// fonts that are plainly bitmasks. One glyph anywhere in the font that cannot be a bitmask
    /// settles it for the rest.
    /// </remarks>
    public FontPixelFormat PixelFormat { get; set; } = FontPixelFormat.Monochrome;

    /// <summary>The glyph for a character code, or null when the font does not carry it.</summary>
    public FontGlyph GlyphFor(int character) {
        int index = character - FirstCharacter;

        return index >= 0 && index < Glyphs.Count ? Glyphs[index] : null;
    }
}

/// <summary>How a font stores its pixels.</summary>
/// <remarks>
/// <b>Not every .FNT is a bitmask.</b> The text fonts pack eight pixels to a byte; SPELL.FNT spends
/// a whole byte on each one, which is what lets its symbols carry colour. Which it is cannot be
/// assumed — it is derived from the strides across the whole font, see
/// <see cref="FontResource.PixelFormat"/>.
/// </remarks>
public enum FontPixelFormat {
    /// <summary>One bit per pixel, most significant bit leftmost.</summary>
    Monochrome,

    /// <summary>One byte per pixel — a palette index, zero meaning nothing drawn.</summary>
    Paletted,
}

/// <summary>One glyph: how wide it is, and which pixels are set.</summary>
/// <remarks>
/// <b>The row stride comes from the OFFSET TABLE, not from the width.</b> Glyph data is addressed
/// by offset, and dividing a glyph's span by the font's height gives its bytes per row — which is
/// the only way to tell the two pixel formats apart. Reading SPELL.FNT as a bitmask, which its
/// widths alone would suggest, produces noise: its rows are ten bytes for ten pixels.
/// </remarks>
public class FontGlyph {
    /// <summary>How far the pen advances, and how many pixels of each row belong to this glyph.</summary>
    public int Width { get; set; }

    /// <summary>Bytes per row, as the offset table gives it.</summary>
    public int BytesPerRow { get; set; }

    /// <summary>The format of this glyph's rows — the font's, which is where it is decided.</summary>
    /// <seealso cref="FontResource.PixelFormat"/>
    public FontPixelFormat PixelFormat { get; set; } = FontPixelFormat.Monochrome;

    /// <summary>Whether this glyph's stride is more than a bitmask of its width would need.</summary>
    /// <remarks>
    /// Evidence FOR the paletted format, and only that — a false answer means "this glyph does not
    /// say", not "the font is a bitmask", which is why the font decides and the glyph does not.
    /// </remarks>
    public bool StrideExceedsABitmask => BytesPerRow > ((Width + 7) / 8);

    /// <summary>One entry per row, each <see cref="BytesPerRow"/> bytes.</summary>
    public List<byte[]> Rows { get; set; } = new List<byte[]>();

    /// <summary>The palette index at a pixel, or 0 for nothing drawn.</summary>
    /// <param name="x">Column from the left, 0..<see cref="Width"/>-1.</param>
    /// <param name="y">Row from the top.</param>
    /// <remarks>
    /// A monochrome font answers 1 for a set pixel, so a caller that only wants "is there ink here"
    /// can treat both formats the same; one that wants the symbol's own colours reads the index.
    /// </remarks>
    public int PixelAt(int x, int y) {
        if (y < 0 || y >= Rows.Count || x < 0 || x >= Width) {
            return 0;
        }
        byte[] row = Rows[y];
        if (PixelFormat == FontPixelFormat.Paletted) {
            return x < row.Length ? row[x] : 0;
        }
        int index = x / 8;

        return index < row.Length && (row[index] & (0x80 >> (x % 8))) != 0 ? 1 : 0;
    }

    /// <summary>Whether anything is drawn at a pixel.</summary>
    public bool IsSet(int x, int y) => PixelAt(x, y) != 0;
}
