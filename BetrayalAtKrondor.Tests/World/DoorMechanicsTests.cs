namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// wcursor_object_toggle_open_close. Two rules carry the behaviour: the lock lives in a rotation
/// field, and you cannot pull a door shut while standing in it.
/// </summary>
public class DoorMechanicsTests {
    /// <summary>A state word for door <paramref name="id"/>, closed, at frame 0.</summary>
    private static int Closed(int id) => id << DoorMechanics.IdShift;

    private static int Open(int id) => Closed(id) | DoorMechanics.OpenBit;

    [Fact]
    public void AZeroStateWordIsNotADoor() {
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(0, 0, false, 0, 0);

        Assert.Equal(DoorMechanics.DoorAction.Ignored, decision.Action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(37)]
    [InlineData(255)]
    public void TheDoorIdRoundTripsThroughTheStateWord(int id) {
        Assert.Equal(id, DoorMechanics.DoorIdOf(Closed(id)));
        Assert.Equal(id, DoorMechanics.DoorIdOf(Open(id)));
    }

    [Fact]
    public void TheOpenFlagIsSevenThousandPlusTheDoorId() {
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Closed(12), 0, false, 0, 0);

        Assert.Equal(7012, decision.OpenFlag);
    }

    [Fact]
    public void AnUnlockedClosedDoorOpens() {
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Closed(5), 0, false, 0, 0);

        Assert.Equal(DoorMechanics.DoorAction.Open, decision.Action);
        Assert.Equal(0, decision.LockValue);
    }

    [Fact]
    public void ALockedDoorReportsItsDifficultyInsteadOfOpening() {
        // The lock value comes out of the entity's pitch field, which is otherwise a rotation.
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Closed(5), 40, false, 0, 0);

        Assert.Equal(DoorMechanics.DoorAction.Locked, decision.Action);
        Assert.Equal(40, decision.LockValue);
    }

    [Fact]
    public void StandingInAnOpenDoorwayYouCannotShutIt() {
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Open(5), 0, true, 100, 100);

        Assert.Equal(DoorMechanics.DoorAction.TooCloseToClose, decision.Action);
    }

    [Fact]
    public void StepBackFarEnoughAndItShuts() {
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Open(5), 0, true, 801, 0);

        Assert.Equal(DoorMechanics.DoorAction.Close, decision.Action);
    }

    [Theory]
    [InlineData(800, 800, DoorMechanics.DoorAction.TooCloseToClose)]   // on the boundary, inclusive
    [InlineData(801, 800, DoorMechanics.DoorAction.Close)]
    [InlineData(800, 801, DoorMechanics.DoorAction.Close)]
    [InlineData(-800, -800, DoorMechanics.DoorAction.TooCloseToClose)] // sign does not matter
    [InlineData(-801, 0, DoorMechanics.DoorAction.Close)]
    public void TheCloseBlockIsASquareNotARadius(int dx, int dy, DoorMechanics.DoorAction expected) {
        // Both axes must be inside, which produces an anomaly a radius check never would: a party
        // at (800, 800) is BLOCKED despite being ~1131 units away, while one at (0, 900) — nearer in
        // a straight line — is allowed to close the door. The corners of the box reach further than
        // its edges.
        Assert.Equal(expected, DoorMechanics.Decide(Open(1), 0, true, dx, dy).Action);
    }

    [Fact]
    public void ALockedDoorThatIsAlreadyOpenJustCloses() {
        // The lock only guards opening; nothing re-checks it on the way shut.
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Open(5), 40, true, 2000, 2000);

        Assert.Equal(DoorMechanics.DoorAction.Close, decision.Action);
    }

    [Fact]
    public void TheFlagDecidesTheBranchNotTheStateWordsOpenBit() {
        // The state word's bit is the visual that follows the flag. Where they disagree the flag
        // wins, which is what makes a door loaded from a save render in the right position.
        DoorMechanics.DoorDecision decision = DoorMechanics.Decide(Closed(5), 0, isOpen: true,
            partyDx: 5000, partyDy: 5000);

        Assert.Equal(DoorMechanics.DoorAction.Close, decision.Action);
    }

    [Fact]
    public void TheOpenBitAndFrameAreWrittenWithoutDisturbingTheId() {
        int state = Closed(37);

        int opened = DoorMechanics.WithOpen(state, true);
        Assert.True(DoorMechanics.IsOpenState(opened));
        Assert.Equal(37, DoorMechanics.DoorIdOf(opened));

        int framed = DoorMechanics.WithFrame(opened, 7);
        Assert.Equal(7, framed & DoorMechanics.FrameMask);
        Assert.Equal(37, DoorMechanics.DoorIdOf(framed));
        Assert.True(DoorMechanics.IsOpenState(framed));

        Assert.False(DoorMechanics.IsOpenState(DoorMechanics.WithOpen(framed, false)));
    }
}
