namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.GameState;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// The time-of-day daylight term. The ramps joining the two plateaus are where a port goes wrong —
/// the original rebases them by 16-bit overflow, which does not survive being widened.
/// </summary>
public class DaylightLevelTests {
    private static long At(int hour, int minute = 0) =>
        (hour * GameTime.UnitsPerHour) + ((minute * GameTime.UnitsPerHour) / 60);

    [Fact]
    public void TheMiddleOfTheDayIsFlatAndBright() {
        Assert.Equal(DaylightLevel.Day, DaylightLevel.At(At(8)));
        Assert.Equal(DaylightLevel.Day, DaylightLevel.At(At(12)));
        Assert.Equal(DaylightLevel.Day, DaylightLevel.At(At(16, 59)));
    }

    [Fact]
    public void TheNightIsFlatAndNeverFullyDark() {
        // The floor is 15, not 0 — the world is always a little visible.
        Assert.Equal(DaylightLevel.Night, DaylightLevel.At(At(20)));
        Assert.Equal(DaylightLevel.Night, DaylightLevel.At(At(0)));
        Assert.Equal(DaylightLevel.Night, DaylightLevel.At(At(3, 59)));
        Assert.True(DaylightLevel.Night > 0);
    }

    [Fact]
    public void BothRampsLandExactlyOnThePlateausTheyJoin() {
        // Continuity is the point: a port that lands a step off shows a visible jump twice a day.
        Assert.Equal(DaylightLevel.Night, DaylightLevel.At(At(4)));
        Assert.Equal(DaylightLevel.Day, DaylightLevel.At(At(8)));
        Assert.Equal(DaylightLevel.Day, DaylightLevel.At(At(17)));
        Assert.Equal(DaylightLevel.Night, DaylightLevel.At(At(20)));
    }

    [Fact]
    public void DawnClimbsAndDuskFalls() {
        Assert.True(DaylightLevel.At(At(5)) > DaylightLevel.At(At(4)));
        Assert.True(DaylightLevel.At(At(7)) > DaylightLevel.At(At(5)));

        Assert.True(DaylightLevel.At(At(18)) < DaylightLevel.At(At(17)));
        Assert.True(DaylightLevel.At(At(19)) < DaylightLevel.At(At(18)));
    }

    [Fact]
    public void TheRampsAreNotTheSameLength() {
        // Dawn takes four hours and dusk three, so dusk falls faster than dawn climbs.
        Assert.Equal(4 * GameTime.UnitsPerHour, DaylightLevel.DawnUnits);
        Assert.Equal(3 * GameTime.UnitsPerHour, DaylightLevel.DuskUnits);
    }

    [Fact]
    public void DuskFallsFasterThanDawnClimbs() {
        int dawnAfterAnHour = DaylightLevel.At(At(5)) - DaylightLevel.Night;
        int duskAfterAnHour = DaylightLevel.Day - DaylightLevel.At(At(18));

        Assert.True(duskAfterAnHour > dawnAfterAnHour);
    }

    [Fact]
    public void TheMidpointOfEachRampIsHalfway() {
        Assert.Equal(DaylightLevel.Night + (DaylightLevel.Swing / 2), DaylightLevel.At(At(6)));
        Assert.Equal(DaylightLevel.Day - (DaylightLevel.Swing / 2), DaylightLevel.At(At(18, 30)));
    }

    [Fact]
    public void TheLevelNeverLeavesItsBand() {
        for (var unit = 0; unit < GameTime.UnitsPerDay; unit += 37) {
            Assert.InRange(DaylightLevel.At(unit), DaylightLevel.Night, DaylightLevel.Day);
        }
    }

    [Fact]
    public void ItRepeatsEveryDay() {
        for (var hour = 0; hour < 24; hour++) {
            Assert.Equal(DaylightLevel.At(At(hour)),
                DaylightLevel.At(At(hour) + (GameTime.UnitsPerDay * 9)));
        }
    }

    [Fact]
    public void TheSwingIsTheWholeDistanceBetweenThePlateaus() {
        Assert.Equal(49, DaylightLevel.Swing);
        Assert.Equal(DaylightLevel.Day - DaylightLevel.Night, DaylightLevel.Swing);
    }

    [Fact]
    public void FullDaylightAndFullNightAnswerForTheirPlateaus() {
        Assert.True(DaylightLevel.IsFullDaylight(At(12)));
        Assert.False(DaylightLevel.IsFullDaylight(At(6)));

        Assert.True(DaylightLevel.IsFullNight(At(2)));
        Assert.False(DaylightLevel.IsFullNight(At(18)));
    }
}
