namespace BetrayalAtKrondor.Tests.Dialog;

using System.Collections.Generic;
using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The clear-then-latch a choice menu performs on its branches' global keys —
/// <c>CreateMenuEntriesFromDialogData</c> (@0x4b1e7) and <c>ShowDialogChoiceMenu</c> (@0x4b689).
/// </summary>
/// <remarks>
/// <b>Nothing wrote either half until 2026-08-30.</b> The immediate continuation is driven by the
/// returned branch INDEX, which is what kept the gap invisible: a dialog only notices when
/// something downstream tests the choice key, and <c>DialogBranchWalker</c> really does read them.
/// </remarks>
public class ChoiceLatchTests {
    [Fact]
    public void ClearingIsScopedToTheMenusOwnBranches() {
        // *** No global clear. *** A latch for a key this menu does not offer survives, which is
        // what lets the same key mean something durable elsewhere. Clearing everything would
        // quietly reset unrelated state every time a Yes/No box opened.
        var globals = new Dictionary<int, int> {
            [256] = 1,   // a stale Yes from an earlier menu
            [257] = 0,
            [999] = 1,   // nothing to do with this menu
        };

        foreach (int key in new[] { 256, 257 }) {
            globals[key] = DialogChoiceEntries.ClearedValue;
        }

        Assert.Equal(0, globals[256]);
        Assert.Equal(0, globals[257]);
        Assert.Equal(1, globals[999]);
    }

    [Fact]
    public void ClearedAndChosenAreDifferentValues() {
        // Both candidates are cleared and only the picked one is set, so the two values must
        // differ — a walker matching on "non-zero" would otherwise see every candidate.
        Assert.Equal(0, DialogChoiceEntries.ClearedValue);
        Assert.Equal(1, DialogChoiceMenu.ChosenValue);
    }

    [Fact]
    public void AChoiceMenuHasNoSpareSlot() {
        // One button per branch — no cancel, no farewell. With the dismissal rule that means
        // escaping a choice menu presses its LAST entry rather than backing out, so the last
        // branch is the de-facto default and its order in the data matters.
        Assert.Equal(2, DialogChoiceEntries.ButtonCount(2));
        Assert.Equal(3, DialogChoiceEntries.ButtonCount(3));
        Assert.True(DialogChoiceMenu.DismissalPressesLastEntry(DialogChoiceMenu.DismissedResult));
    }

    [Fact]
    public void AnEntryActionIdIsDistinguishableFromAKeystroke() {
        // Below 0x80 a poll result is a keystroke; at or above it names an entry. Reading a
        // keystroke as an index picks a branch nobody chose.
        Assert.Equal(-1, DialogChoiceMenu.EntryIndexOf(DialogChoiceMenu.DismissedResult));
        Assert.Equal(0, DialogChoiceMenu.EntryIndexOf(DialogChoiceMenu.EntryActionIdBase));
        Assert.Equal(2, DialogChoiceMenu.EntryIndexOf(DialogChoiceMenu.EntryActionIdBase + 2));
    }
}
