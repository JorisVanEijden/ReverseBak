namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Giving a container an item that came from nowhere — the dialog's "here, take this". The refusal
/// matters as much as the success: the original only charges for what was accepted.
/// </summary>
public class InventoryAcquireTests {
    private const byte Arrows = 36;

    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") {
            Number = Arrows, Name = "Quarrels", ObjectType = ObjectType.Misc,
            Flags = ObjectFlags.Stackable, InventorySlots = 1, MaxAmount = 40,
        },
    });

    private static RuntimeContainer Pack(int capacity, params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = capacity, ContainerType = SaveGameContainerType.Inventory };
        c.Items.AddRange(items);
        return c;
    }

    [Fact]
    public void AnItemWithRoomIsAccepted() {
        RuntimeContainer pack = Pack(8);

        Assert.True(InventoryAcquire.TryGive(pack, new RuntimeItem(Arrows, 10, 0), Objects()));
        Assert.Single(pack.Items);
        Assert.True(pack.Dirty);
    }

    [Fact]
    public void AGiftMergesIntoAStackAlreadyCarried() {
        RuntimeContainer pack = Pack(8, new RuntimeItem(Arrows, 10, 0));

        InventoryAcquire.TryGive(pack, new RuntimeItem(Arrows, 5, 0), Objects());

        Assert.Single(pack.Items);
        Assert.Equal(15, pack.Items[0].Variable);
    }

    [Fact]
    public void AFullPackRefusesTheGift() {
        // The caller must see this: the original charges only for an item that was accepted.
        var pack = Pack(0);

        Assert.False(InventoryAcquire.TryGive(pack, new RuntimeItem(Arrows, 1, 0), Objects()));
        Assert.Empty(pack.Items);
    }

    [Fact]
    public void NothingIsGivenToNothing() {
        Assert.False(InventoryAcquire.TryGive(null, new RuntimeItem(Arrows, 1, 0), Objects()));
        Assert.False(InventoryAcquire.TryGive(Pack(4), null, Objects()));
    }
}
