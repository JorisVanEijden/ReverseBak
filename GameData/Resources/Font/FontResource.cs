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

    /// <summary>
    /// The engine's <c>fontGlyphFormat</c> — the header byte, negated.
    /// </summary>
    /// <remarks>
    /// <b>The file states its format; nothing about it has to be inferred.</b> The shipped fonts
    /// carry 0xFF (format 1) or 0xFD (format 3), and the loader refuses anything else. Bit 0 means
    /// "variable width, addressed through the offset table" — set in both — and the value's size is
    /// what picks the pixel layout, which is <see cref="PixelFormat"/>.
    /// </remarks>
    public int GlyphFormat { get; set; } = MonochromeGlyphFormat;

    /// <summary>The shipped format of GAME, BOOK and PUZZLE — one bit per pixel.</summary>
    public const int MonochromeGlyphFormat = 1;

    /// <summary>
    /// Whether this font's pixels are packed bits or whole bytes.
    /// </summary>
    /// <remarks>
    /// The blitter's own test: <c>fontGlyphFormat &gt; 1</c> is a byte per pixel, otherwise packed
    /// bits. So the boundary is the format number, not anything measured off the glyph data.
    /// </remarks>
    public FontPixelFormat PixelFormat =>
        GlyphFormat > MonochromeGlyphFormat ? FontPixelFormat.Paletted : FontPixelFormat.Monochrome;

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
/// assumed — the font's header says which it is, see <see cref="FontResource.GlyphFormat"/>.
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

    /// <summary>One entry per row, each <see cref="BytesPerRow"/> bytes.</summary>
    public List<byte[]> Rows { get; set; } = new List<byte[]>();

    /// <summary>The palette pen at a pixel, or 0 for nothing drawn.</summary>
    /// <param name="x">Column from the left, 0..<see cref="Width"/>-1.</param>
    /// <param name="y">Row from the top.</param>
    /// <remarks>
    /// A monochrome font answers 1 for a set pixel, so a caller that only wants "is there ink here"
    /// can treat both formats the same; one that wants the symbol's own colours reads the pen.
    ///
    /// <para><b>The byte IS the pen, and it overrides whatever colour the caller set.</b>
    /// <c>drawGlyphClipped</c> assigns <c>textColor = byte</c> for every byte of 5 or more, and
    /// remaps the four below that through a small table. So a paletted font ignores the ink it is
    /// drawn with — see <c>SpellSymbolDisplay</c> for what that costs the casting ring.
    /// </para>
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
