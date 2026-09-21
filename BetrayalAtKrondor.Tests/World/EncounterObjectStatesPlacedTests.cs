namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The third writer to the encounter-actor block — <c>rgnenc_persist_actor_placed</c>.
/// </summary>
public class EncounterObjectStatesPlacedTests {
    private const int RefPair = 3;
    private const int Record = 2;
    private const int Slot = 4;

    private static EncounterObjectStates WithPose(int x, int y, short facing) {
        var states = new EncounterObjectStates();
        states.MarkPlaced(RefPair, Record, Slot, x, y, facing, underground: false);
        return states;
    }

    private static EncounterObjectStates.Entry Read(EncounterObjectStates states) =>
        states[EncounterObjectStates.IndexOf(RefPair, Record, Slot)];

    [Fact]
    public void AboveGroundTheCallersPoseIsWritten() {
        // *** THE ONLY WRITER THAT KEEPS A POSE. *** The removal and the reset zero it, which made
        // "the block does not remember where" look like a property of the block rather than of
        // those two writers. A roamer saved outdoors resumes where it had walked to.
        EncounterObjectStates.Entry e = Read(WithPose(1234, 5678, 900));

        Assert.Equal(1234, e.WorldXOffset);
        Assert.Equal(5678, e.WorldYOffset);
        Assert.Equal(900, e.Facing);
    }

    [Fact]
    public void UndergroundTheSTOREDPoseIsKeptAndTheCallersIsIgnored() {
        // The original re-reads the existing entry down there and writes back only the kind, so a
        // dungeon actor resumes exactly where the block last had it. Passing the live pose in and
        // having it applied anyway is the mistake this pins.
        var states = new EncounterObjectStates();
        states.MarkPlaced(RefPair, Record, Slot, 1111, 2222, 333, underground: false);
        states.MarkPlaced(RefPair, Record, Slot, 9999, 8888, 777, underground: true);

        EncounterObjectStates.Entry e = Read(states);
        Assert.Equal(1111, e.WorldXOffset);
        Assert.Equal(2222, e.WorldYOffset);
        Assert.Equal(333, e.Facing);
    }

    [Fact]
    public void TheKindBecomesSTANDINGWhateverItWas() {
        var states = new EncounterObjectStates();
        states.SetKindForTest(RefPair, Record, Slot, EncounterObjectStates.KindRoaming);

        states.MarkPlaced(RefPair, Record, Slot, 10, 20, 30, underground: false);

        Assert.Equal(EncounterObjectStates.KindStanding, Read(states).Kind);
    }

    [Fact]
    public void APlacedActorIsNeverPromotedBackToRoaming() {
        // The one-way trip: nothing in the game turns Standing back into Roaming, so a wandering
        // monster that gets saved comes back stopped and stays stopped.
        var states = new EncounterObjectStates();
        states.SetKindForTest(RefPair, Record, Slot, EncounterObjectStates.KindRoaming);
        states.MarkPlaced(RefPair, Record, Slot, 0, 0, 0, underground: false);

        Assert.Equal(0, states.StopRoaming(RefPair, Record));
        Assert.Equal(EncounterObjectStates.KindStanding, Read(states).Kind);
    }

    [Fact]
    public void ItWritesOneSlotAndLeavesTheRestOfTheRecordAlone() {
        var states = new EncounterObjectStates();
        states.SetKindForTest(RefPair, Record, Slot + 1, EncounterObjectStates.KindRoaming);

        states.MarkPlaced(RefPair, Record, Slot, 42, 43, 44, underground: false);

        Assert.Equal(EncounterObjectStates.KindRoaming,
            states[EncounterObjectStates.IndexOf(RefPair, Record, Slot + 1)].Kind);
    }

