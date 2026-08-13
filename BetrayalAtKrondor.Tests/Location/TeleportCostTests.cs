namespace BetrayalAtKrondor.Tests.Location;

using GameData.Resources.Location;
using Xunit;

/// <summary>
/// The rift-map fare (MODALSCR.C:236-242). Two things carry it: the distance is measured between
/// BUTTONS ON THE MAP PICTURE, and the approximation is octagonal rather than Euclidean.
/// </summary>
public class TeleportCostTests {
    [Fact]
    public void AStraightLineCostsItsLength() {
        Assert.Equal(80, TeleportCost.OctagonalDistance(80, 0));
        Assert.Equal(80, TeleportCost.OctagonalDistance(0, 80));
    }

    [Fact]
    public void SignDoesNotMatter() {
        Assert.Equal(TeleportCost.OctagonalDistance(40, 80),
            TeleportCost.OctagonalDistance(-40, -80));
    }

    [Fact]
    public void TheApproximationIsTheLargerAxisPlusThreeEighthsOfTheSmaller() {
        // 80 + 40*3/8 = 80 + 15 = 95, whichever way round the axes come.
        Assert.Equal(95, TeleportCost.OctagonalDistance(40, 80));
        Assert.Equal(95, TeleportCost.OctagonalDistance(80, 40));
    }

    [Fact]
    public void ADiagonalIsUnderPricedAgainstTheRealDistance() {
        // The true distance for (80, 80) is ~113; the octagon answers 110. Cheap on purpose, and
        // the reason a hypotenuse must not be substituted.
        int octagon = TeleportCost.OctagonalDistance(80, 80);

        Assert.Equal(110, octagon);
        Assert.True(octagon < 113, "the approximation should sit under the true hypotenuse");
    }

    [Fact]
    public void TheThreeEighthsTermTruncates() {
        // 7*3/8 = 2 (2.625 truncated), not 3 — integer division at each step.
        Assert.Equal(102, TeleportCost.OctagonalDistance(7, 100));
    }

    [Fact]
    public void TheFareIsTheBasePlusDistanceTimesTheRate() {
        // dx=80, dy=40 -> 95 units; 95*3 = 285, +10 base.
        Assert.Equal(295, TeleportCost.Price(100, 100, 180, 140, baseCost: 10, costPerUnit: 3));
    }

    [Fact]
    public void TravellingNowhereStillCostsTheBase() {
        Assert.Equal(10, TeleportCost.Price(100, 100, 100, 100, baseCost: 10, costPerUnit: 3));
    }

    [Fact]
    public void TheFareIsSymmetricBetweenTwoTemples() {
        long there = TeleportCost.Price(100, 100, 180, 140, 10, 3);
        long back = TeleportCost.Price(180, 140, 100, 100, 10, 3);

        Assert.Equal(there, back);
    }

    [Fact]
    public void AFreeTempleChargesNothingForDistance() {
        Assert.Equal(10, TeleportCost.Price(0, 0, 900, 900, baseCost: 10, costPerUnit: 0));
    }

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(1, 1, 11)]
    [InlineData(7, 3, 18)]      // 7 + 3*3/8 = 7+1
    [InlineData(123, 45, 149)]  // 123 + 45*3/8 = 123+16
    public void TheOriginalsTimesTenPlusFiveDivideTenRoundTripIsANoOp(
        int distanceX, int distanceY, long expected) {
        // The original writes (v*10 + 5)/10, which reads as "round to nearest" and is not: for an
        // integer v it is exactly v. Pinning it means nobody reintroduces a rounding rule that was
        // never there.
        long ours = TeleportCost.Price(0, 0, distanceX, distanceY, baseCost: 10, costPerUnit: 1);
        int distance = TeleportCost.OctagonalDistance(distanceX, distanceY);
        long theirs = ((10 + (long)distance * 1) * 10 + 5) / 10;

        Assert.Equal(expected, ours);
        Assert.Equal(theirs, ours);
    }
}
