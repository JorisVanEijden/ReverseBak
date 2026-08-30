namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The cover/traffic sweep over loaded scenery — <c>actor_maybeEmptyStashByExposure</c>
/// (ACTSPAWN.C:317).
/// </summary>
public class StashSweepTests {
    private const int TreeStump = 0x1e;   // cover
    private const int Building = 10;      // traffic
    private const int Unlisted = 0x33;    // neither

    private static StashExposure.NearbyEntity At(int kind, int x) =>
        new(kind, worldX: x, worldY: 0);

    [Fact]
    public void AnEmptyWorldLeavesBOTHWeightsAtONE() {
        // *** Not zero. *** A stash with no cover at all is divided by one rather than being
        // undefined, and a stash with no traffic is multiplied by one rather than scoring nothing.
        (int cover, int traffic) = StashExposure.AccumulateWeights(0, 0, new StashExposure.NearbyEntity[0]);
        Assert.Equal(1, cover);
        Assert.Equal(1, traffic);
        Assert.Equal((1, 1), StashExposure.AccumulateWeights(0, 0, null));
    }

    [Fact]
    public void CoverCountsDoubleWithinAThousandAndNothingBeyondSixThousand() {
        Assert.Equal(1 + 2, StashExposure.AccumulateWeights(0, 0, new[] { At(TreeStump, 999) }).CoverWeight);
        Assert.Equal(1 + 1, StashExposure.AccumulateWeights(0, 0, new[] { At(TreeStump, 5999) }).CoverWeight);
        Assert.Equal(1, StashExposure.AccumulateWeights(0, 0, new[] { At(TreeStump, 6000) }).CoverWeight);
    }

    [Fact]
    public void TrafficReachesFiveTimesFurtherAndCountsSixTimesAsHard() {
        // The two scales are deliberately incomparable: a building 25,000 away still doubles the
        // risk of a stash that has only one bush by it.
        Assert.Equal(1 + 12, StashExposure.AccumulateWeights(0, 0, new[] { At(Building, 14999) }).TrafficWeight);
        Assert.Equal(1 + 6, StashExposure.AccumulateWeights(0, 0, new[] { At(Building, 29999) }).TrafficWeight);
        Assert.Equal(1, StashExposure.AccumulateWeights(0, 0, new[] { At(Building, 30000) }).TrafficWeight);
    }

    [Fact]
    public void AnUnlistedShapeContributesToNeither() {
        (int cover, int traffic) = StashExposure.AccumulateWeights(0, 0, new[] { At(Unlisted, 10) });
        Assert.Equal(1, cover);
        Assert.Equal(1, traffic);
    }

    [Fact]
    public void DistanceIsTheOCTAGONALApproximation_NotAHypotenuse() {
        // A stump on the diagonal at (4300, 4300) measures 5912 octagonally — inside the 6000
        // band — where the true hypotenuse is 6081 and would fall outside it. Every band in this
        // sweep moves if the measurement is swapped.
        Assert.Equal(5912, WorldDistance.Octagonal(4300, 4300));
        Assert.Equal(1 + 1,
            StashExposure.AccumulateWeights(0, 0, new[] { new StashExposure.NearbyEntity(TreeStump, 4300, 4300) })
                .CoverWeight);
    }

    [Fact]
    public void TheWORSTCaseIsBesideARoadAndTheBESTIsShelteredAndRemote() {
        // The design the mechanic exists for, end to end.
        var exposed = new[] { At(Building, 500), At(Building, 900) };
        var sheltered = new[] { At(TreeStump, 100), At(TreeStump, 200), At(TreeStump, 300) };

        (int exposedCover, int exposedTraffic) = StashExposure.AccumulateWeights(0, 0, exposed);
        (int shelteredCover, int shelteredTraffic) = StashExposure.AccumulateWeights(0, 0, sheltered);

        long exposedScore = StashExposure.ScoreFor(false, false, 0, false, false,
            exposedTraffic, exposedCover, wholeDaysSinceTouched: 1);
        long shelteredScore = StashExposure.ScoreFor(false, false, 0, false, false,
            shelteredTraffic, shelteredCover, wholeDaysSinceTouched: 1);

        Assert.True(exposedScore > shelteredScore * 10,
            $"a cache by the road ({exposedScore}) is far worse than a sheltered one ({shelteredScore})");
    }

    [Fact]
    public void NothingIsStolenWithinTheFirstDayHoweverExposed() {
        // The integer division is the mechanic's safety catch — the whole score is multiplied by
        // whole days elapsed, which is 0 for the first 24 hours.
        (int cover, int traffic) = StashExposure.AccumulateWeights(0, 0, new[] { At(Building, 100) });
        Assert.Equal(0, StashExposure.ScoreFor(false, false, 0, false, false, traffic, cover,
            wholeDaysSinceTouched: StashExposure.WholeDaysSince(1000, 900)));
    }
}
