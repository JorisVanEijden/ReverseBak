namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Font;
using ResourceExtraction.Extractors;
using System.IO;
using Xunit;

/// <summary>
/// The <c>.FNT</c> reader — metrics and glyph bitmaps.
/// </summary>
/// <remarks>
/// Built from a hand-written file rather than the shipped ones so the bit order is pinned by
/// something whose answer is known independently of the parser.
/// </remarks>
public class FontExtractorTests {
    [Fact]
    public void ReadsTheMetricsAndOneGlyphPerCharacter() {
        FontResource font = Extract();

        Assert.Equal(3, font.Height);
        Assert.Equal(2, font.Baseline);
        Assert.Equal(32, font.FirstCharacter);
        Assert.Equal(2, font.Glyphs.Count);
    }

    [Fact]
    public void APixelIsSetFromTheTOPBitDown() {
        // One bit per pixel, most significant first — a reader that counted from the low end would
        // mirror every glyph, which on a SYMBOL font is not obviously wrong to look at.
        FontGlyph glyph = Extract().Glyphs[0];

        Assert.True(glyph.IsSet(0, 0), "the leftmost pixel of row 0 is set");
        Assert.False(glyph.IsSet(1, 0));
        Assert.True(glyph.IsSet(7, 1), "the rightmost pixel of row 1 is set");
    }

    [Fact]
    public void TheStrideComesFromTheOffsetTableRatherThanTheWidth() {
        // A glyph's span divided by the height is its bytes per row, and that is the ONLY thing
        // that distinguishes a bitmask font from a byte-per-pixel one. GAME.FNT packs eight pixels
        // into a byte; SPELL.FNT spends a byte on each.
        FontResource font = Extract();

        Assert.Equal(1, font.Glyphs[0].BytesPerRow);
        Assert.Equal(2, font.Glyphs[1].BytesPerRow);
        Assert.Equal(FontPixelFormat.Monochrome, font.PixelFormat);
        Assert.True(font.Glyphs[1].IsSet(11, 0), "the last column of a 12-wide glyph is in byte 1");
    }

    [Fact]
    public void AskingForACharacterTheFontDoesNotCarryAnswersNothing() {
        FontResource font = Extract();

        Assert.NotNull(font.GlyphFor(32));
        Assert.Null(font.GlyphFor(31));    // before the first character
        Assert.Null(font.GlyphFor(34));    // past the last
    }

    [Fact]
    public void TheHEADERSaysWhichPixelFormatAFontIsIn() {
        // 0xFD negates to 3, and the blitter's test is "greater than 1" — so a byte per pixel, and
        // every glyph in the font takes that reading including the one-pixel one, whose single byte
        // would measure the same either way.
        FontResource font = ExtractPaletted();

        Assert.Equal(3, font.GlyphFormat);
        Assert.Equal(FontPixelFormat.Paletted, font.PixelFormat);
        Assert.All(font.Glyphs, g => Assert.Equal(FontPixelFormat.Paletted, g.PixelFormat));
        Assert.Equal(0x6c, font.Glyphs[0].PixelAt(0, 0));
        Assert.Equal(0x2f, font.Glyphs[1].PixelAt(3, 0));
    }

    /// <summary>A two-glyph font: one 8 wide, one 12 wide, three rows each.</summary>
    private static FontResource Extract() {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        // The compressed payload is written uncompressed (type 0), so the reader's decompressor is
        // the identity and the bytes below are exactly what it sees.
        w.Write(new byte[] { (byte)'F', (byte)'N', (byte)'T', 0x3A });   // tag, NUL-terminated
        w.Write(0u);            // file size, unread
        w.Write((byte)0xff);    // glyph format: 0xFF negates to 1 — one bit per pixel
        w.Write((byte)8);       // nominal width, unread
        w.Write((byte)3);       // height
        w.Write((byte)2);       // baseline
        w.Write((byte)32);      // first character
        w.Write((byte)2);       // glyph count
        w.Write((ushort)0);     // payload length, unread
        w.Write((byte)0);       // compression: none
        w.Write(15u);           // decompressed size, unread — the reader takes the rest

        w.Write((ushort)0);     // offsets: glyph 0 at 0, glyph 1 three rows later
        w.Write((ushort)3);
        w.Write((byte)8);       // widths
        w.Write((byte)12);

        // Glyph 0, 8 wide: one byte a row. Row 0 sets the leftmost pixel, row 1 the rightmost.
        w.Write(new byte[] { 0b1000_0000, 0b0000_0001, 0 });
        // Glyph 1, 12 wide: two bytes a row, and column 11 is bit 4 of the low byte.
        w.Write(new byte[] { 0, 0b0001_0000, 0, 0, 0, 0 });
        w.Flush();
        body.Position = 0;

        return new FontExtractor().Extract("TEST.FNT", body);
    }

    /// <summary>A byte-per-pixel font: one glyph 1 wide, one 4 wide, two rows each.</summary>
    private static FontResource ExtractPaletted() {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        w.Write(new byte[] { (byte)'F', (byte)'N', (byte)'T', 0x3A });
        w.Write(0u);
        w.Write((byte)0xfd);    // glyph format: 0xFD negates to 3 — one byte per pixel
        w.Write((byte)4);
        w.Write((byte)2);       // height
        w.Write((byte)2);
        w.Write((byte)0);       // first character — a symbol font is indexed from zero
        w.Write((byte)2);
        w.Write((ushort)0);
        w.Write((byte)0);
        w.Write(10u);

        w.Write((ushort)0);     // glyph 0 spans 2 bytes: 1 byte a row
        w.Write((ushort)2);     // glyph 1 spans the rest, 8 bytes: 4 bytes a row
        w.Write((byte)1);       // widths
        w.Write((byte)4);

        w.Write(new byte[] { 0x6c, 0x00 });
        w.Write(new byte[] { 0x00, 0x00, 0x00, 0x2f, 0, 0, 0, 0 });
        w.Flush();
        body.Position = 0;

        return new FontExtractor().Extract("SYM.FNT", body);
    }
}
