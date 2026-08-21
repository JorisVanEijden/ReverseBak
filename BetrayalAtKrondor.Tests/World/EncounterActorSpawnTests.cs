namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Spawning a zone's roaming actors, and the one field that means two things.
/// </summary>
public class EncounterActorSpawnTests {
    [Fact]
    public void TheKindAndThePersistedStateAreTheSameNumber() {
        // *** The thing to understand before anything else here. *** The renderer's "kind" and the
        // save code's "state" read the same high byte, so the two vocabularies line up exactly.
        Assert.Equal(EncounterActorSpawn.Gone, EncounterActorPersistence.Removed);
        Assert.Equal(EncounterActorSpawn.Standing, EncounterActorPersistence.Placed);
        Assert.Equal(EncounterActorSpawn.Unseeded, EncounterActorPersistence.Untouched);

        Assert.Equal(3, EncounterActorSpawn.KindOf(EncounterActorSpawn.Roaming));
        Assert.Equal(4, EncounterActorSpawn.KindOf(EncounterActorSpawn.Standing));
    }

    [Fact]
    public void TheLowBitsRideAlongAndDoNotDisturbTheKind() {
        // A roaming actor carries its walk frame and direction in the same word, so anything reading
        // the kind must mask rather than compare the whole value.
        int walking = EncounterActorSpawn.FreshlyPlacedState(frameRoll: 2, directionRoll: 1);

        Assert.Equal(3, EncounterActorSpawn.KindOf(walking));
        Assert.NotEqual(EncounterActorSpawn.Roaming, walking);
    }

    [Fact]
    public void SeedingHappensOnceAndIsReadOffTheFirstSlot() {
        Assert.True(EncounterActorSpawn.NeedsSeeding(EncounterActorSpawn.Unseeded));
        Assert.False(EncounterActorSpawn.NeedsSeeding(EncounterActorSpawn.Gone));
        Assert.False(EncounterActorSpawn.NeedsSeeding(EncounterActorSpawn.Standing));
    }

    [Fact]
    public void ASlotSeedsPendingONLYForALivingRosterMember() {
        // The pass reads each named combatant's own flags, so a group already wiped out never comes
        // back — and it is the combatant table that remembers, not the encounter record.
        Assert.True(EncounterActorSpawn.SeedsAsPending(rosterSlot: 4, combatantIsDead: false));
        Assert.False(EncounterActorSpawn.SeedsAsPending(rosterSlot: 4, combatantIsDead: true));
        Assert.False(EncounterActorSpawn.SeedsAsPending(rosterSlot: -1, combatantIsDead: false));
    }

    [Fact]
    public void OnlyAPendingActorTakesItsPositionFromTheTemplate() {
        // An actor that has been placed before resumes from its stored pose; that is what lets a
        // dungeon roamer pick up where it was left.
        Assert.True(EncounterActorSpawn.PlacesFromTemplate(EncounterActorSpawn.Pending));
        Assert.False(EncounterActorSpawn.PlacesFromTemplate(EncounterActorSpawn.Roaming));
        Assert.False(EncounterActorSpawn.PlacesFromTemplate(EncounterActorSpawn.Standing));
    }

    [Fact]
    public void GoneAndUnseededActorsAreNeverPlaced() {
        Assert.False(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Gone, standingOnly: false));
        Assert.False(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Unseeded, standingOnly: false));
    }

    [Fact]
    public void TheRecordFlagCanRestrictAZoneToStandingActorsOnly() {
        // Flag bit 0 set: a roaming group authored on such a record simply does not appear.
        Assert.True(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Roaming, standingOnly: false));
        Assert.False(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Roaming, standingOnly: true));
        Assert.True(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Standing, standingOnly: true));
        Assert.False(EncounterActorSpawn.IsPlaced(EncounterActorSpawn.Pending, standingOnly: true));
    }

    [Fact]
    public void EveryFreshActorStartsMidStrideAndTheyDoNotAllMatch() {
        // Placing a group on frame 0 all walking the same way makes them move in lockstep, which is
        // the tell of a ported spawn. The rolls are what break that up.
        int a = EncounterActorSpawn.FreshlyPlacedState(0, 0);
        int b = EncounterActorSpawn.FreshlyPlacedState(2, 1);

        Assert.NotEqual(a, b);
        Assert.Equal(0, a & EncounterActorSpawn.WalkDirectionBit);
        Assert.Equal(EncounterActorSpawn.WalkDirectionBit, b & EncounterActorSpawn.WalkDirectionBit);
        Assert.Equal(3, EncounterActorSpawn.KindOf(a));
        Assert.Equal(3, EncounterActorSpawn.KindOf(b));
    }

    [Fact]
    public void EveryFreshFrameIsWithinTheCycle() {
        for (var roll = 0; roll < EncounterActorSpawn.WalkFrameCount; roll++) {
            int state = EncounterActorSpawn.FreshlyPlacedState(roll, 0);
            Assert.InRange(state & 3, 0, EncounterActorSpawn.WalkFrameCount - 1);
        }
    }

    [Fact]
    public void SAVINGAWandererStopsItPermanently() {
        // *** Looks like a bug, and is what the game does. *** persist_actor_placed writes 0x400
        // whatever the actor was; nothing promotes standing back to roaming, and the movement updater
        // ignores every kind but roaming. So a saved wanderer comes back stopped and stays stopped.
        Assert.Equal(EncounterActorSpawn.Standing, EncounterActorSpawn.StateAfterPersisting);
        Assert.NotEqual(EncounterActorSpawn.Roaming, EncounterActorSpawn.StateAfterPersisting);

        // And a standing actor still places — it is stopped, not absent.
        Assert.True(EncounterActorSpawn.IsPlaced(
            EncounterActorSpawn.StateAfterPersisting, standingOnly: false));
        Assert.False(EncounterActorSpawn.PlacesFromTemplate(EncounterActorSpawn.StateAfterPersisting));
    }

    [Fact]
    public void TheActorCountComesFromTheFirstSlotAndIsCappedAtSeven() {
        Assert.Equal(3, EncounterActorSpawn.ActorCount(3));
        Assert.Equal(7, EncounterActorSpawn.ActorCount(7));
        // A corrupt byte must not walk off the end of a seven-entry roster.
        Assert.Equal(7, EncounterActorSpawn.ActorCount(200));
        Assert.Equal(0, EncounterActorSpawn.ActorCount(-1));
    }

    [Fact]
    public void TheBlockLayoutAgreesWithThePersistenceIndexing() {
        // Same 5 x 7 block the save file uses; disagreeing would put a spawned actor's state on top
        // of another slot's.
        Assert.Equal(EncounterActorSpawn.MaxPlacedObjects,
            EncounterActorSpawn.MaxRecords * EncounterActorSpawn.SlotsPerRecord);
        Assert.Equal(EncounterActorPersistence.StateIndex(0, 2, 3),
            EncounterActorSpawn.StateSlot(2, 3));
    }

    [Fact]
    public void WaypointsAreTileRelativeJustLikeTheSpawnPoint() {
        // Converting the spawn but forgetting the waypoints gives actors that walk off toward the
        // corner of the map.
        Assert.Equal(64000L * 3 + 250, EncounterActorSpawn.ToWorld(3, 250));
        Assert.Equal(250L, EncounterActorSpawn.ToWorld(0, 250));
    }
}
