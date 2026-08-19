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
        Assert.Equal(FontPixelFormat.Paletted, font.PixelFormat);
        Assert.Contains(font.Glyphs, g => g.Rows.Any(r => r.Any(b => b != 0)));
    }

    [Theory]
    [InlineData("GAME.FNT")]
    [InlineData("BOOK.FNT")]
    [InlineData("PUZZLE.FNT")]
    public void TheTextAndPuzzleFontsStayBitmasksDespiteTheirNarrowGlyphs(string name) {
        string path = Find(name);
        if (path == null) {
            return;
        }

        FontResource font = Read(path, name);

        // *** A ONE-PIXEL GLYPH TAKES ONE BYTE IN EITHER FORMAT. *** BOOK.FNT has two of them and
        // PUZZLE.FNT 155, so a per-glyph "stride at least the width" rule calls those paletted and
        // reads a packed byte as a palette index. The font decides for all its glyphs, which is why
        // these stay bitmasks — no glyph in them has a row too wide to be one.
        Assert.Equal(FontPixelFormat.Monochrome, font.PixelFormat);
        Assert.All(font.Glyphs, g => Assert.Equal(FontPixelFormat.Monochrome, g.PixelFormat));
        Assert.DoesNotContain(font.Glyphs, g => g.StrideExceedsABitmask);
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
