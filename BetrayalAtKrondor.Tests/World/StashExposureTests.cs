namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Items left in the world being found and taken.
/// </summary>
public class StashExposureTests {
    private const int NoIntensity = 0;

    private static long Score(int traffic = 1, int cover = 1, long days = 1,
        int intensity = NoIntensity, bool hundred = false, bool flagBit2 = false,
        bool eventState = false, bool partyOrCombat = false) =>
        StashExposure.ScoreFor(eventState, partyOrCombat, intensity, hundred, flagBit2,
            traffic, cover, days);

    [Fact]
    public void NothingIsStolenWithinADayOfTheLastTouch() {
        // *** THE SAFETY CATCH. *** The elapsed term is whole days in INTEGER arithmetic, so it is
        // zero for the first 24 hours and takes the score with it. A float here would start
        // stealing on the very first check.
        Assert.Equal(0, StashExposure.WholeDaysSince(StashExposure.UnitsPerDay - 1, 0));
        Assert.Equal(0, Score(days: 0));
        Assert.Equal(1, StashExposure.WholeDaysSince(StashExposure.UnitsPerDay, 0));
    }

    [Fact]
    public void ATouchInTheFutureIsNotANegativeAge() {
        // Game time can be rewound by a chapter transition; a negative age would turn into a huge
        // unsigned one and rob every cache in the world.
        Assert.Equal(0, StashExposure.WholeDaysSince(100, 5000));
    }

    [Fact]
    public void CoverDividesAndTrafficMultiplies() {
        long exposed = Score(traffic: 13, cover: 1);
        long sheltered = Score(traffic: 1, cover: 13);
        Assert.True(exposed > sheltered);
        Assert.Equal(StashExposure.BaseScore * 13, exposed);
        Assert.Equal(StashExposure.BaseScore / 13, sheltered);
    }

    [Fact]
    public void TrafficReachesFurtherAndCountsHarderThanCover() {
        // The two scales are not comparable and averaging them would flatten the mechanic.
        Assert.Equal(0, StashExposure.CoverContribution(0x1e, 6000));
        Assert.Equal(1, StashExposure.CoverContribution(0x1e, 5999));
        Assert.Equal(2, StashExposure.CoverContribution(0x1e, 999));

        Assert.Equal(0, StashExposure.TrafficContribution((int)WorldEntityType.Building, 30000));
        Assert.Equal(6, StashExposure.TrafficContribution((int)WorldEntityType.Building, 29999));
        Assert.Equal(12, StashExposure.TrafficContribution((int)WorldEntityType.Building, 14999));
    }

    [Fact]
    public void AKindIsEitherCoverOrTraffic_neverBoth() {
        for (var kind = 0; kind < 64; kind++) {
            Assert.False(StashExposure.GivesCover(kind) && StashExposure.DrawsTraffic(kind),
                $"kind {kind} counted twice");
        }
        Assert.True(StashExposure.GivesCover((int)WorldEntityType.Corn));
        Assert.True(StashExposure.GivesCover((int)WorldEntityType.TreeStump));
        Assert.True(StashExposure.DrawsTraffic((int)WorldEntityType.Well));
        Assert.True(StashExposure.DrawsTraffic((int)WorldEntityType.WayMarker));
    }

    [Fact]
    public void TheHundredFlagIsAnABSOLUTEExemptionOnTheCDBuild() {
        // *** THE BUILD DIVERGENCE, AND WE TARGET THE CD. *** The 1.00 floppy divides by 100 —
        // a hundredfold reduction that still leaves the stash robbable given enough days. If this
        // ever reads 10 instead of 0, someone has ported the floppy branch.
        Assert.Equal(0, Score(hundred: true, traffic: 100, days: 1000));
    }

    [Fact]
    public void ResidenceAndEventStateScoreZero() {
        Assert.Equal(0, Score(eventState: true));
        Assert.Equal(0, Score(partyOrCombat: true));
    }

    [Fact]
    public void IntensityAndTheFlagBitBothDivide() {
        // 6/2 + 1 = 4, so an intensity of six quarters the risk.
        Assert.Equal(StashExposure.BaseScore / 4, Score(intensity: 6));
        Assert.Equal(StashExposure.BaseScore / StashExposure.FlaggedProximityDivisor,
            Score(flagBit2: true));
    }

    [Fact]
    public void TheForceEventEmptiesEvenAZeroScoredStash() {
        // *** AN OR, NOT A GATE. *** It reaches the caches that residence and the hundred flag
        // otherwise make untouchable. Only the IsExempt cases escape it, because they return first.
        Assert.True(StashExposure.IsEmptied(score: 0, roll: 9999, forceEmptyEventSet: true));
        Assert.False(StashExposure.IsEmptied(score: 0, roll: 0, forceEmptyEventSet: false));
    }

    [Fact]
    public void TheFourEarlyReturnsAreExemptions() {
        Assert.True(StashExposure.IsExempt(hasLastTouch: false, 100, false, 3, 0));
        Assert.True(StashExposure.IsExempt(true, lastTouchTime: 0, false, 3, 0),
            "an untouched stash is not one touched at the epoch");
        Assert.True(StashExposure.IsExempt(true, 100, inCombat: true, 3, 0));
        Assert.True(StashExposure.IsExempt(true, 100, false, itemCount: 0, 0));
        Assert.True(StashExposure.IsExempt(true, 100, false, 3,
            actorFlags: StashExposure.ProtectedFlag));
        Assert.False(StashExposure.IsExempt(true, 100, false, 3, 0));
    }
}
