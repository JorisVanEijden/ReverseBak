namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

/// <summary>
/// What a cutscene's dialog command asks for (TASK-159).
/// </summary>
/// <remarks>
/// The cutscene engine is the least-covered system in the project, and these rules were the part of
/// it reachable without standing up a player: a dispatch on two fields, a key derivation and a
/// six-way mode split. All three were previously inline in Unity — the key derivation in four
/// separate places — so nothing could fail if one copy drifted.
/// </remarks>
public class CutsceneDialogCommandTests {
    [Fact]
    public void THETWOFIELDSAreReadTogether() {
        // *** Dialog16Id 0 is three different commands. *** Reading it alone, a port clears the
        // dialog plate when the scene meant to turn a book page.
        Assert.Equal(CutsceneDialogCommand.Kind.Clear, CutsceneDialogCommand.KindOf(0, 255));
        Assert.Equal(CutsceneDialogCommand.Kind.BookAnimation, CutsceneDialogCommand.KindOf(0, 0));
        Assert.Equal(CutsceneDialogCommand.Kind.BookAnimation, CutsceneDialogCommand.KindOf(0, 20));
        Assert.Equal(CutsceneDialogCommand.Kind.None, CutsceneDialogCommand.KindOf(0, 21));
    }

    [Fact]
    public void MINUSONEIsADrawCommand_NotDialog1599999() {
        // It blits from image slot 2 and is deliberately unimplemented. Falling through to the
        // display arm would ask for a dialog id below the base — a lookup that cannot succeed and
        // would log a missing-entry warning on every frame that carries one.
        Assert.Equal(CutsceneDialogCommand.Kind.None, CutsceneDialogCommand.KindOf(-1, 0));
        Assert.Equal(CutsceneDialogCommand.Kind.None, CutsceneDialogCommand.KindOf(-1, 255));
    }

    [Fact]
    public void APOSITIVEIdShowsADialogWhateverTheMode() {
        foreach (int arg2 in new[] { 0, 1, 2, 3, 4, 5, 255 }) {
            Assert.Equal(CutsceneDialogCommand.Kind.Display,
                CutsceneDialogCommand.KindOf(12, arg2));
        }
    }

    [Fact]
    public void ANEGATIVEIdOtherThanMinusOneDoesNothing() {
        // Not observed in the shipped data, but the display arm is guarded by `> 0` rather than
        // `!= 0`, and this pins that rather than leaving it to the next reader to re-derive.
        Assert.Equal(CutsceneDialogCommand.Kind.None, CutsceneDialogCommand.KindOf(-7, 0));
    }

    [Fact]
    public void THECLEARTestBeatsTheBookStepRange() {
        // The order is load-bearing the moment MaxBookStep ever widens: 255 must stay a clear.
        // Asserted through the public constants so that widening the range fails HERE rather than
        // silently turning every clear into a book step.
        Assert.True(CutsceneDialogCommand.ClearArg > CutsceneDialogCommand.MaxBookStep,
            "if the step range ever reaches the clear arg, KindOf's ordering is the only thing "
            + "keeping them apart — and this test is the warning");
        Assert.Equal(CutsceneDialogCommand.Kind.Clear,
            CutsceneDialogCommand.KindOf(0, CutsceneDialogCommand.ClearArg));
    }

    [Fact]
    public void THEDDXKEYTakesTheFULLIdNotTheField() {
        // *** The trap the four inline copies invited. *** Three call sites hold the raw field and
        // must add the base; one already holds a resolved id and must not. Adding it twice lands in
        // DIAL_Z32 and finds nothing.
        int full = CutsceneDialogCommand.DialogIdFor(12);
        Assert.Equal(1600012, full);
        Assert.Equal("DIAL_Z16.DDX", CutsceneDialogCommand.DdxKeyFor(full));
        Assert.NotEqual("DIAL_Z16.DDX",
            CutsceneDialogCommand.DdxKeyFor(CutsceneDialogCommand.DialogIdFor(full)));
    }

    [Theory]
    [InlineData(1600000, "DIAL_Z16.DDX")]
    [InlineData(1699999, "DIAL_Z16.DDX")]
    [InlineData(1700000, "DIAL_Z17.DDX")]
    [InlineData(3000000, "DIAL_Z30.DDX")]
    public void THEKEYIsZeroPaddedToTwoDigits(int dialogId, string expected) =>
        // "DIAL_Z6.DDX" resolves to nothing; the D2 is not cosmetic.
        Assert.Equal(expected, CutsceneDialogCommand.DdxKeyFor(dialogId));

    [Fact]
    public void WAITINGForInputIsNotARange() {
        // *** Three of six wait, and they are not contiguous. *** Both `arg2 != 3` and `arg2 >= 4`
        // look like reasonable simplifications and both are wrong for two of the six modes.
        Assert.True(CutsceneDialogCommand.WaitsForInput(0));    // narrative, waits
        Assert.False(CutsceneDialogCommand.WaitsForInput(1));   // select font
        Assert.False(CutsceneDialogCommand.WaitsForInput(2));   // open book
        Assert.False(CutsceneDialogCommand.WaitsForInput(3));   // narrative, auto-advances
        Assert.True(CutsceneDialogCommand.WaitsForInput(4));    // interactive
        Assert.True(CutsceneDialogCommand.WaitsForInput(5));    // simple
    }

    [Fact]
    public void ANUNKNOWNModeDoesNotWait() {
        // A cutscene that stops for a keypress nobody knows to give is unrecoverable; one that
        // advances through an unknown mode merely looks wrong. The safe default is the one that
        // keeps playing.
        Assert.False(CutsceneDialogCommand.WaitsForInput(99));
        Assert.False(CutsceneDialogCommand.WaitsForInput(-1));
    }
}
