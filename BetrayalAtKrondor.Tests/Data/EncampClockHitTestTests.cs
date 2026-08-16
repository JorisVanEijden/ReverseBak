namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;
using Xunit;

/// <summary>
/// The rest dial's hit test (<c>encamp_getClockEntryAtMouse</c> @0x70b2f). What carries: the box is
/// not centred on the dial position.
/// </summary>
public class EncampClockHitTestTests {
    // The shipped geometry, canonical: a 45x54 box anchored at 15,18.
    private static EncampData Dial(params (int X, int Y)[] hours) {
        var data = new EncampData("ENCAMP.DAT") {
            IconAnchorX = 15, IconAnchorY = 18, IconWidth = 45, IconHeight = 54,
        };
        foreach ((int x, int y) in hours) {
            data.ClockEntries.Add(new EncampPoint { X = x, Y = y });
        }

        return data;
    }

    [Fact]
    public void ThePositionItselfHits() =>
        Assert.Equal(0, Dial((355, 636)).ClockEntryAt(355, 636));

    [Fact]
    public void TheBoxReachesFurtherRightThanLeft() {
        // (45-15)/2 = 15 to the left, then a full 45 wide — so 15 left, 30 right. Centring it would
        // shift every hour's target and make the dial read as misaligned with its own artwork.
        EncampData dial = Dial((355, 636));

        Assert.Equal(0, dial.ClockEntryAt(355 - 15, 636));
        Assert.Equal(0, dial.ClockEntryAt(355 + 30, 636));
        Assert.Equal(EncampData.NoEntry, dial.ClockEntryAt(355 - 16, 636));
        Assert.Equal(EncampData.NoEntry, dial.ClockEntryAt(355 + 31, 636));
    }

    [Fact]
    public void TheBoxReachesFurtherDownThanUp() {
        // (54-18)/2 = 18 up, then a full 54 — so 18 above, 36 below.
        EncampData dial = Dial((355, 636));

        Assert.Equal(0, dial.ClockEntryAt(355, 636 - 18));
        Assert.Equal(0, dial.ClockEntryAt(355, 636 + 36));
        Assert.Equal(EncampData.NoEntry, dial.ClockEntryAt(355, 636 - 19));
        Assert.Equal(EncampData.NoEntry, dial.ClockEntryAt(355, 636 + 37));
    }

    [Fact]
    public void ACentredBoxWouldAnswerDifferently() {
        // The bug this pins. A centred 45-wide box would span 355±22, which both accepts a point
        // the original rejects and rejects one it accepts.
        EncampData dial = Dial((355, 636));

        Assert.Equal(EncampData.NoEntry, dial.ClockEntryAt(355 - 22, 636));
        Assert.Equal(0, dial.ClockEntryAt(355 + 28, 636));
    }

    [Fact]
    public void NothingUnderTheCursorIsMinusOne() =>
        Assert.Equal(EncampData.NoEntry, Dial((355, 636)).ClockEntryAt(0, 0));

    [Fact]
    public void AnEmptyDialHitsNothing() =>
        Assert.Equal(EncampData.NoEntry, Dial().ClockEntryAt(355, 636));

    [Fact]
    public void EachHourIsItsOwnIndex() {
        // Table order is the hour order — the first six shipped positions.
        EncampData dial = Dial((355, 636), (285, 624), (215, 588), (155, 546), (115, 492), (85, 432));

        Assert.Equal(0, dial.ClockEntryAt(355, 636));
        Assert.Equal(2, dial.ClockEntryAt(215, 588));
        Assert.Equal(5, dial.ClockEntryAt(85, 432));
    }

    [Fact]
    public void TheFirstMatchInTableOrderWins() {
        // Cannot happen with the shipped positions, which do not overlap — but it is the original's
        // tie-break and costs nothing to keep.
        EncampData dial = Dial((355, 636), (356, 636));

        Assert.Equal(0, dial.ClockEntryAt(356, 636));
    }
}
