namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Data;
using Xunit;

/// <summary>
/// The travel HUD's bookmark quick-save.
/// </summary>
public class BookmarkSaveTests {
    [Fact]
    public void ABookmarkNeedsSomewhereToGo() {
        // The whole routine is wrapped in a save-directory check: with none it refuses and does
        // nothing, so the button is unusable until the player has saved once.
        Assert.False(BookmarkSave.CanSave(hasActiveSaveDirectory: false));
        Assert.True(BookmarkSave.CanSave(hasActiveSaveDirectory: true));
    }

    [Fact]
    public void ItAlwaysWritesSlotZeroOfTheCurrentDirectory() {
        // One bookmark per saved game, overwritten without asking — not a separate autosave file.
        Assert.Equal(0, BookmarkSave.Slot);
    }

    [Fact]
    public void THEHEADERNAMEISNOTTHEDISPLAYEDNAME() {
        // The header carries "Copied Bookmark"; the file picker forces "Bookmark" over it for slot 0
        // regardless. Two strings for one slot, neither derived from the other — so a port that
        // writes the displayed name into the header produces a file the original would show
        // differently.
        Assert.Equal("Copied Bookmark", BookmarkSave.HeaderName);
        Assert.NotEqual("Bookmark", BookmarkSave.HeaderName);
    }

    [Fact]
    public void AFAILEDWRITESAYSNOTHING() {
        // Only the success path speaks. Adding an error box would show a message the game never has,
        // about a failure the player cannot act on.
        Assert.False(BookmarkSave.ReportsWriteFailure);
    }

    [Fact]
    public void ONLYTHREESCANCODESACCEPTTheVerifyPrompt() {
        // Not any-key. Enter plus two others; everything else declines.
        Assert.True(BookmarkSave.VerifyAccepts(0x1c));
        Assert.True(BookmarkSave.VerifyAccepts(0x4c));
        Assert.True(BookmarkSave.VerifyAccepts(0x52));

        Assert.False(BookmarkSave.VerifyAccepts(0x39));   // space
        Assert.False(BookmarkSave.VerifyAccepts(0x01));   // escape
    }

    [Fact]
    public void ARIGHTCLICKCANCELSEvenThoughThePromptDoesNotSaySo() {
        // A left button is folded in as Enter and a right one as scancode 1, which is not in the
        // accepting set — so right-click declines.
        Assert.True(BookmarkSave.VerifyAccepts(BookmarkSave.LeftClickScanCode));
        Assert.False(BookmarkSave.VerifyAccepts(BookmarkSave.RightClickScanCode));
    }

    [Fact]
    public void TheCompassIconIsShiftedTHENScaled() {
        // (yaw >> 13) << 2 — three bits of heading select one of eight facings, multiplied by four
        // because the icon indexes four-frame groups. Shifting by 13 alone indexes the wrong table.
        Assert.Equal(0, BookmarkSave.CompassIconFor(0));
        Assert.Equal(4, BookmarkSave.CompassIconFor(1 << 13));
        Assert.Equal(28, BookmarkSave.CompassIconFor(7 << 13));

        // Every facing lands on a multiple of four, and there are eight of them.
        for (var facing = 0; facing < 8; facing++) {
            int icon = BookmarkSave.CompassIconFor(facing << 13);
            Assert.Equal(0, icon % 4);
            Assert.Equal(facing * 4, icon);
        }
    }

    [Fact]
    public void AChapterWithNoMapEntryStillSaves() {
        // The failed lookup writes -1/-1 and icon 0; it is not a reason to refuse the bookmark.
        Assert.Equal(-1, BookmarkSave.NoMapPosition);
        Assert.True(BookmarkSave.CanSave(hasActiveSaveDirectory: true));
    }

    [Fact]
    public void TheSlotFileNameRoundTrips() {
        for (var slot = 0; slot <= 99; slot++) {
            Assert.True(BookmarkSave.TryParseSlot(BookmarkSave.FileNameForSlot(slot), out int back));
            Assert.Equal(slot, back);
        }
        Assert.Equal("SAVE00.GAM", BookmarkSave.FileNameForSlot(BookmarkSave.Slot));
    }

    [Fact]
    public void ASTRAYFILEISNOTMISTAKENFORSLOTZERO() {
        // *** The reason this refuses rather than guessing. *** Slot 0 IS the bookmark, so anything
        // that parsed loosely to 0 would make a later bookmark overwrite the wrong file.
        Assert.False(BookmarkSave.TryParseSlot("NOTES.TXT", out _));
        Assert.False(BookmarkSave.TryParseSlot("SAVE.GAM", out _));
        Assert.False(BookmarkSave.TryParseSlot("SAVE0.GAM", out _));
        Assert.False(BookmarkSave.TryParseSlot("SAVE001.GAM", out _));
        Assert.False(BookmarkSave.TryParseSlot("SAVE-1.GAM", out _));
        Assert.False(BookmarkSave.TryParseSlot("", out _));
        Assert.False(BookmarkSave.TryParseSlot(null, out _));
    }

    [Fact]
    public void ParsingIsCaseInsensitive() {
        // DOS-era names arrive in either case depending on who wrote the directory.
        Assert.True(BookmarkSave.TryParseSlot("save07.gam", out int slot));
        Assert.Equal(7, slot);
    }
}
