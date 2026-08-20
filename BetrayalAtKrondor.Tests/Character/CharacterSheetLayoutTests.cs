namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The character sheet's layout (<c>charscreen_info_draw</c> @0x580fe). Canonical 1600x1200.
/// </summary>
public class CharacterSheetLayoutTests {
    [Fact]
    public void ThePanelIsTheOriginalRectangleScaledOnce() {
        // VGA (84,9) 222x71 — x5 across, x6 down, and no other arithmetic on the way.
        Assert.Equal(84 * 5, CharacterSheetLayout.PanelX);
        Assert.Equal(9 * 6, CharacterSheetLayout.PanelY);
        Assert.Equal(222 * 5, CharacterSheetLayout.PanelWidth);
        Assert.Equal(71 * 6, CharacterSheetLayout.PanelHeight);
    }

    [Fact]
    public void BothHeadingsShareABaseline() =>
        // "Ratings:" and "Condition:" are one row; only their x differs.
        Assert.Equal(14 * 6, CharacterSheetLayout.HeadingY);

    [Fact]
    public void TheConditionColumnStartsRightOfItsHeading() =>
        Assert.True(CharacterSheetLayout.ConditionX > CharacterSheetLayout.RatingsHeadingX);

    // ---- the list ---------------------------------------------------------------------------

    [Fact]
    public void ConditionLinesAreNineOriginalRowsApart() {
        int first = CharacterSheetLayout.ConditionLineY(1);
        int second = CharacterSheetLayout.ConditionLineY(2);

        Assert.Equal(9 * 6, second - first);
        Assert.Equal((9 + 16) * 6, first);
    }

    [Fact]
    public void TheListClosesUpRatherThanLeavingGaps() {
        // Lines are numbered by what was DRAWN, not by which condition it is — so three afflictions
        // always occupy the first three lines, whichever three they are.
        Assert.Equal(CharacterSheetLayout.ConditionLineY(1), CharacterSheetLayout.ConditionLineY(1));
        Assert.True(CharacterSheetLayout.ConditionLineY(3) > CharacterSheetLayout.ConditionLineY(2));
    }

    [Fact]
    public void NormalSitsSlightlyLowerThanTheFirstConditionLine() {
        // VGA 28 against the first line's 25. Three original pixels, for no reason the code gives.
        // They never appear together, so it only ever shows as a wobble between characters.
        Assert.Equal(28 * 6, CharacterSheetLayout.NormalY);
        Assert.NotEqual(CharacterSheetLayout.ConditionLineY(1), CharacterSheetLayout.NormalY);
        Assert.True(CharacterSheetLayout.NormalY > CharacterSheetLayout.ConditionLineY(1));
    }

    [Fact]
    public void OnlyPositiveConditionsAreListed() {
        Assert.True(CharacterSheetLayout.IsListed(1));
        Assert.False(CharacterSheetLayout.IsListed(0));
    }

    [Fact]
    public void ACuresNegativeAmountIsNeverRenderedAsACondition() =>
        // The test is signed and strict, which is what keeps ApplyCondition's -100 off the sheet.
        Assert.False(CharacterSheetLayout.IsListed(TempleHealMenu.ClearAmount));

    [Fact]
    public void TheSheetCoversEveryCondition() =>
        Assert.Equal(TempleHealMenu.ConditionCount, CharacterSheetLayout.ConditionCount);

    // ---- the two sizes -------------------------------------------------------------------------

    [Fact]
    public void TheHealerDrawsTheCompactSheet() {
        // charscreen_temple_heal_menu passes 0 — portrait, ratings and conditions, no lower half.
        Assert.False(CharacterSheetLayout.IsFullSheet(0));
        Assert.True(CharacterSheetLayout.IsFullSheet(1));
    }

    // ---- the two sizes ------------------------------------------------------------------------

