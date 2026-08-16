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
        Assert.Equal(EncampDial.StoneIconPlain, EncampDial.IconFor(stone: 5, currentHour: 9));

    [Fact]
    public void TheCurrentHoursStoneIsRed() =>
        Assert.Equal(EncampDial.StoneIconMarked, EncampDial.IconFor(stone: 9, currentHour: 9));

    [Fact]
    public void TheHoveredStoneIsGold() =>
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 5, currentHour: 9, hoveredStone: 5));

    [Fact]
    public void HoverBeatsTheCurrentHour() =>
        // The original tests hover BEFORE "now", so pointing at the current hour turns it gold.
        Assert.Equal(EncampDial.StoneIconHovered,
            EncampDial.IconFor(stone: 9, currentHour: 9, hoveredStone: 9));

    [Fact]
    public void NoHoverIsNotStoneZero() =>
        // -1 means nothing hovered; reading it as a stone index would light up midnight.
        Assert.Equal(EncampDial.StoneIconMarked,
            EncampDial.IconFor(stone: 0, currentHour: 0, hoveredStone: -1));

    [Fact]
    public void EveryStoneGetsAnIcon() {
        for (var stone = 0; stone < EncampDial.Stones; stone++) {
            int icon = EncampDial.IconFor(stone, currentHour: 13);
            Assert.True(icon == EncampDial.StoneIconPlain || icon == EncampDial.StoneIconMarked);
        }
    }
}
