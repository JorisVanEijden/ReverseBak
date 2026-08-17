namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using Xunit;

/// <summary>Dropping a tool on the lock (<c>sub_ovr166_210</c> @0x5beb0).</summary>
public class PicklockDropTests {
    private const bool Picks = true;
    private const bool Key = false;

    [Fact]
    public void AKeysObjectIdIsItsLockNumberPlusSixty() =>
        Assert.Equal(60 + 7, PicklockDrop.KeyObjectIdFor(7));

    [Theory]
    [InlineData(Picks, PicklockAttempt.AttemptResult.Opened, 83)]
    [InlineData(Picks, PicklockAttempt.AttemptResult.Failed, 84)]
    [InlineData(Picks, PicklockAttempt.AttemptResult.ToolBroke, 85)]
    [InlineData(Key, PicklockAttempt.AttemptResult.Opened, 81)]
    [InlineData(Key, PicklockAttempt.AttemptResult.Failed, 82)]
    [InlineData(Key, PicklockAttempt.AttemptResult.ToolBroke, 245)]
    public void EachOutcomeHasItsOwnDialog(bool picks, PicklockAttempt.AttemptResult result, int ddx) =>
        Assert.Equal(ddx, PicklockDrop.DialogFor(picks, result));

    [Fact]
    public void TheSixDialogsAreAllDistinct() {
        // Keys and picks never share a message, even for the same outcome — "wrong key" and
        // "you cannot open it" are different sentences about different tools.
        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (bool picks in new[] { true, false }) {
            foreach (PicklockAttempt.AttemptResult r in new[] {
                         PicklockAttempt.AttemptResult.Opened,
                         PicklockAttempt.AttemptResult.Failed,
                         PicklockAttempt.AttemptResult.ToolBroke }) {
                Assert.True(seen.Add(PicklockDrop.DialogFor(picks, r)));
            }
        }
        Assert.Equal(6, seen.Count);
    }

    // ---- the write-back, which is the part that differs by tool ----------------------------

    [Fact]
    public void NothingBreaksWhenNothingBroke() {
        Assert.Equal(PicklockDrop.BreakageTarget.None,
            PicklockDrop.BreakageFor(Picks, PicklockAttempt.AttemptResult.Opened));
        Assert.Equal(PicklockDrop.BreakageTarget.None,
            PicklockDrop.BreakageFor(Key, PicklockAttempt.AttemptResult.Failed));
    }

    [Fact]
    public void ABrokenKeyComesOutOfTheSharedInventory() =>
        // Keys live there; the original removes the key from the scratch container AND from the
        // shared inventory explicitly.
        Assert.Equal(PicklockDrop.BreakageTarget.SharedInventory,
            PicklockDrop.BreakageFor(Key, PicklockAttempt.AttemptResult.ToolBroke));

    [Fact]
    public void ABrokenPicklockComesOutOfThePartyAtLarge() =>
        // The displayed pick stack is a synthetic aggregate with no owning member, so there is no
        // container to take it from — it goes through the generic consume-one-from-the-party path.
        Assert.Equal(PicklockDrop.BreakageTarget.PartyAtLarge,
            PicklockDrop.BreakageFor(Picks, PicklockAttempt.AttemptResult.ToolBroke));

    [Fact]
    public void TheTwoToolsNeverShareABreakageTarget() =>
        // The distinction this type exists for: treating them alike loses picks that are not in
        // the shared inventory, or takes keys from packs where keys never are.
        Assert.NotEqual(
            PicklockDrop.BreakageFor(Key, PicklockAttempt.AttemptResult.ToolBroke),
            PicklockDrop.BreakageFor(Picks, PicklockAttempt.AttemptResult.ToolBroke));
}
