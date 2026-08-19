namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.Font;
using ResourceExtraction.Extractors;
using System.IO;
using System.Linq;
using Xunit;

/// <summary>
/// The reader against the shipped fonts. Skip-if-absent, like the other game-data tests.
/// </summary>
public class FontExtractorShippedTests {
    [Theory]
    [InlineData("GAME.FNT", 10, 32, 95)]
    [InlineData("BOOK.FNT", 15, 32, 96)]
    [InlineData("SPELL.FNT", 9, 0, 96)]
    [InlineData("PUZZLE.FNT", 8, 0, 251)]
    public void EveryShippedFontReadsWholeAndInStep(string name, int height, int first, int glyphs) {
        string path = Find(name);
        if (path == null) {
            return;
        }

        FontResource font = Read(path, name);

        Assert.Equal(height, font.Height);
        Assert.Equal(first, font.FirstCharacter);
        Assert.Equal(glyphs, font.Glyphs.Count);
        // Every glyph got its full complement of rows: a reader out of step runs short on the last.
        Assert.All(font.Glyphs, g => Assert.Equal(height, g.Rows.Count));
        Assert.All(font.Glyphs, g => Assert.All(g.Rows, r => Assert.Equal(g.BytesPerRow, r.Length)));
    }

    [Fact]
    public void TheSpellFontIsSymbolsRatherThanLetters() {
        string path = Find("SPELL.FNT");
        if (path == null) {
            return;
        }

        FontResource font = Read(path, "SPELL.FNT");

        // It starts at character ZERO, where the text fonts start at space — so the glyphs are
        // indexed as a SET rather than addressed by character, which is what a symbol font is.
        // PUZZLE.FNT does the same; GAME and BOOK both start at 32.
        Assert.Equal(0, font.FirstCharacter);
        // *** AND ITS PIXELS ARE BYTES, NOT BITS. *** Ten bytes a row for a ten-pixel glyph, where
        // GAME.FNT spends one byte on eight pixels. That is why the stride has to come from the
        // offset table: read as a bitmask — which its widths alone would suggest — a symbol decodes
        // to noise that still looks like a drawing went wrong somewhere else.
        Assert.Equal(3, font.GlyphFormat);
        Assert.Equal(FontPixelFormat.Paletted, font.PixelFormat);
        Assert.Contains(font.Glyphs, g => g.Rows.Any(r => r.Any(b => b != 0)));
    }

    [Theory]
    [InlineData("GAME.FNT")]
    [InlineData("BOOK.FNT")]
    [InlineData("PUZZLE.FNT")]
    public void TheTextAndPuzzleFontsSayTheyAreBitmasks(string name) {
        string path = Find(name);
        if (path == null) {
            return;
        }

        FontResource font = Read(path, name);

        // 0xFF in the header, negated to 1. Measuring the glyph data instead would agree here, but
        // it is an inference where the file makes a statement — and a one-pixel glyph (BOOK has two,
        // PUZZLE 155) takes one byte in EITHER format, so measurement has nothing to go on there.
        Assert.Equal(FontResource.MonochromeGlyphFormat, font.GlyphFormat);
        Assert.Equal(FontPixelFormat.Monochrome, font.PixelFormat);
        Assert.All(font.Glyphs, g => Assert.Equal(FontPixelFormat.Monochrome, g.PixelFormat));
    }

    private static FontResource Read(string path, string name) {
        using FileStream stream = File.OpenRead(path);

        return new FontExtractor().Extract(name, stream);
    }

    /// <summary>The game directory, walking up from the test binary as the other data tests do.</summary>
    private static string Find(string name) {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null) {
            string candidate = Path.Combine(dir.FullName, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }

        return null;
    }
}