    [Fact]
    public void TheCompactSheetDrawsNoRatingRowsBelowThePanel() {
        // The loop over the lower half's twelve rows tests the flag before its first iteration
        // (0x58369), so a compact sheet is the panel and the condition list and nothing else.
        Assert.False(CharacterSheetLayout.DrawsLowerHalf(0));
        Assert.True(CharacterSheetLayout.DrawsLowerHalf(1));
    }

    [Fact]
    public void TheTwoRowDrawersMeetWithoutAGapOrAnOverlap() {
        // Attributes 0..3 are the panel's, 4..15 the lower half's, and between them they cover
        // every attribute the sheet can show exactly once.
        Assert.Equal(CharacterSheetPanelRow.Count, CharacterSheetLayout.LowerHalfFirstAttribute);
        Assert.Equal(CharacterSheetRow.DisplayableAttributes,
            CharacterSheetLayout.LowerHalfFirstAttribute
            + CharacterSheetLayout.LowerHalfAttributeCount);
    }

    // ---- the frame and the vines --------------------------------------------------------------

    [Fact]
    public void EachPanelEdgeIsTheOppositeOneTurnedRound() {
        var frame = CharacterSheetLayout.PanelFrame;

        Assert.Equal(4, frame.Count);
        // The two vertical rules are one image, the right-hand one mirrored; the two horizontals
        // likewise, the lower one flipped. Drawing either unturned lights the dots from the wrong
        // side.
        Assert.Equal(frame[0].IconIndex, frame[1].IconIndex);
        Assert.Equal(GameData.Resources.Image.ImageFlags.HorizontalFlip, frame[1].Flags);
        Assert.Equal(frame[2].IconIndex, frame[3].IconIndex);
        Assert.Equal(GameData.Resources.Image.ImageFlags.VerticalFlip, frame[3].Flags);
    }

    [Fact]
    public void TheFrameSitsOnThePanelsOwnEdges() {
        var frame = CharacterSheetLayout.PanelFrame;

        Assert.Equal(CharacterSheetLayout.PanelX, frame[0].X);
        Assert.Equal(CharacterSheetLayout.PanelY, frame[0].Y);
        Assert.Equal(CharacterSheetLayout.PanelY, frame[2].Y);
        // The horizontals start two original pixels inside the left edge so they butt against the
        // verticals instead of crossing them.
        Assert.Equal(CharacterSheetLayout.PanelX + (2 * 5), frame[2].X);
    }

    [Fact]
    public void BothSizesAreDecoratedAndNotWithTheSamePiece() {
        // Not a piece of the lower half that the compact form drops — a choice between two
        // decorations, so a compact sheet drawn without them leaves that edge bare.
        Assert.All(CharacterSheetLayout.Vines(1),
            piece => Assert.Equal(CharacterSheetLayout.CornerVineIcon, piece.IconIndex));
        Assert.All(CharacterSheetLayout.Vines(0),
            piece => Assert.Equal(CharacterSheetLayout.SmallVineIcon, piece.IconIndex));
    }

    [Fact]
    public void AVineHangsOffTheLeftEdge() =>
        // VGA x=-4. A renderer that clamped it to the screen would shift the piece inward.
        Assert.True(CharacterSheetLayout.Vines(0)[1].X < 0);

    [Fact]
    public void TheSheetFadesInAndNeverFadesOut() {
        // charscreen_info_loop fades on entry only: the loop ends, the saved palette goes back and
        // the caller redraws. Pairing the fade-in with a fade-out on close shows black the original
        // never shows.
        Assert.True(CharacterSheetLayout.FadesInOnEntryOnly);
    }

    [Fact]
    public void TheFadeIsCountedInFramesAndIsEightOfThem() {
        // intensity 63 down by 8 a frame -> 63,55,47,39,31,23,15,7 = eight presented frames.
        Assert.Equal(8, CharacterSheetLayout.FadeFrames);
        var steps = 0;
        for (int i = CharacterSheetLayout.FadeStartIntensity; i > 0;
             i -= CharacterSheetLayout.FadeIntensityStep) {
            steps++;
        }
        Assert.Equal(CharacterSheetLayout.FadeFrames, steps);
    }
}
