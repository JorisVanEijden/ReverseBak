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
    public void AGlyphSpendsCeilingOfItsWidthInBytesPerRow() {
        // The ceiling of the width, not a fixed count — every shipped font needs one or two, and
        // the rule is stated as the format states it rather than as what this data happens to use.
        FontResource font = Extract();

        Assert.Equal(1, font.Glyphs[0].BytesPerRow);   // 8 wide
        Assert.Equal(2, font.Glyphs[1].BytesPerRow);   // 12 wide
        Assert.True(font.Glyphs[1].IsSet(11, 0), "the last column of a 12-wide glyph is in byte 1");
    }

    [Fact]
    public void AskingForACharacterTheFontDoesNotCarryAnswersNothing() {
        FontResource font = Extract();

        Assert.NotNull(font.GlyphFor(32));
        Assert.Null(font.GlyphFor(31));    // before the first character
        Assert.Null(font.GlyphFor(34));    // past the last
    }

    /// <summary>A two-glyph font: one 8 wide, one 12 wide, three rows each.</summary>
    private static FontResource Extract() {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        // The compressed payload is written uncompressed (type 0), so the reader's decompressor is
        // the identity and the bytes below are exactly what it sees.
        w.Write(new byte[] { (byte)'F', (byte)'N', (byte)'T', 0x3A });   // tag, NUL-terminated
        w.Write(0u);            // file size, unread
        w.Write((byte)1);       // version
        w.Write((byte)8);       // nominal width, unread
        w.Write((byte)3);       // height
        w.Write((byte)2);       // baseline
        w.Write((byte)32);      // first character
        w.Write((byte)2);       // glyph count
        w.Write((ushort)0);     // payload length, unread
        w.Write((byte)0);       // compression: none
        w.Write(15u);           // decompressed size, unread — the reader takes the rest

        w.Write((ushort)0);     // offsets, read and discarded
        w.Write((ushort)0);
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
}
