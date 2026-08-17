namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using Xunit;

/// <summary>What the picklock screen shows (<c>sub_ovr166_DF</c> @0x5bdf9).</summary>
public class PicklockWorkingSetTests {
    [Fact]
    public void ThePicksBecomeOneStackHoldingThePartyWideTotal() =>
        // Not one stack per member: CountItemInWholeParty is appended as a SINGLE item.
        Assert.Equal(7, PicklockWorkingSet.PickStackQuantity(7));

    [Fact]
    public void NoPicksAppendNoStack() =>
        Assert.False(PicklockWorkingSet.HasPickStack(0));

    [Fact]
    public void OnePickStillAppendsAStack() =>
        Assert.True(PicklockWorkingSet.HasPickStack(1));

    [Fact]
    public void ManyPicksAddOnlyOneEntry() {
        // The distinction the whole type exists for: entries, not items.
        Assert.Equal(4, PicklockWorkingSet.EntryCount(sharedItemCount: 3, partyWideLockpickCount: 20));
        Assert.Equal(4, PicklockWorkingSet.EntryCount(3, 1));
    }

    [Fact]
    public void WithoutPicksTheSetIsJustTheSharedKeys() =>
        Assert.Equal(3, PicklockWorkingSet.EntryCount(3, 0));

    [Fact]
    public void AnEmptySetIsWhatTriggersTheRefusal() {
        // numberOfItems == 0 -> ddx 86. Same arithmetic as LockPicking.CanAttempt, from the other
        // side, so the two cannot disagree about when the screen opens.
        Assert.Equal(0, PicklockWorkingSet.EntryCount(0, 0));
        Assert.False(LockPicking.CanAttempt(sharedItemCount: 0, lockpickCount: 0));

        Assert.True(PicklockWorkingSet.EntryCount(0, 5) > 0);
        Assert.True(LockPicking.CanAttempt(0, 5));
    }

    [Fact]
    public void KeysAloneAreEnoughToOpenTheScreen() {
        // A party with no picks but shared keys still gets the screen — it simply cannot pick.
        Assert.True(PicklockWorkingSet.EntryCount(2, 0) > 0);
        Assert.True(LockPicking.CanAttempt(2, 0));
    }

    [Fact]
    public void TheSynthesizedStackCarriesNoFlags() =>
        // Explicitly cleared: it inherits no condition, equipped bit or modifier from whichever
        // member's picks it stands for.
        Assert.Equal(0, PicklockWorkingSet.PickStackItemFlags);

    [Fact]
    public void TheLatchSwingsUpInThirteenFramesAndSETTLESATTWENTYTWO() {
        // Not 24. The original's counter reaches 24 but then subtracts two again (0x5bcca), so the
        // last frame repeats 22 — ending at 24 lifts the latch two pixels past where it rests.
        int[] frames = PicklockWorkingSet.OpeningLatchOffsets();

        Assert.Equal(13, frames.Length);
        Assert.Equal(0, frames[0]);
        Assert.Equal(22, frames[11]);
        Assert.Equal(22, frames[12]);
        Assert.DoesNotContain(24, frames);
    }

    [Fact]
    public void TheLatchRisesTwoPixelsAFrameAndNeverFallsBack() {
        int[] frames = PicklockWorkingSet.OpeningLatchOffsets();

        for (var i = 1; i < frames.Length; i++) {
            Assert.True(frames[i] - frames[i - 1] is 0 or 2,
                $"frame {i} jumped from {frames[i - 1]} to {frames[i]}");
        }
    }
}
