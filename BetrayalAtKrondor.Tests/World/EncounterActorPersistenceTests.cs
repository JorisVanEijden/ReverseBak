namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The saved state that makes a killed roaming group stay killed. The indexing and the three state
/// values are the whole of it.
/// </summary>
public class EncounterActorPersistenceTests {
    [Fact]
    public void ARefPairHoldsFiveRecordsOfSevenActors() {
        Assert.Equal(7, EncounterActorPersistence.SlotsPerRecord);
        Assert.Equal(5, EncounterActorPersistence.RecordsPerRefPair);
        Assert.Equal(EncounterActorPersistence.SlotsPerRefPair,
            EncounterActorPersistence.RecordsPerRefPair * EncounterActorPersistence.SlotsPerRecord);
    }

    [Fact]
    public void TheFirstSlotOfTheFirstPairIsIndexZero() {
        Assert.Equal(0, EncounterActorPersistence.StateIndex(0, 0, 0));
    }

    [Fact]
    public void SlotsWithinARecordAreConsecutive() {
        Assert.Equal(3, EncounterActorPersistence.StateIndex(0, 0, 3));
    }

    [Fact]
    public void RecordsAreSevenApart() {
        Assert.Equal(7, EncounterActorPersistence.StateIndex(0, 1, 0));
        Assert.Equal(28, EncounterActorPersistence.StateIndex(0, 4, 0));
    }

    [Fact]
    public void RefPairsAreThirtyFiveApart() {
        Assert.Equal(35, EncounterActorPersistence.StateIndex(1, 0, 0));
        Assert.Equal(70, EncounterActorPersistence.StateIndex(2, 0, 0));
    }

    [Fact]
    public void EveryAddressableSlotIsDistinctAndInsideTheBlock() {
        var seen = new System.Collections.Generic.HashSet<int>();
        for (var pair = 0; pair < EncounterActorPersistence.RefPairs; pair++) {
            for (var record = 0; record < EncounterActorPersistence.RecordsPerRefPair; record++) {
                for (var slot = 0; slot < EncounterActorPersistence.SlotsPerRecord; slot++) {
                    int index = EncounterActorPersistence.StateIndex(pair, record, slot);
                    Assert.True(seen.Add(index), $"index {index} collides");
                    Assert.InRange(index, 0,
                        EncounterActorPersistence.RefPairs * EncounterActorPersistence.SlotsPerRefPair - 1);
                }
            }
        }
        Assert.Equal(EncounterActorPersistence.RefPairs * EncounterActorPersistence.SlotsPerRefPair,
            seen.Count);
    }

    [Fact]
    public void SlotZeroInitialisesDifferentlyFromTheRest() {
        // The original writes 0 for slot 0 and 0x100 for the other 34. Reading state 0 as "removed"
        // would conflate two values the game keeps apart.
        Assert.Equal(EncounterActorPersistence.Untouched,
            EncounterActorPersistence.InitialState(0));
        Assert.Equal(EncounterActorPersistence.Removed, EncounterActorPersistence.InitialState(1));
        Assert.Equal(EncounterActorPersistence.Removed, EncounterActorPersistence.InitialState(34));
    }

    [Fact]
    public void TheThreeStatesAreDistinct() {
        Assert.NotEqual(EncounterActorPersistence.Untouched, EncounterActorPersistence.Removed);
        Assert.NotEqual(EncounterActorPersistence.Removed, EncounterActorPersistence.Placed);
        Assert.NotEqual(EncounterActorPersistence.Untouched, EncounterActorPersistence.Placed);
    }

    [Fact]
    public void OnlyUndergroundKeepsTheStoredPoseOnPlacement() {
        // A roaming actor in a dungeon resumes where it was left; outdoors it is repositioned by
        // whatever placed it.
        Assert.True(EncounterActorPersistence.KeepsStoredPose(2));
        Assert.False(EncounterActorPersistence.KeepsStoredPose(1));
        Assert.False(EncounterActorPersistence.KeepsStoredPose(0));
    }
}
