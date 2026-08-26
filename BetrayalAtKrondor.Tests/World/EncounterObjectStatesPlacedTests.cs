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
}
