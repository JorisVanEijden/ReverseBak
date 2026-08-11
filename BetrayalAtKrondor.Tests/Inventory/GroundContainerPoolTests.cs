namespace BetrayalAtKrondor.Tests.Inventory;

using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The ground-bag slot pool (<c>actorspawn_enc_location</c>, ACTSPAWN.C:170) — free-slot-first,
/// then least-recently-touched recycling with protected bags sorted last.
/// </summary>
public class GroundContainerPoolTests {
    private const SaveGameContainerDataType PoolFlags =
        SaveGameContainerDataType.Timestamp | SaveGameContainerDataType.SelfSpawn;

    private static RuntimeContainer Free(int timestamp = 0) => new RuntimeContainer {
        ContainerType = SaveGameContainerType.Free,
        Capacity = 20, Zone = 255, X = 0, Y = 0,
        DataTypes = PoolFlags, Timestamp = timestamp,
    };

    private static RuntimeContainer Bag(int timestamp, bool protectedItem = false) {
        var c = new RuntimeContainer {
            ContainerType = SaveGameContainerType.Bag,
            Capacity = 20, Zone = 3, X = 100, Y = 200,
            DataTypes = PoolFlags, Timestamp = timestamp,
        };
        if (protectedItem) {
            c.DataTypes |= SaveGameContainerDataType.HoldsProtectedItem;
        }
        c.Items.Add(new RuntimeItem(80, 1, 0));
        return c;
    }

    private static RuntimeContainer Chest() => new RuntimeContainer {
        ContainerType = SaveGameContainerType.Chest,
        Capacity = 6, Zone = 3, X = 1, Y = 2,
        DataTypes = SaveGameContainerDataType.Lock,
    };

    [Fact]
    public void SelectSlot_PrefersFirstFreeRecord_OverAnyBag() {
        var free = Free();
        var zone = new List<RuntimeContainer> { Bag(1), Chest(), free, Free() };

        RuntimeContainer picked = GroundContainerPool.SelectSlot(zone, out bool recycled);

        Assert.Same(free, picked);
        Assert.False(recycled);
    }

    [Fact]
    public void SelectSlot_NoFreeSlot_RecyclesLeastRecentlyTouchedBag() {
        var oldest = Bag(10);
        var zone = new List<RuntimeContainer> { Bag(50), oldest, Bag(30), Chest() };

        RuntimeContainer picked = GroundContainerPool.SelectSlot(zone, out bool recycled);

        Assert.Same(oldest, picked);
        Assert.True(recycled);
    }

    /// <summary>The engine ORs 0x80000000 into a protected bag's key and compares UNSIGNED, so a
    /// bag holding a quest item loses to every unprotected bag however stale it is.</summary>
    [Fact]
    public void SelectSlot_ProtectedBagIsRecycledLast_EvenWhenOldest() {
        var protectedOldest = Bag(1, protectedItem: true);
        var plainNewer = Bag(9999);
        var zone = new List<RuntimeContainer> { protectedOldest, plainNewer };

        RuntimeContainer picked = GroundContainerPool.SelectSlot(zone, out _);

        Assert.Same(plainNewer, picked);
    }

    [Fact]
    public void SelectSlot_AllBagsProtected_StillRecyclesTheOldestOfThem() {
        var oldest = Bag(5, protectedItem: true);
        var zone = new List<RuntimeContainer> { Bag(80, protectedItem: true), oldest };

        Assert.Same(oldest, GroundContainerPool.SelectSlot(zone, out _));
    }

    [Fact]
    public void SelectSlot_NoFreeAndNoSelfSpawnRecord_ReturnsNull() {
        var zone = new List<RuntimeContainer> { Chest() };

        Assert.Null(GroundContainerPool.SelectSlot(zone, out bool recycled));
        Assert.False(recycled);
    }

    [Fact]
    public void Claim_StampsBagIdentityAtTheGivenPosition() {
        var slot = Free();

        GroundContainerPool.Claim(slot, zone: 4, x: 123456, y: 654321, gameTime: 777);

        Assert.Equal(SaveGameContainerType.Bag, slot.ContainerType);
        Assert.Equal(4, slot.Zone);
        Assert.Equal(123456, slot.X);
        Assert.Equal(654321, slot.Y);
        Assert.Equal(GroundContainerPool.BagWorldItemId, slot.WorldItemId);
        Assert.Equal(GroundContainerPool.BagMinChapter, slot.MinChapter);
        Assert.Equal(GroundContainerPool.BagMaxChapter, slot.MaxChapter);
        Assert.Equal(777, slot.Timestamp);
        Assert.True(slot.DataTypes.HasFlag(SaveGameContainerDataType.SelfSpawn));
        Assert.True(slot.HeaderDirty);
        Assert.True(slot.Dirty);
    }

    [Fact]
    public void Claim_OnARecycledBag_DiscardsItsPreviousContents() {
        var recycledBag = Bag(1);
        Assert.NotEmpty(recycledBag.Items);

        GroundContainerPool.Claim(recycledBag, zone: 2, x: 5, y: 6, gameTime: 100);

        Assert.Empty(recycledBag.Items);
    }

    [Fact]
    public void ReleaseIfEmpty_FreesAnEmptiedBagBackToThePool() {
        var bag = Bag(1);
        bag.Items.Clear();

        Assert.True(GroundContainerPool.ReleaseIfEmpty(bag));
        Assert.Equal(SaveGameContainerType.Free, bag.ContainerType);
        Assert.True(bag.HeaderDirty);
    }

    [Fact]
    public void ReleaseIfEmpty_KeepsABagThatStillHoldsSomething() {
        var bag = Bag(1);

        Assert.False(GroundContainerPool.ReleaseIfEmpty(bag));
        Assert.Equal(SaveGameContainerType.Bag, bag.ContainerType);
    }

    /// <summary>A corpse is not self-spawning, so emptying it leaves a lootable-but-empty corpse
    /// rather than freeing the record.</summary>
    [Fact]
    public void ReleaseIfEmpty_IgnoresANonSelfSpawningContainer() {
        var corpse = new RuntimeContainer {
            ContainerType = SaveGameContainerType.Corpse, Capacity = 6, DataTypes = 0,
        };

        Assert.False(GroundContainerPool.ReleaseIfEmpty(corpse));
    }

    [Fact]
    public void RecomputeHoldsProtectedItem_SetsAndClearsFromTheObjectRecords() {
        var objects = new ObjectInfoSet("objinfo", new List<ObjectInfo> {
            new ObjectInfo("plain") { Number = 10, Flags = ObjectFlags.Stackable },
            new ObjectInfo("quest") { Number = 120, Flags = ObjectFlags.Protected },
        });
        var bag = Bag(1);
        bag.Items.Clear();
        bag.Items.Add(new RuntimeItem(10, 1, 0));

        GroundContainerPool.RecomputeHoldsProtectedItem(bag, objects);
        Assert.False(bag.DataTypes.HasFlag(SaveGameContainerDataType.HoldsProtectedItem));

        bag.Items.Add(new RuntimeItem(120, 1, 0));
        GroundContainerPool.RecomputeHoldsProtectedItem(bag, objects);
        Assert.True(bag.DataTypes.HasFlag(SaveGameContainerDataType.HoldsProtectedItem));

        bag.Items.RemoveAt(1);
        GroundContainerPool.RecomputeHoldsProtectedItem(bag, objects);
        Assert.False(bag.DataTypes.HasFlag(SaveGameContainerDataType.HoldsProtectedItem));
    }
}
