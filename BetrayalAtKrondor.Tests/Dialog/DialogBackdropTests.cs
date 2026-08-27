namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Which dialogs are drawn on the full-screen parchment.
/// </summary>
/// <remarks>
/// <b>These tests previously asserted the opposite of the truth</b>, and were green the whole time,
/// because they encoded one half of a branch as the whole rule. <c>ExecuteDialog</c> tests
/// <c>flags &amp; 2</c> at 0x49903; a port that stops reading there concludes the flag is the only
/// trigger. The branch taken when the flag is CLEAR (0x49b21) tests <c>dialogType == 6</c> and
/// draws the same parchment. Both triggers are real, so both are pinned here — and the shipped
/// entry that distinguishes them is pinned by name, because it is the one a player sees first.
/// </remarks>
public class DialogBackdropTests {
    private static DialogEntry Entry(DialogType type, DialogEntryFlags flags) =>
        new DialogEntry { DialogType = type, Flags = flags };

    [Fact]
    public void IsolatePaletteDrawsTheParchment() =>
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            DialogEntryFlags.IsolatePalette, DialogType.Normal));

    [Fact]
    public void TheFullScreenTYPEDrawsItWithNoFlagAtAll() =>
        // *** The correction. *** 0x49b27-0x49b33: mov al,[bx+dialogType]; dec ax; cmp ax,5; jnz.
        // dialogType 6 is PlainFullScreen, so the type triggers the parchment on its own.
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.PlainFullScreen, DialogEntryFlags.Legacy10)));

    [Fact]
    public void TheSHIPPEDNarrativeThePlayerMeetsFirstGetsIt() {
        // DIAL_Z30 @120988, "Someone was calling." — PlainFullScreen carrying only Legacy10. The
        // earlier reading called this the worked example of an entry that gets NO backdrop, and our
        // port drew its text straight over the world because of it. Named here so the regression
        // has to argue with a specific dialog rather than an abstraction.
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.PlainFullScreen, DialogEntryFlags.Legacy10)));
    }

    [Fact]
    public void AnOrdinaryTypeWithTheFlagAlsoDrawsIt() =>
        // The questioning menu (2000001) is DialogType.Normal and gets the parchment from the flag.
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.Normal,
                DialogEntryFlags.PreserveKeyword | DialogEntryFlags.IsolatePalette
                | DialogEntryFlags.ChoiceMenu)));

    [Fact]
    public void AnOrdinaryTypeWithNoFlagDrawsNothing() =>
        // Neither trigger fires: this is the case that really does render over what is on screen.
        Assert.False(DialogBackdrop.DrawsFullScreenBackdrop(
            Entry(DialogType.Normal, DialogEntryFlags.Legacy10)));

    [Fact]
    public void TheFlagIsFoundAmongOthers() =>
        Assert.True(DialogBackdrop.DrawsFullScreenBackdrop(
            DialogEntryFlags.CenterText | DialogEntryFlags.IsolatePalette
            | DialogEntryFlags.SkipWait, DialogType.Normal));

    [Fact]
    public void NothingToAskAboutIsNotABackdrop() =>
        Assert.False(DialogBackdrop.DrawsFullScreenBackdrop((DialogEntry)null));

    [Fact]
    public void ONLYTheFlagPathRedrawsTheWorldOverIt() {
        // *** The reason the two triggers cannot be collapsed into one flag. *** The flagged form
        // paints the world viewport back over the parchment (0x49a98); the type-6 form falls
        // through to the text with the parchment still covering the screen. Treating them alike
        // would punch a viewport-shaped hole in the narrative the player is meant to read.
        Assert.True(DialogBackdrop.RedrawsWorldViewport(DialogEntryFlags.IsolatePalette));
        Assert.False(DialogBackdrop.RedrawsWorldViewport(DialogEntryFlags.Legacy10));
    }

    [Fact]
    public void TheResourceIsTheOneTheOriginalLoads() =>
        // resourceLoadSCX("Dialog.scr") at 0x499dc and again at 0x49b41; the loader rewrites the
        // last character to 'x', so our locator reaches that same member as DIALOG.SCX.
        Assert.Equal("DIALOG.SCX", DialogBackdrop.Resource);
}
