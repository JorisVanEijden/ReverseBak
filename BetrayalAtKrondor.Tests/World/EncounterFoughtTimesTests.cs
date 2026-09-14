namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

public class EncounterFoughtTimesTests {
    [Fact]
    public void TheTableSitsTwoTablesBeforeTheObjectStates() {
        Assert.Equal(0x3967, EncounterFoughtTimes.BodyOffset);
    }

    [Fact]
    public void ANeverFoughtEncounterGivesNothing() {
        Assert.Equal(0, EncounterFoughtTimes.RecoveryDelta(500000, 0));
    }

    [Fact]
    public void EachWholeHourIsOnePointInEightEight() {
        Assert.Equal(14L << 8, EncounterFoughtTimes.RecoveryDelta(120000 + 14 * 0x708 + 5, 120000));
    }

    [Fact]
    public void TheSixteenBitShiftTurnsLongAbsencesIntoADrainAndBack() {
        // CBENC.C:82 shifts a 16-bit int before widening it.
        Assert.Equal(127L << 8, EncounterFoughtTimes.RecoveryDelta(1 + 127 * 0x708, 1));
        Assert.Equal(-32768L, EncounterFoughtTimes.RecoveryDelta(1 + 128 * 0x708, 1));
        Assert.Equal(-14336L, EncounterFoughtTimes.RecoveryDelta(1 + 200 * 0x708, 1));
        Assert.Equal(0L, EncounterFoughtTimes.RecoveryDelta(1 + 256 * 0x708, 1));
    }

    [Fact]
    public void AStampSurvivesASaveAndALoad() {
        var times = new EncounterFoughtTimes();
        times.Stamp(42, 123456);
        var body = new byte[EncounterObjectStates.BodyOffset];
        Assert.True(times.Save(body));

        var reloaded = new EncounterFoughtTimes();
        reloaded.Load(body);
        Assert.Equal(123456u, reloaded.FoughtAt(42));
        Assert.Equal(0u, reloaded.FoughtAt(41));
    }
}
