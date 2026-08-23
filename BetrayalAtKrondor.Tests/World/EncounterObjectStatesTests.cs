namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// The per-encounter-actor state block — the save state behind "a killed roster actor stays gone".
/// </summary>
public class EncounterObjectStatesTests {
    [Fact]
    public void TheIndexMatchesTheOriginalsStride() {
        // refPair * 0x23 + recordIndex * 7 + slotIndex, spelled out rather than restated: 0x23 is
        // 35 and it is 5 records x 7 slots, not a free constant.
        Assert.Equal(0x23, EncounterObjectStates.EntriesPerRefPair);
        Assert.Equal(0, EncounterObjectStates.IndexOf(0, 0, 0));
        Assert.Equal(0x23, EncounterObjectStates.IndexOf(1, 0, 0));
        Assert.Equal(0x23 + 7, EncounterObjectStates.IndexOf(1, 1, 0));
        Assert.Equal(0x23 + 7 + 3, EncounterObjectStates.IndexOf(1, 1, 3));
        Assert.Equal(EncounterObjectStates.EntryCount - 1,
            EncounterObjectStates.IndexOf(39, 4, 6));
    }

    [Fact]
    public void MarkRemovedWritesTheKindAndZeroesThePose() {
        var states = new EncounterObjectStates();
        states.MarkRemoved(refPair: 2, recordIndex: 1, slotIndex: 4);

        EncounterObjectStates.Entry e = states[EncounterObjectStates.IndexOf(2, 1, 4)];
        Assert.Equal(EncounterObjectStates.KindRemoved, e.Kind);
        // The record says "gone", not "gone from here" — both writers clear the pose and keep only
        // the kind, so a port that stored the death position would be inventing state.
        Assert.Equal(0, e.WorldXOffset);
        Assert.Equal(0, e.WorldYOffset);
        Assert.Equal(0, e.Facing);
    }

    [Fact]
    public void MarkingOneSlotLeavesItsNeighboursAlone() {
        var states = new EncounterObjectStates();
        states.MarkRemoved(refPair: 2, recordIndex: 1, slotIndex: 4);

        // An off-by-one in the stride would land in the next slot or the next record and read
        // identically at the marked index.
        Assert.True(states[EncounterObjectStates.IndexOf(2, 1, 3)].IsEmpty);
        Assert.True(states[EncounterObjectStates.IndexOf(2, 1, 5)].IsEmpty);
        Assert.True(states[EncounterObjectStates.IndexOf(2, 0, 4)].IsEmpty);
        Assert.True(states[EncounterObjectStates.IndexOf(1, 1, 4)].IsEmpty);
        Assert.Equal(1, states.CountOfKind(EncounterObjectStates.KindRemoved));
    }

    [Fact]
    public void RoundTripsThroughASaveBody() {
        var states = new EncounterObjectStates();
        states.MarkRemoved(0, 0, 0);
        states.MarkRemoved(39, 4, 6);
        states.MarkReset(7, 2, 1);

        var body = new byte[EncounterObjectStates.BodyOffset + EncounterObjectStates.SaveSize];
        Assert.True(states.Save(body));

        var reloaded = new EncounterObjectStates();
        reloaded.Load(body);

        Assert.Equal(2, reloaded.CountOfKind(EncounterObjectStates.KindRemoved));
        Assert.Equal(1, reloaded.CountOfKind(EncounterObjectStates.KindReset));
        Assert.Equal(EncounterObjectStates.KindReset,
            reloaded[EncounterObjectStates.IndexOf(7, 2, 1)].Kind);
    }

    [Fact]
    public void TheBlockSitsWhereWeSayItDoesInTheSHIPPEDFiles() {
        // The test a round-trip cannot replace: Load and Save are symmetric, so they agree with
        // each other at a WRONG offset just as happily. The shipped files can be asked directly
        // because the two we have differ in exactly the way the format predicts — a new game has
        // never removed anything, and a played save has.
        byte[]? startup = ReadGameFile("STARTUP.GAM");
        byte[]? save = ReadGameFile(System.IO.Path.Combine("GAMES", "dir.G01", "SAVE02.GAM"));
        if (startup == null || save == null) {
            return; // skip-if-absent, like the other game-data tests
        }

        var fresh = new EncounterObjectStates();
        fresh.Load(startup, EncounterObjectStates.FileOffset);
        Assert.Equal(0, fresh.CountOfKind(EncounterObjectStates.KindRemoved));
        Assert.Equal(0, fresh.CountOfKind(EncounterObjectStates.KindReset));

        var played = new EncounterObjectStates();
        played.Load(save, EncounterObjectStates.FileOffset);

        // Exact counts from the shipped file, which is a fixed artefact. Checked against the
        // wrong-offset cases rather than assumed: reading this file as a BODY offset, or
        // misaligned by 4, both give 0 here, so those errors fail loudly.
        //
        // What this canNOT catch is the block starting one ENTRY early or late — +/-12 still reads
        // ~1350 removals, because the block is a long run of similar records.
        // TheBlockEndsWhereTheCombatantPoolBegins is what pins the start, by requiring
        // BodyOffset + SaveSize to land exactly on the combatant pool. The two tests cover
        // different halves of "is this offset right"; neither is redundant.
        Assert.Equal(1350, played.CountOfKind(EncounterObjectStates.KindRemoved));
        Assert.Equal(9, played.CountOfKind(EncounterObjectStates.KindReset));
    }

    [Fact]
    public void TheBlockEndsWhereTheCombatantPoolBegins() {
        // Two independent derivations of the same boundary. This class walked canassa's macro chain
        // (GAM_ENC_OBJ_STATE <- GAM_ENC_VISITED_TIME(700) <- ... <- sizeof(GameState)); the save
        // writer's block sizes were measured separately. They must meet, and if a future edit moves
        // either, this says so instead of leaving one of them quietly wrong.
        Assert.Equal(ResourceExtraction.SaveGameOffsets.StateDataSize
            + ResourceExtraction.SaveGameOffsets.WorldDataSize,
            EncounterObjectStates.BodyOffset + EncounterObjectStates.SaveSize);
    }

    private static byte[]? ReadGameFile(string name) {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null) {
            string candidate = System.IO.Path.Combine(dir.FullName, "OriginalGame", name);
            if (System.IO.File.Exists(candidate)) {
                return System.IO.File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }
        return null;
    }
}
