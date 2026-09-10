namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// A key a dialog hands over goes on the party's ring, never into the receiver's pack.
/// </summary>
/// <remarks>
/// <c>cmbinv_actor_acquire_item</c> (CMBINV.C:1008) opens with the category-7 branch, before the
/// space test — the same diversion <see cref="InventoryTransfer"/> applies to a pickup. Until
/// 2026-09-10 the dialog path skipped it, and since the picklock screen's working set is built from
/// the ring, a key that landed in a pack could not be tried on any lock.
/// </remarks>
public class AcquiredKeyGoesOnTheRingTests {
    /// <summary>Object 71, the Royal Key of Krondor: the only key the game ever hands out.</summary>
    private const int RoyalKeyOfKrondor = 71;

    private const int Broadsword = 18;

    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") {
            Number = RoyalKeyOfKrondor, Name = "Royal Key of Krondor",
            ObjectType = ObjectType.Key, InventorySlots = 1, MaxAmount = 1,
        },
        new ObjectInfo("O") {
            Number = Broadsword, Name = "Broadsword",
            ObjectType = ObjectType.Sword, InventorySlots = 3, MaxAmount = 1,
        },
    });

    private static RuntimeContainer Pack() =>
        new RuntimeContainer { Capacity = 20, ContainerType = SaveGameContainerType.Inventory };

    private static RuntimeContainer Ring() =>
        new RuntimeContainer { Capacity = 20, ContainerType = SaveGameContainerType.SharedKeys };

    [Fact]
    public void AKeyLandsOnTheRingAndNotInThePack() {
        RuntimeContainer pack = Pack(), ring = Ring();

        Assert.True(InventoryAcquire.TryGive(
            pack, new RuntimeItem(RoyalKeyOfKrondor, 1, 0), Objects(), ring));

        Assert.Empty(pack.Items);
        Assert.Equal(RoyalKeyOfKrondor, Assert.Single(ring.Items).ObjectId);
    }

    [Fact]
    public void ASecondCopyOfTheSameKindBumpsTheCountRatherThanAddingASlot() {
        RuntimeContainer pack = Pack(), ring = Ring();
        ObjectInfoSet objects = Objects();

        InventoryAcquire.TryGive(pack, new RuntimeItem(RoyalKeyOfKrondor, 1, 0), objects, ring);
        InventoryAcquire.TryGive(pack, new RuntimeItem(RoyalKeyOfKrondor, 1, 0), objects, ring);

        Assert.Equal(2, Assert.Single(ring.Items).Variable);
    }

    [Fact]
    public void WithNoRingTheKeyFallsBackToThePack() {
        // A save that carries no SharedKeys container gets the old behaviour rather than losing the
        // item — the diversion is disabled by a null, the way InventoryTransfer's is.
        RuntimeContainer pack = Pack();

        Assert.True(InventoryAcquire.TryGive(
            pack, new RuntimeItem(RoyalKeyOfKrondor, 1, 0), Objects(), sharedKeys: null));

        Assert.Equal(RoyalKeyOfKrondor, Assert.Single(pack.Items).ObjectId);
    }

    [Fact]
    public void EverythingThatIsNotAKeyStillGoesToThePack() {
        RuntimeContainer pack = Pack(), ring = Ring();

        Assert.True(InventoryAcquire.TryGive(pack, new RuntimeItem(Broadsword, 100, 0), Objects(), ring));

        Assert.Equal(Broadsword, Assert.Single(pack.Items).ObjectId);
        Assert.Empty(ring.Items);
    }
}
