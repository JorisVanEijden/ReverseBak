namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// Reading the riddle at all — <c>CIPHER.C</c>'s legibility branch.
/// </summary>
public class CipherLegibilityTests {
    [Fact]
    public void EitherRouteAloneIsEnough() {
        Assert.True(CipherPuzzleLayout.IsLegible(readerInParty: true, readerSpellActive: false));
        Assert.True(CipherPuzzleLayout.IsLegible(readerInParty: false, readerSpellActive: true));
        Assert.False(CipherPuzzleLayout.IsLegible(readerInParty: false, readerSpellActive: false));
    }

    [Fact]
    public void THEALIENSCRIPTIsShownFirstEvenToAPartyThatCanReadIt() {
        // The screen is rendered before the test is made, so the riddle visibly TRANSFORMS rather
        // than simply appearing legible. Drawing straight in the readable font is the obvious
        // implementation and loses the only moment the alien script is ever seen by such a party.
        Assert.True(CipherPuzzleLayout.AlienIsAlwaysDrawnFirst);
        Assert.Equal(CipherPuzzleLayout.AlienFont,
            CipherPuzzleLayout.FontForPass(firstPass: true, legible: true));
    }

    [Fact]
    public void OnlyTheSecondPassOfALegiblePartyUsesThePuzzleFont() {
        Assert.Equal(CipherPuzzleLayout.PuzzleFont,
            CipherPuzzleLayout.FontForPass(firstPass: false, legible: true));
        Assert.Equal(CipherPuzzleLayout.AlienFont,
            CipherPuzzleLayout.FontForPass(firstPass: false, legible: false));
    }

    [Fact]
    public void TheOpeningDialogsAreBOTHHeardWhicheverWayItReads() {
        // Both play before the legibility test, so an unreadable riddle is not a quieter screen.
        Assert.Equal(0x0b, CipherPuzzleLayout.OpeningDialog);
        Assert.Equal(0x0c, CipherPuzzleLayout.AfterDrawDialog);
        Assert.NotEqual(CipherPuzzleLayout.OpeningDialog, CipherPuzzleLayout.AfterDrawDialog);
    }

    [Fact]
    public void TheReaderIsACHARACTERIdAndNotARosterSlot() {
        // The original scans the active party for this id rather than indexing it. Character 1 is
        // Gorath — a moredhel reading moredhel script — and no skill substitutes for him.
        Assert.Equal(1, CipherPuzzleLayout.ReaderPartyMember);
        // The spell route is a mask SLOT, and the slot is the dispatch order rather than the spell
        // number — so naming it 4 only means anything if that slot really is Union.
        Assert.Equal(CipherPuzzleLayout.ReaderSpellEvent,
            GameData.Resources.Spells.FieldSpells.EventIdOf(
                GameData.Resources.Spells.FieldSpells.Union));
    }

    [Fact]
    public void TheScreenHasItsOwnTrack() {
        Assert.Equal(0x3eb, CipherPuzzleLayout.MusicTrack);
    }
}
