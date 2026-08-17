namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
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

    // ---- destroying the snapped tool ------------------------------------------------------

    private const int PeasantsKey = 61;
    private const int VirtueKey = 62;
    private const int Picklocks = 80;
    private const PicklockAttempt.AttemptResult Broke = PicklockAttempt.AttemptResult.ToolBroke;

    private static readonly Dictionary<int, ObjectInfo> Catalog = new Dictionary<int, ObjectInfo> {
        [PeasantsKey] = new ObjectInfo("Peasant's Key"),
        [VirtueKey] = new ObjectInfo("Virtue Key"),
        [Picklocks] = new ObjectInfo("Picklocks") { Flags = ObjectFlags.Stackable },
    };

    private static ObjectInfo Lookup(int id) => Catalog.TryGetValue(id, out ObjectInfo o) ? o : null;

    private static RuntimeContainer Bag(params (int ObjectId, byte Count)[] items) {
        var container = new RuntimeContainer { Capacity = 20 };
        foreach ((int objectId, byte count) in items) {
            container.Items.Add(new RuntimeItem((byte)objectId, count, 0));
        }
        return container;
    }

    private static int CountOf(RuntimeContainer bag, int objectId) {
        var total = 0;
        foreach (RuntimeItem item in bag.Items) {
            if (item.ObjectId == objectId) {
                total += item.Variable;
            }
        }
        return total;
    }

    [Fact]
    public void TheKEYTHATBROKEIsDestroyed_NotWhicheverKeyIsFirst() {
        // The party can carry several keys, so the working set is not a one-key list and "the key
        // in there" has no single answer. Picking the first one destroys a key the player never
        // dragged — the whole reason the dropped id is carried through the drop.
        RuntimeContainer working = Bag((PeasantsKey, 1), (VirtueKey, 1));
        RuntimeContainer shared = Bag((PeasantsKey, 1), (VirtueKey, 1));

        PicklockDrop.ApplyBreakage(false, Broke, VirtueKey, working, shared, null, Lookup);

        Assert.Equal(1, CountOf(working, PeasantsKey));
        Assert.Equal(0, CountOf(working, VirtueKey));
        Assert.Equal(1, CountOf(shared, PeasantsKey));
        Assert.Equal(0, CountOf(shared, VirtueKey));
    }

    [Fact]
    public void ASnappedPickComesOutOfTheFIRSTPACKHOLDINGONE_NotTheSharedStock() {
        // The displayed pick stack is a synthetic aggregate with no owner, so the party is walked
        // in roster order. Nothing is taken from the shared stock, where picks never are.
        RuntimeContainer working = Bag((Picklocks, 7));
        RuntimeContainer shared = Bag((PeasantsKey, 1));
        RuntimeContainer empty = Bag();
        RuntimeContainer carrier = Bag((Picklocks, 3));
        RuntimeContainer alsoCarrying = Bag((Picklocks, 2));

        Assert.True(PicklockDrop.ApplyBreakage(true, Broke, Picklocks, working, shared,
            new[] { empty, carrier, alsoCarrying }, Lookup));

        Assert.Equal(6, CountOf(working, Picklocks));
        Assert.Equal(2, CountOf(carrier, Picklocks));
        Assert.Equal(2, CountOf(alsoCarrying, Picklocks)); // only the first holder pays
        Assert.Equal(1, CountOf(shared, PeasantsKey));
    }

    [Fact]
    public void NothingIsDestroyedWhenTheToolSurvives() {
        RuntimeContainer working = Bag((PeasantsKey, 1));
        RuntimeContainer shared = Bag((PeasantsKey, 1));

        Assert.False(PicklockDrop.ApplyBreakage(
            false, PicklockAttempt.AttemptResult.Failed, PeasantsKey, working, shared, null, Lookup));
        Assert.False(PicklockDrop.ApplyBreakage(
            false, PicklockAttempt.AttemptResult.Opened, PeasantsKey, working, shared, null, Lookup));

        Assert.Equal(1, CountOf(working, PeasantsKey));
        Assert.Equal(1, CountOf(shared, PeasantsKey));
    }

    [Fact]
    public void AStackLosesOne_NotTheWholeStack() {
        RuntimeContainer working = Bag((Picklocks, 7));
        RuntimeContainer carrier = Bag((Picklocks, 4));

        PicklockDrop.ApplyBreakage(true, Broke, Picklocks, working, null, new[] { carrier }, Lookup);

        Assert.Equal(6, CountOf(working, Picklocks));
        Assert.Equal(3, CountOf(carrier, Picklocks));
    }

    [Fact]
    public void TheWorkingSetStillLosesItWhenNoPackHasOneLeft() {
        // The screen must stop offering a tool that just snapped even if the party bookkeeping
        // cannot find it — otherwise it can be dragged onto the lock again.
        RuntimeContainer working = Bag((Picklocks, 1));

        Assert.False(PicklockDrop.ApplyBreakage(
            true, Broke, Picklocks, working, null, new RuntimeContainer[] { null }, Lookup));

        Assert.Empty(working.Items);
    }
}
