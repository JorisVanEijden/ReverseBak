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
        Assert.True(font.Glyphs.Max(g => g.Width) <= 16,
            "no shipped glyph needs more than two bytes a row");
        Assert.Contains(font.Glyphs, g => g.Rows.Any(r => r.Any(b => b != 0)));
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