    [Fact]
    public void ThePoseSurvivesARoundTripThroughASaveBody() {
        // It is only worth keeping if it persists; the block's own reader/writer must carry it.
        EncounterObjectStates written = WithPose(-500, 600, -700);
        var body = new byte[EncounterObjectStates.BodyOffset + EncounterObjectStates.SaveSize];
        Assert.True(written.Save(body));

        var read = new EncounterObjectStates();
        read.Load(body);

        EncounterObjectStates.Entry e = Read(read);
        Assert.Equal(-500, e.WorldXOffset);
        Assert.Equal(600, e.WorldYOffset);
        Assert.Equal(-700, e.Facing);
        Assert.Equal(EncounterObjectStates.KindStanding, e.Kind);
    }
    /// <summary>
    /// The underground keep is only worth keeping if a real pose is stored — TASK-558.
    /// </summary>
    /// <remarks>
    /// <c>MarkPlaced(underground: true)</c> keeps the STORED pose rather than taking the arena's,
    /// faithfully. The stored pose is seeded here by an above-ground placement, which is how
    /// production writes one — these used to seed it with a <c>SnapshotPose</c> helper modelling
    /// <c>rgnenc_persist_zone_snapshot</c>, but TASK-558 concluded in so many words that *"the fix
    /// is not the zone snapshot this task was named for"*: the real defect was
    /// <see cref="EncounterObjectStates.StopRoaming"/> blanking the pose. The helper had no
    /// production caller and only these tests kept it looking alive, so it is gone and the seeding
    /// now uses live API only.
    /// </remarks>
    [Fact]
    public void AStoredPoseSurvivesTheUndergroundKeep() {
        var states = new EncounterObjectStates();
        // Above ground, MarkPlaced WRITES the pose it is given — that is the seed.
        states.MarkPlaced(RefPair, Record, Slot, 1234, -5678, 0x4000, underground: false);

        // The fight ends underground: MarkPlaced keeps what was already stored.
        states.MarkPlaced(RefPair, Record, Slot, 9999, 8888, 777, underground: true);

        EncounterObjectStates.Entry e = Read(states);
        Assert.Equal(1234, e.WorldXOffset);
        Assert.Equal(-5678, e.WorldYOffset);
        Assert.Equal(0x4000, e.Facing);
    }

    [Fact]
    public void WithNoStoredPoseTheUndergroundKeepIsZeros() {
        // The control, and the bug as it was reported: five Mac Mordain corpses at their tile's
        // origin because the kept pose was never written.
        var states = new EncounterObjectStates();

        states.MarkPlaced(RefPair, Record, Slot, 9999, 8888, 777, underground: true);

        EncounterObjectStates.Entry e = Read(states);
        Assert.Equal(0, e.WorldXOffset);
        Assert.Equal(0, e.WorldYOffset);
    }

    /// <summary>
    /// Defeating a roaming actor must not lose where it was standing — TASK-558.
    /// </summary>
    /// <remarks>
    /// <b>The bug this pins put every dungeon body at its tile's origin.</b> The original assigns
    /// the kind word alone — <c>g_pEncounterObjectState[base + j].wKind_state = 0x400;</c>
    /// (<c>rgnenc_mark_defended</c>, RGNENC.C:496-498) — and leaves the pose beside it untouched.
    /// <see cref="EncounterObjectStates.StopRoaming"/> used to go through the private Write helper,
    /// which replaces the WHOLE entry, so the offsets and facing were blanked. An offset of (0,0)
    /// from the party's tile IS the tile origin, which is how five corpses in the upper Mac Mordain
    /// Cadal came to be stacked at (640000,640000).
    ///
    /// <para><b>Non-vacuous:</b> with the preservation removed and <c>Write(at, KindStanding)</c>
    /// restored, this fails on the first offset assertion, reporting 0 instead of 4321.</para>
    /// </remarks>
    [Fact]
    public void StopRoamingKeepsThePoseAndOnlyChangesTheKind() {
        // Built through Load, because nothing public promotes an entry to Roaming — that happens
        // inside the seed. A crafted block is the only way to start from "placed and walking with a
        // real pose", which is exactly the state the defect destroys.
        var body = new byte[EncounterObjectStates.BodyOffset + EncounterObjectStates.SaveSize];
        int at = EncounterObjectStates.BodyOffset
            + (EncounterObjectStates.IndexOf(RefPair, Record, Slot) * EncounterObjectStates.EntrySize);
        System.BitConverter.GetBytes(4321).CopyTo(body, at);
        System.BitConverter.GetBytes(8765).CopyTo(body, at + 4);
        System.BitConverter.GetBytes((short)0x2000).CopyTo(body, at + 8);
        System.BitConverter.GetBytes((ushort)(EncounterObjectStates.KindRoaming << 8)).CopyTo(body, at + 10);

        var states = new EncounterObjectStates();
        states.Load(body);
        Assert.Equal(EncounterObjectStates.KindRoaming, Read(states).Kind);

        Assert.Equal(1, states.StopRoaming(RefPair, Record));

        EncounterObjectStates.Entry after = Read(states);
        Assert.Equal(4321, after.WorldXOffset);
        Assert.Equal(8765, after.WorldYOffset);
        Assert.Equal((short)0x2000, after.Facing);
        Assert.Equal(EncounterObjectStates.KindStanding, after.Kind);
    }
}
