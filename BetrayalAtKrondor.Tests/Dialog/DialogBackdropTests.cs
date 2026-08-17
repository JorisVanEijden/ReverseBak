namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Which dialogs are drawn on the full-screen parchment.
/// </summary>
/// <remarks>
/// The trap these pin down: the backdrop is chosen by a FLAG, not by the dialog type. Reading
/// <see cref="DialogType.PlainFullScreen"/> as "full-screen parchment" is the natural mistake, and
/// it puts the parchment on the wrong entry of the shipped pair below.
/// </remarks>
public class DialogBackdropTests {
    private static DialogEntry Entry(DialogType type, DialogEntryFlags flags) =>
        new DialogEntry { DialogType = type, Flags = flags };

    [Fact]
    public void IsolatePaletteDrawsTheParchment() =>
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(DialogEntryFlags.IsolatePalette));

    [Fact]
    public void WithoutTheFlagThereIsNoBackdrop() =>
        Assert.False(DialogBackdrop.DrawsFullScreenBackdrop(DialogEntryFlags.Legacy10));

    [Fact]
    public void TheFullScreenTYPEDoesNotDrawIt() =>
        // The whole point. ExecuteDialog gates the backdrop on the flag at 0x49903 and never
        // consults dialogType for it.
        Assert.False(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.PlainFullScreen, DialogEntryFlags.Legacy10)));

    [Fact]
    public void AnOrdinaryTypeWithTheFlagDoesDrawIt() =>
        // And the converse: the questioning menu (2000001) is DialogType.Normal and still gets the
        // parchment, because it carries IsolatePalette.
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.Normal,
                DialogEntryFlags.PreserveKeyword | DialogEntryFlags.IsolatePalette
                | DialogEntryFlags.ChoiceMenu)));

    [Fact]
    public void TheFlagIsFoundAmongOthers() =>
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            DialogEntryFlags.CenterText | DialogEntryFlags.IsolatePalette
            | DialogEntryFlags.SkipWait));

    [Fact]
    public void NothingToAskAboutIsNotABackdrop() =>
        Assert.False(DialogBackdrop.DrawsFullScreenBackdrop((DialogEntry)null));

    [Fact]
    public void TheResourceIsTheOneTheOriginalLoads() =>
        // resourceLoadSCX("Dialog.scr") at 0x499dc; our locator reaches that member as DIALOG.SCX.
        Assert.Equal("DIALOG.SCX", DialogBackdrop.Resource);
}
