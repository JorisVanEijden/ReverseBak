namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using Xunit;

/// <summary>
/// The rest dial's stones (<c>encamp_run</c> @0x703d0, around 0x706d9).
/// </summary>
public class EncampDialTests {
    [Fact]
    public void AStoneIsAnHourWithNoOffset() {
        // The original multiplies the hit-test index straight by an hour. The artwork starts its
        // run at the lower right, which invites a rotation that is not there.
        Assert.Equal(0, EncampDial.HourFor(0));
        Assert.Equal(23, EncampDial.HourFor(EncampDial.Stones - 1));
    }

    [Fact]
    public void TheTargetIsAWholeNumberOfHours() {
        Assert.Equal(0, EncampDial.TargetTicksFor(0));
        Assert.Equal(EncampDial.TicksPerHour, EncampDial.TargetTicksFor(1));
        Assert.Equal(23 * EncampDial.TicksPerHour, EncampDial.TargetTicksFor(23));
    }

    [Fact]
    public void AnHourIsTheOriginalsSevenHundredAndEight() =>
        // 0x708 two-second units. Pinned because every rest target is built from it.
        Assert.Equal(0x708, EncampDial.TicksPerHour);

    [Fact]
    public void PressingAndReleasingTheSameStoneChoosesIt() =>
        Assert.True(EncampDial.Commits(pressedStone: 7, releasedStone: 7));

    [Fact]
    public void SlidingOffAStoneAbandonsTheChoice() =>
        // The ordinary behaviour of a button, applied to hotspots that are not buttons.
        Assert.False(EncampDial.Commits(pressedStone: 7, releasedStone: 8));

    [Fact]
    public void ReleasingOverNothingCommitsNothing() =>
        Assert.False(EncampDial.Commits(pressedStone: 7, releasedStone: EncampData.NoEntry));

    [Fact]
    public void AReleaseWithNoPressBehindItCommitsNothing() =>
        // Both are -1 when nothing is latched and nothing is under the cursor; equal, but not a
        // choice. Guarding only on equality would start a rest from a stray click on the backdrop.
        Assert.False(EncampDial.Commits(EncampData.NoEntry, EncampData.NoEntry));

    // ---- stone icons (sub_ovr182_67A @0x70a4a) ------------------------------------------------

    [Fact]
    public void AnOrdinaryStoneIsDarkBlue() =>
        // The gold stones painted into ENCAMP.SCX are backdrop the game covers over; drawing
        // nothing leaves the artwork's gold showing, which is the bug this fixes.
        Assert.Equal(EncampDial.StoneIconPlain, EncampDial.IconFor(stone: 5, markedHour: 9));

    [Fact]
    public void TheCurrentHoursStoneIsRed() =>
        Assert.Equal(EncampDial.StoneIconMarked, EncampDial.IconFor(stone: 9, markedHour: 9));

