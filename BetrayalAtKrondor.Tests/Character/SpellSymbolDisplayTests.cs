namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

public class SpellSymbolDisplayTests {
    [Fact]
    public void TheStoredPositionIsTheGlyphsCentreNotItsCorner() {
        // A 40-wide glyph on a node at (210, 270), with the canonical half-line box of 30.
        (int x, int y) = SpellSymbolDisplay.GlyphOrigin(210, 270, glyphWidth: 40, halfLineBox: 30);

        Assert.Equal(190, x);
        Assert.Equal(240, y);
    }

    [Fact]
    public void AWiderGlyphMovesFurtherLeftButNotDown() {
        (int narrowX, int narrowY) = SpellSymbolDisplay.GlyphOrigin(100, 100, 10, 30);
        (int wideX, int wideY) = SpellSymbolDisplay.GlyphOrigin(100, 100, 50, 30);

        Assert.Equal(narrowX - 20, wideX);
        Assert.Equal(narrowY, wideY);
    }

    [Fact]
    public void TheFadeBrightensByOneStepPerPass() {
        Assert.Equal(10, SpellSymbolDisplay.FadeColour(10, pass: 0, colourStep: 3));
        Assert.Equal(13, SpellSymbolDisplay.FadeColour(10, pass: 1, colourStep: 3));
        Assert.Equal(28, SpellSymbolDisplay.FadeColour(10, SpellSymbolDisplay.FadePasses - 1, 3));
    }

    [Fact]
    public void AColourStepOfZeroSkipsTheFadeEntirely() {
        // Which is how the screen redraws symbols without replaying the entrance.
        Assert.False(SpellSymbolDisplay.FadeRuns(0));
        Assert.True(SpellSymbolDisplay.FadeRuns(1));
        Assert.True(SpellSymbolDisplay.FadeRuns(-1));
    }

    [Fact]
    public void TheSettledColourIsBrighterThanTheFadeEverReaches() {
        int lastFadePass = SpellSymbolDisplay.FadeColour(10, SpellSymbolDisplay.FadePasses - 1, 3);
        int settled = SpellSymbolDisplay.SettledColour(10, 3);

        Assert.Equal(28, lastFadePass);
        Assert.Equal(46, settled);
        Assert.True(settled > lastFadePass, "reusing the last fade colour leaves the ring dim");
    }

    [Fact]
    public void TheSelectedSymbolCyclesEightColoursEveryFourTicks() {
        Assert.Equal(208, SpellSymbolDisplay.SelectedColour(0));
        Assert.Equal(208, SpellSymbolDisplay.SelectedColour(3));
        Assert.Equal(209, SpellSymbolDisplay.SelectedColour(4));
        Assert.Equal(215, SpellSymbolDisplay.SelectedColour(31));
        // And it wraps rather than running away.
        Assert.Equal(208, SpellSymbolDisplay.SelectedColour(32));
    }
}