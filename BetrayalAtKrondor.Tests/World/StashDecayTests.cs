namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.World;
using Xunit;

/// <summary>The whole stash-exposure decision for one container, composed.</summary>
public class StashDecayTests {
    private const uint Day = StashExposure.UnitsPerDay;
    private static readonly StashExposure.NearbyEntity[] Nothing = new StashExposure.NearbyEntity[0];

    /// <summary>A bag on the ground with a touch time, holding something.</summary>
    private static RuntimeContainer Stash(uint touchedAt, SaveGameContainerType type
            = SaveGameContainerType.Bag) {
        var rc = new RuntimeContainer {
            ContainerType = type,
            DataTypes = SaveGameContainerDataType.Timestamp,
            Timestamp = (int)touchedAt,
            X = 0,
            Y = 0,
        };
        rc.Items.Add(new RuntimeItem(1, 0, 0));
        return rc;
    }

    [Fact]
    public void AnUntouchedStashIsExemptRatherThanAncient() {
        // A zero timestamp is the ABSENCE of a touch, not a touch at the epoch. Reading it as very
        // old robs every such cache on the first check.
        RuntimeContainer never = Stash(0);
        Assert.True(StashDecay.Decide(never, Day * 100, false, Nothing, 0, false).Exempt);
    }

    [Fact]
    public void AnEmptyStashAndAProtectedOneAreBothExempt() {
        RuntimeContainer empty = Stash(Day);
        empty.Items.Clear();
        Assert.True(StashDecay.Decide(empty, Day * 5, false, Nothing, 0, false).Exempt);

        RuntimeContainer guarded = Stash(Day);
        guarded.DataTypes |= SaveGameContainerDataType.HoldsProtectedItem;
        Assert.True(StashDecay.Decide(guarded, Day * 5, false, Nothing, 0, false).Exempt);
    }

    [Fact]
    public void NothingIsPilferedMidFight() {
        Assert.True(StashDecay.Decide(Stash(Day), Day * 5, inCombat: true, Nothing, 0, false).Exempt);
    }

    [Fact]
    public void AnExemptStashSurvivesEvenTheFORCEEVENT() {
        // *** The distinction the model insists on. *** The four early returns are immune to the
        // force event; a merely zero-SCORED stash is not.
        RuntimeContainer guarded = Stash(Day);
        guarded.DataTypes |= SaveGameContainerDataType.HoldsProtectedItem;
        Assert.False(StashDecay.Decide(guarded, Day * 5, false, Nothing, 0, forceEmptyEventSet: true)
            .Empties);

        RuntimeContainer carried = Stash(Day, SaveGameContainerType.Inventory);
        StashDecay.Verdict zeroScored =
            StashDecay.Decide(carried, Day * 5, false, Nothing, 0, forceEmptyEventSet: true);
        Assert.False(zeroScored.Exempt);
        Assert.Equal(0, zeroScored.Score);
        Assert.True(zeroScored.Empties, "the force event takes even a zero-scored stash");
    }

    [Fact]
    public void ACarriedInventoryScoresZeroBecauseOfItsResidence() {
        StashDecay.Verdict v = StashDecay.Decide(
            Stash(Day, SaveGameContainerType.Inventory), Day * 9, false,
            new[] { new StashExposure.NearbyEntity(10, 100, 0) }, 0, false);
        Assert.Equal(0, v.Score);
        Assert.False(v.Empties);
    }

    [Fact]
    public void NothingIsStolenWithinTheFirstDay() {
        StashDecay.Verdict sameDay = StashDecay.Decide(Stash(Day), Day + (Day / 2), false,
            new[] { new StashExposure.NearbyEntity(10, 100, 0) }, 0, false);
        Assert.False(sameDay.Exempt);
        Assert.Equal(0, sameDay.Score);
    }

    [Fact]
    public void ABagLeftByABuildingScoresAndTheRollDecides() {
        var road = new[] { new StashExposure.NearbyEntity(10, 100, 0) };
        StashDecay.Verdict v = StashDecay.Decide(Stash(Day), Day * 3, false, road,
            roll: 0, forceEmptyEventSet: false);

        Assert.False(v.Exempt);
        Assert.True(v.Score > 0, "two days by a building is a real chance");
        Assert.True(v.Empties, "a roll of 0 is under any positive score");

        // The same stash with a roll above the score survives.
        Assert.False(StashDecay.Decide(Stash(Day), Day * 3, false, road,
            roll: (int)v.Score, forceEmptyEventSet: false).Empties);
    }

    [Fact]
    public void TheHUNDREDFLAGIsAnAbsoluteExemptionInTheCDBuild() {
        // V102CD zeroes the score outright where the floppy divides by 100. We target the CD.
        RuntimeContainer flagged = Stash(Day);
        flagged.DataTypes |= SaveGameContainerDataType.Lock;
        flagged.Params = new SaveGameContainerLockData(0, 0, puzzleChest: 1, 0);

        Assert.Equal(0, StashDecay.Decide(flagged, Day * 30, false,
            new[] { new StashExposure.NearbyEntity(10, 100, 0) }, 0, false).Score);
    }

    [Fact]
    public void ShelterBeatsExposure() {
        var road = new[] { new StashExposure.NearbyEntity(10, 100, 0) };
        var trees = new[] {
            new StashExposure.NearbyEntity(0x1e, 100, 0),
            new StashExposure.NearbyEntity(0x1e, 200, 0),
        };

        long exposed = StashDecay.Decide(Stash(Day), Day * 3, false, road, 0, false).Score;
        long hidden = StashDecay.Decide(Stash(Day), Day * 3, false, trees, 0, false).Score;
        Assert.True(exposed > hidden, $"by the road {exposed} vs among trees {hidden}");
    }
}
