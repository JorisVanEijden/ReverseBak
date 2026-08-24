namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// Putting a defeated encounter back — <c>rgnenc_complete_consume</c>.
/// </summary>
public class EncounterResetTests {
    [Fact]
    public void OnlyCombAndTrapCarryAnEncounter() {
        Assert.True(EncounterReset.CarriesEncounter(TileEventType.Comb));
        Assert.True(EncounterReset.CarriesEncounter(TileEventType.Trap));
        foreach (TileEventType other in new[] {
            TileEventType.Bkgr, TileEventType.Dial, TileEventType.Town,
            TileEventType.Zone, TileEventType.Bloc, TileEventType.Disa }) {
            Assert.False(EncounterReset.CarriesEncounter(other));
        }
    }

    [Fact]
    public void TheRecordIndexIsNotTheTriggerIndex() {
        // The heart of it: records count only Comb/Trap triggers, so anything else between them
        // shifts the answer. Taking the record index as a trigger index clears the wrong hotspot.
        var triggers = new[] {
            TileEventType.Dial,   // 0
            TileEventType.Comb,   // 1  <- record 0
            TileEventType.Zone,   // 2
            TileEventType.Bloc,   // 3
            TileEventType.Trap,   // 4  <- record 1
            TileEventType.Comb,   // 5  <- record 2
        };

        Assert.Equal(1, EncounterReset.TriggerIndexForRecord(triggers, 0));
        Assert.Equal(4, EncounterReset.TriggerIndexForRecord(triggers, 1));
        Assert.Equal(5, EncounterReset.TriggerIndexForRecord(triggers, 2));
    }

    [Fact]
    public void CombAndTrapShareOneSequence() {
        // They are not counted separately — both append to the same record list.
        var triggers = new[] { TileEventType.Trap, TileEventType.Comb };

        Assert.Equal(0, EncounterReset.TriggerIndexForRecord(triggers, 0));
        Assert.Equal(1, EncounterReset.TriggerIndexForRecord(triggers, 1));
    }

    [Fact]
    public void AMissingRecordYieldsMinusOne() {
        var triggers = new[] { TileEventType.Comb };

        Assert.Equal(-1, EncounterReset.TriggerIndexForRecord(triggers, 1));
        Assert.Equal(-1, EncounterReset.TriggerIndexForRecord(triggers, -1));
        Assert.Equal(-1, EncounterReset.TriggerIndexForRecord(null, 0));
        Assert.Equal(-1, EncounterReset.TriggerIndexForRecord(new TileEventType[0], 0));
    }

    [Fact]
    public void AZoneHoldsAsManyEncountersAsTheSaveBlockRemembers() {
        // The record list stops at five and the save block reserves five records per ref-pair.
        // Asserted against the save block rather than a literal so the two cannot drift.
        Assert.Equal(EncounterActorPersistence.RecordsPerRefPair, EncounterReset.MaxRecordsPerZone);
        Assert.Equal(5, EncounterReset.MaxRecordsPerZone);
    }

    [Fact]
    public void AResetClearsThreeHotspotFlags_NotJustFought() {
        // Leaving any of these set gives an encounter that is armed again but still recorded as
        // dealt with, so it never re-fires.
        Assert.Equal(
            new[] { EncounterReset.ClearedFlag.Done, EncounterReset.ClearedFlag.ScoutTried,
                    EncounterReset.ClearedFlag.Scouted },
            EncounterReset.ClearedHotspotFlags);
    }
}
