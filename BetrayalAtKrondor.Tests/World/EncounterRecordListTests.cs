namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The chunk's encounter record list — <c>g_anEncounterRecordIds</c>, and the lookup every write to
/// the encounter-state block starts with.
/// </summary>
public class EncounterRecordListTests {
    private static (TileEventType, long?) Comb(long id) => (TileEventType.Comb, id);
    private static (TileEventType, long?) Trap(long id) => (TileEventType.Trap, id);
    private static (TileEventType, long?) Other(TileEventType t) => (t, (long?)null);

    [Fact]
    public void OnlyCombAndTrapTriggersTakeASlot() {
        // The original's kind test is `wKind == 1 || wKind == 7`; a Zone or Dial trigger between
        // two encounters must not push the second one along a slot.
        List<long> ids = EncounterReset.RecordIds(new[] {
            Other(TileEventType.Zone), Comb(40), Other(TileEventType.Dial), Trap(41),
        });

        Assert.Equal(new long[] { 40, 41 }, ids);
    }

    [Fact]
    public void TheListStopsAtFive_andASixthEncounterHasNoSlotAtAll() {
        // *** NOT A FORMALITY. *** rgnenc_load_encounter_actors appends only while the count is
        // below five. A port that let the list grow would compute a slot inside the NEXT ref pair's
        // block and stamp state onto a different zone's encounters.
        var triggers = new List<(TileEventType, long?)>();
        for (var i = 0; i < 7; i++) {
            triggers.Add(Comb(100 + i));
        }

        List<long> ids = EncounterReset.RecordIds(triggers);

        Assert.Equal(EncounterReset.MaxRecordsPerZone, ids.Count);
        Assert.Equal(-1, EncounterReset.RecordIndexOf(triggers, 105));
        Assert.Equal(4, EncounterReset.RecordIndexOf(triggers, 104));
    }

    [Fact]
    public void AnEncounterThatIsNotInTheListReportsNoSlot() {
        // Both persist routines scan for the id and RETURN WITHOUT WRITING when it is absent, so
        // "we do not know where this goes" is an ordinary outcome rather than an error.
        Assert.Equal(-1, EncounterReset.RecordIndexOf(new[] { Comb(40) }, 41));
        Assert.Equal(-1, EncounterReset.RecordIndexOf(null, 40));
    }

    [Fact]
    public void TwoTriggersOnOneEncounterShareARecord() {
        // Two ways into the same group of actors; the scan takes the first match.
        Assert.Equal(0, EncounterReset.RecordIndexOf(new[] { Comb(40), Comb(40) }, 40));
    }

    [Fact]
    public void RecordIndexIsTheEncountersPosition_notItsNumber() {
        // The encounter number indexes the 700 enemy-party records; the record index addresses one
        // of five slots. Reading one as the other writes far outside the block.
        int record = EncounterReset.RecordIndexOf(new[] { Comb(612), Comb(37) }, 37);

        Assert.Equal(1, record);
        Assert.True(record < EncounterActorPersistence.RecordsPerRefPair);
    }

    [Fact]
    public void AnAddressIsKnownOnlyInsideTheBlock() {
        Assert.True(new EncounterActorPersistence.RecordAddress(0, 0).IsKnown);
        Assert.True(new EncounterActorPersistence.RecordAddress(
            EncounterActorPersistence.RefPairs - 1,
            EncounterActorPersistence.RecordsPerRefPair - 1).IsKnown);

        Assert.False(EncounterActorPersistence.RecordAddress.None.IsKnown);
        Assert.False(new EncounterActorPersistence.RecordAddress(
            EncounterActorPersistence.RefPairs, 0).IsKnown);
        // An encounter NUMBER used as a record index: the mistake the type exists to make visible.
        Assert.False(new EncounterActorPersistence.RecordAddress(3, 612).IsKnown);
    }
}
