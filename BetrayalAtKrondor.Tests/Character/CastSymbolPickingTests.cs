namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Picking a spell off the casting ring — <c>UI_GetSymbolAtMouse</c> and the panel's resting list.
/// </summary>
public class CastSymbolPickingTests {
    [Fact]
    public void TheCanonicalHitBoxIsNOTSquare() {
        // The original's box is 10x10 in ITS pixels, and those are taller than they are wide. A
        // canonical caller that passes the unscaled 10 gets a box a fifth of a character across —
        // unhittable with a real mouse, yet every test aimed at a point's exact centre still passes,
        // which is how it survives.
        Assert.Equal(50, CastRingLayout.CanonicalHitBoxWidth);
        Assert.Equal(60, CastRingLayout.CanonicalHitBoxHeight);

        const int pointX = 500, pointY = 600;
        Assert.False(CastRingLayout.Contains(pointX, pointY, pointX + 20, pointY));
        Assert.True(CastRingLayout.Contains(pointX, pointY, pointX + 20, pointY,
            CastRingLayout.CanonicalHitBoxWidth, CastRingLayout.CanonicalHitBoxHeight));
    }

    [Fact]
    public void TheBoxIsStrictAtBothEndsInEitherScale() {
        // Low edge at x - width/2, and both comparisons strict — so a 50-wide box accepts 49.
        const int x = 500, y = 600;
        int w = CastRingLayout.CanonicalHitBoxWidth, h = CastRingLayout.CanonicalHitBoxHeight;

        Assert.False(CastRingLayout.Contains(x, y, x - (w / 2), y, w, h));
        Assert.True(CastRingLayout.Contains(x, y, x - (w / 2) + 1, y, w, h));
        Assert.True(CastRingLayout.Contains(x, y, x + (w / 2) - 1, y, w, h));
        Assert.False(CastRingLayout.Contains(x, y, x + (w / 2), y, w, h));
        Assert.False(CastRingLayout.Contains(x, y, x, y - (h / 2), w, h));
        Assert.False(CastRingLayout.Contains(x, y, x, y + (h / 2), w, h));
    }

    [Fact]
    public void AnUncastableSpellIsNotMerelyGreyedButTransparentToTheCursor() {
        // The castability test is folded INTO the hit test, so the cursor falls through an
        // uncastable symbol to whatever is behind it rather than landing on a dead widget.
        var symbols = new List<(int X, int Y, int SpellId)> { (500, 600, 7), (510, 600, 9) };

        int hit = CastRingLayout.SymbolAt(symbols, 505, 600, spell => spell != 7,
            CastRingLayout.CanonicalHitBoxWidth, CastRingLayout.CanonicalHitBoxHeight);

        Assert.Equal(1, hit);
    }

    [Fact]
    public void OverlappingSymbolsGoToTHEFIRSTBYINDEXNotTheNearest() {
        var symbols = new List<(int X, int Y, int SpellId)> { (500, 600, 7), (510, 600, 9) };

        int hit = CastRingLayout.SymbolAt(symbols, 509, 600, null,
            CastRingLayout.CanonicalHitBoxWidth, CastRingLayout.CanonicalHitBoxHeight);

        Assert.Equal(0, hit);
    }

    [Fact]
    public void NothingUnderTheCursorIsMinusOneRatherThanTheNearestSymbol() {
        var symbols = new List<(int X, int Y, int SpellId)> { (500, 600, 7) };

        Assert.Equal(-1, CastRingLayout.SymbolAt(symbols, 900, 600, null,
            CastRingLayout.CanonicalHitBoxWidth, CastRingLayout.CanonicalHitBoxHeight));
    }

    [Fact]
    public void TheSymbolsColourArgumentsAreInertForTheShippedFont() {
        // SPELL.FNT is a byte per pixel and drawGlyphClipped takes each byte of 5 or more AS the
        // pen, overwriting the caller's colour. Its ink bytes are 0, 6, 35, 108, 110 — all either
        // transparent or at or above the threshold — so the fade never fades and the "selected"
        // shimmer marks nothing. Kept as a rule because it is what the routine says; pinned as
        // inapplicable so nobody builds a highlight out of it and calls it faithful.
        Assert.False(SpellSymbolDisplay.ColourAppliesToTheShippedSymbolFont);
        Assert.All(new[] { 6, 35, 108, 110 },
            pen => Assert.True(pen >= SpellSymbolDisplay.LowestLiteralPen));
    }

    [Fact]
    public void TheNameListStepIsItsOwnAndNotTheBodyLineStep() {
        // Three different steps live in this one box: 13 under the title, 11 between body lines,
        // 10 between names. Reusing the body step spreads a full list past the panel's bottom.
        Assert.NotEqual(SpellInfoPanel.BodyLineStep, SpellInfoPanel.NameListStep);
        Assert.Equal(SpellInfoPanel.TitleY, SpellInfoPanel.NameListY(0));
        Assert.Equal(SpellInfoPanel.TitleY + (2 * SpellInfoPanel.NameListStep),
            SpellInfoPanel.NameListY(2));
    }

    [Fact]
    public void TheListCountsONLYTheNamesItDrew() {
        // The advance sits inside the castable branch, so a skipped spell leaves no gap. Feeding it
        // the loop index instead of the drawn count would hole the list wherever one was skipped.
        Assert.Equal(SpellInfoPanel.NameListY(1),
            SpellInfoPanel.NameListFirstY + SpellInfoPanel.NameListStep);
    }
}