    [Fact]
    public void TheHoveredStoneIsGold() =>
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 5, markedHour: 9, highlightedStone: 5));

    [Fact]
    public void HoverBeatsTheCurrentHour() =>
        // The original tests hover BEFORE "now", so pointing at the current hour turns it gold.
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 9, markedHour: 9, highlightedStone: 9));

    [Fact]
    public void NoHoverIsNotStoneZero() =>
        // -1 means nothing hovered; reading it as a stone index would light up midnight.
        Assert.Equal(EncampDial.StoneIconMarked,
            EncampDial.IconFor(stone: 0, markedHour: 0, highlightedStone: -1));

    [Fact]
    public void EveryStoneGetsAnIcon() {
        for (var stone = 0; stone < EncampDial.Stones; stone++) {
            int icon = EncampDial.IconFor(stone, markedHour: 13);
            Assert.True(icon == EncampDial.StoneIconPlain || icon == EncampDial.StoneIconMarked);
        }
    }

    // ---- the marked range ---------------------------------------------------------------------

    [Fact]
    public void ARangeMarksEveryHourInIt() {
        // How a camp rest shows its progress: the arc grows from the hour it began to the hour the
        // clock has reached.
        for (var stone = 0; stone < EncampDial.Stones; stone++) {
            Assert.Equal(stone is >= 9 and <= 13,
                EncampDial.InSpan(stone, spanStartHour: 9, spanEndHour: 13));
        }
    }

    [Fact]
    public void ARangeWRAPSPASTMIDNIGHT() {
        // The only way an overnight rest can be drawn at all. 22:00 -> 03:00 must mark 22,23,0,1,2,3
        // and nothing between 4 and 21.
        foreach (int stone in new[] { 22, 23, 0, 1, 2, 3 }) {
            Assert.True(EncampDial.InSpan(stone, 22, 3), $"{stone} should be in the night");
        }
        foreach (int stone in new[] { 4, 12, 21 }) {
            Assert.False(EncampDial.InSpan(stone, 22, 3), $"{stone} should not be");
        }
    }

    [Fact]
    public void ARangeOfOneHourDoesNotPaintTheWholeRing() {
        // Without the equal-ends guard the wrapping test reads as "at or after N or at or before N",
        // which is every stone. The original carries a separate flag for exactly this.
        for (var stone = 0; stone < EncampDial.Stones; stone++) {
            Assert.Equal(stone == 7, EncampDial.InSpan(stone, 7, 7));
        }
    }

    [Fact]
    public void NoRangeMarksNothing() =>
        Assert.False(EncampDial.InSpan(5, -1, -1));

    [Fact]
    public void TheINNSGoldStoneIsItsWakingHour() {
        // The inn passes its waking hour where the camp screen passes the cursor, so the gold stone
        // there is a fixed promise rather than a hover. Both go through the same argument.
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 5, markedHour: 22, highlightedStone: 5));
        Assert.Equal(EncampDial.StoneIconMarked,
            EncampDial.IconFor(stone: 22, markedHour: 22, highlightedStone: 5));
        Assert.Equal(EncampDial.StoneIconPlain,
            EncampDial.IconFor(stone: 10, markedHour: 22, highlightedStone: 5));
    }

    [Fact]
    public void GoldBeatsRedWhenTheyLandOnTheSameStone() =>
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 9, markedHour: 9, highlightedStone: 9,
                spanStartHour: 8, spanEndHour: 10));

    // ---- the sundial's shadow -----------------------------------------------------------------

    [Fact]
    public void ThereIsNoShadowAtNIGHT() {
        Assert.Equal(-1, EncampDial.ShadowArcPointFor(0));
        Assert.Equal(-1, EncampDial.ShadowArcPointFor(5 * EncampDial.TicksPerHour));
        Assert.Equal(-1, EncampDial.ShadowArcPointFor(19 * EncampDial.TicksPerHour));
        Assert.Equal(-1, EncampDial.ShadowArcPointFor(23 * EncampDial.TicksPerHour));
    }

    [Fact]
    public void ThereIsNoShadowAtNOONEither() =>
        // It would be the degenerate line straight down the gnomon.
        Assert.Equal(-1, EncampDial.ShadowArcPointFor(12 * EncampDial.TicksPerHour));

    [Fact]
    public void TheShadowSweepsTheArcThroughTheDay() {
        Assert.Equal(EncampDial.ShadowArcFirstEntry,
            EncampDial.ShadowArcPointFor(6 * EncampDial.TicksPerHour));   // dawn: the first point
        Assert.Equal(EncampDial.ShadowArcFirstEntry + 23,
            EncampDial.ShadowArcPointFor(18 * EncampDial.TicksPerHour));  // dusk: the last
    }

    [Fact]
    public void NoonIsSKIPPEDRatherThanRepeated() {
        // Every half-hour past midday shifts down by one, which is what makes 24 points cover a
        // sweep that otherwise needs 25.
        int before = EncampDial.ShadowArcPointFor((int)(11.5 * EncampDial.TicksPerHour));
        int after = EncampDial.ShadowArcPointFor((int)(12.5 * EncampDial.TicksPerHour));

        Assert.Equal(before + 1, after);
    }

    [Fact]
    public void EveryDaylightHourPicksAPointInsideTheArc() {
        for (int hour = EncampDial.ShadowFirstHour; hour <= EncampDial.ShadowLastHour; hour++) {
            int point = EncampDial.ShadowArcPointFor(hour * EncampDial.TicksPerHour);
            if (point < 0) {
                continue;   // noon
            }
            Assert.InRange(point, EncampDial.ShadowArcFirstEntry, EncampDial.ShadowArcFirstEntry + 23);
        }
    }
}
