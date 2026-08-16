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
}
