namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// The casting ring's arithmetic. The hit box is the interesting part: it is written as 10 wide and
/// accepts 9.
/// </summary>
public class CastRingLayoutTests {
    private static List<RingPosition> Ring() {
        var positions = new List<RingPosition>();
        for (var i = 0; i < CastRingLayout.PositionCount; i++) {
            positions.Add(new RingPosition { X = 100 + (i * 50), Y = 200 });
        }
        return positions;
    }

    [Fact]
    public void TheRingIsSixCategoriesOfFive() {
        Assert.Equal(30, CastRingLayout.PositionCount);
        Assert.Equal(5, CastRingLayout.PositionsPerCategory);
        Assert.Equal(6, CastRingLayout.CategoryCount);
    }

    [Fact]
    public void EachCategorysAnchorIsTheLastOfItsFive() {
        // Matches the shipped RING.DAT, whose anchors sit at 4, 9, 14, 19, 24, 29.
        Assert.Equal(new[] { 4, 9, 14, 19, 24, 29 }, new[] {
            CastRingLayout.AnchorPositionOf(0), CastRingLayout.AnchorPositionOf(1),
            CastRingLayout.AnchorPositionOf(2), CastRingLayout.AnchorPositionOf(3),
            CastRingLayout.AnchorPositionOf(4), CastRingLayout.AnchorPositionOf(5),
        });
    }

    [Fact]
    public void PositionsMapBackToTheirCategory() {
        Assert.Equal(0, CastRingLayout.CategoryOf(0));
        Assert.Equal(0, CastRingLayout.CategoryOf(4));
        Assert.Equal(1, CastRingLayout.CategoryOf(5));
        Assert.Equal(5, CastRingLayout.CategoryOf(29));

        for (var category = 0; category < CastRingLayout.CategoryCount; category++) {
            Assert.Equal(category,
                CastRingLayout.CategoryOf(CastRingLayout.AnchorPositionOf(category)));
        }
    }

    [Fact]
    public void TheHitBoxAcceptsNinePixelsNotTen() {
        // Strict at both ends: with the low edge at x-5 the accepted range is x-4 .. x+4. An
        // inclusive test would make every point one pixel easier to hit than the original.
        Assert.True(CastRingLayout.Contains(100, 100, 96, 100));
        Assert.True(CastRingLayout.Contains(100, 100, 104, 100));
        Assert.False(CastRingLayout.Contains(100, 100, 95, 100));
        Assert.False(CastRingLayout.Contains(100, 100, 105, 100));
    }

    [Fact]
    public void TheBoxIsSquare() {
        Assert.True(CastRingLayout.Contains(100, 100, 100, 96));
        Assert.False(CastRingLayout.Contains(100, 100, 100, 105));
    }

    [Fact]
    public void APositionIsFoundUnderTheCursor() {
        List<RingPosition> ring = Ring();

        Assert.Equal(0, CastRingLayout.PositionAt(ring, 100, 200));
        Assert.Equal(3, CastRingLayout.PositionAt(ring, 250, 200));
        Assert.Equal(-1, CastRingLayout.PositionAt(ring, 125, 200));
    }

    [Fact]
    public void APositionOutsideTheBandIsNotClickable() {
        // The slider limits selection to an affordable range. A point outside it is simply not
        // clickable — it is not clamped to the nearest one that is.
        List<RingPosition> ring = Ring();

        Assert.Equal(-1, CastRingLayout.PositionAt(ring, 100, 200, minIndex: 2, maxIndex: 5));
        Assert.Equal(2, CastRingLayout.PositionAt(ring, 200, 200, minIndex: 2, maxIndex: 5));
        Assert.Equal(-1, CastRingLayout.PositionAt(ring, 400, 200, minIndex: 2, maxIndex: 5));
    }

    [Fact]
    public void AnUncastableSpellsSymbolIsNotClickableAtAll() {
        // Castability is folded INTO the hit test, so the cursor falls through the glyph rather
        // than hitting a disabled one.
        var symbols = new List<(int X, int Y, int SpellId)> { (100, 100, 7), (100, 100, 9) };

        Assert.Equal(0, CastRingLayout.SymbolAt(symbols, 100, 100, _ => true));
        Assert.Equal(1, CastRingLayout.SymbolAt(symbols, 100, 100, spell => spell == 9));
        Assert.Equal(-1, CastRingLayout.SymbolAt(symbols, 100, 100, _ => false));
    }

    [Fact]
    public void TheFirstMatchByIndexWinsRatherThanTheNearest() {
        var overlapping = new List<RingPosition> {
            new RingPosition { X = 102, Y = 100 },
            new RingPosition { X = 100, Y = 100 },
        };

        // The cursor is exactly on the second point, but the first one's box contains it too.
        Assert.Equal(0, CastRingLayout.PositionAt(overlapping, 100, 100));
    }

    [Fact]
    public void NothingUnderTheCursorAnswersMinusOne() {
        Assert.Equal(-1, CastRingLayout.PositionAt(Ring(), 0, 0));
        Assert.Equal(-1, CastRingLayout.PositionAt(null, 0, 0));
        Assert.Equal(-1, CastRingLayout.SymbolAt(null, 0, 0, _ => true));
    }

    [Fact]
    public void TheSixAnchorsAreEveryFifthPosition() {
        // The original tests (position + 1) % 5 rather than reading a flag; the extracted RING.DAT
        // flags exactly these six, which is the cross-check that the two agree.
        var anchors = Enumerable.Range(0, CastRingLayout.PositionCount)
            .Where(CastRingLayout.IsAnchor)
            .ToArray();

        Assert.Equal(new[] { 4, 9, 14, 19, 24, 29 }, anchors);
    }

    [Fact]
    public void AnAnchorDrawsTwoIconsOnFromTheBaseNotOne() {
        Assert.Equal(30, CastRingLayout.IconFor(30, position: 0, markAnchors: true));
        Assert.Equal(32, CastRingLayout.IconFor(30, position: 4, markAnchors: true));
    }

    [Fact]
    public void APassThatDoesNotMarkAnchorsDrawsThemLikeAnyOtherPosition() {
        Assert.Equal(30, CastRingLayout.IconFor(30, position: 4, markAnchors: false));
    }

    [Fact]
    public void TheChosenBandStartsAtTheSpellsMinimumNotAtZero() {
        // Minimum power 5, cursor on power 9 -> positions 4..8 inclusive.
        Assert.False(CastRingLayout.IsInChosenBand(3, minimumPower: 5, chosenPower: 9));
        Assert.True(CastRingLayout.IsInChosenBand(4, minimumPower: 5, chosenPower: 9));
        Assert.True(CastRingLayout.IsInChosenBand(8, minimumPower: 5, chosenPower: 9));
        Assert.False(CastRingLayout.IsInChosenBand(9, minimumPower: 5, chosenPower: 9));
    }

    [Fact]
    public void TheTwoSliderIconsAreDistinct() {
        // Whole ring first, chosen band overdrawn — the same icon for both would show no band.
        Assert.NotEqual(CastRingLayout.SliderRingIcon, CastRingLayout.SliderFilledIcon);
    }

    [Fact]
    public void TheSliderHasThreeIconRolesNotTwo() {
        // Ring, band, and the single hovered position — all distinct, or the cursor is invisible.
        Assert.NotEqual(CastRingLayout.SliderRingIcon, CastRingLayout.SliderHoverIcon);
        Assert.NotEqual(CastRingLayout.SliderFilledIcon, CastRingLayout.SliderHoverIcon);
    }
}
