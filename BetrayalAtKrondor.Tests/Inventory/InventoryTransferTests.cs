namespace BetrayalAtKrondor.Tests.Inventory;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

public class InventoryTransferTests {
    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("p") { Number = 80, Name = "Picklocks", InventorySlots = 1, MaxAmount = 5 },
        new ObjectInfo("g") { Number = 53, Name = "Gold Sovereigns", InventorySlots = 1, MaxAmount = 1000 },
    });
    private static RuntimeContainer C(int cap, int type, params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = cap, ContainerType = type };
        c.Items.AddRange(items); return c;
    }

    [Fact] public void NormalMove_TransfersAndRemovesFromSource() {
        var src = C(4, 5, new RuntimeItem(80, 2, 0));
        var dst = C(24, 1);
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(src.Items);
        Assert.Single(dst.Items);
        Assert.Equal(80, dst.Items[0].ObjectId);
        Assert.True(src.Dirty && dst.Dirty);
    }

    [Fact] public void GoldSovereign_ConvertsToPartyGold_x10() {
        var src = C(4, 5, new RuntimeItem(53, 7, 0)); // 7 sovereigns
        var dst = C(24, 1);
        int gold = 100;
        var r = InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.GoldConverted, r);
        Assert.Equal(100 + 7 * 10, gold);
        Assert.Empty(src.Items);
        Assert.Empty(dst.Items); // no currency item on the character
    }

    [Fact] public void Stackable_MergesUpToMaxAmount() {
        var src = C(4, 5, new RuntimeItem(80, 2, 0));
        var dst = C(24, 1, new RuntimeItem(80, 1, 0)); // already has 1
        int gold = 0;
        InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Single(dst.Items);
        Assert.Equal(3, dst.Items[0].Variable); // 1 + 2
        Assert.Empty(src.Items);
    }

    [Fact] public void DoesNotFit_WhenAtCapacityCount() {
        var dst = C(1, 1, new RuntimeItem(72, 1, 0)); // capacity 1, already full
        var src = C(4, 5, new RuntimeItem(80, 2, 0));
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.DoesNotFit, r);
        Assert.Single(src.Items); // unchanged
    }

    // Regression: the old single-sum-with-slack CanFit allowed 20+1 <= 20+4, over-filling a
    // character inventory. The real two-pass rule requires pass-2 total (all footprints, no
    // slack) <= budget, so a 21st single-slot item must be rejected even though capacity (25)
    // and the old slack check both had room.
    [Fact] public void DoesNotFit_WhenSlotBudgetExceeded_NoSlackOnPass2() {
        var objList = new List<ObjectInfo>();
        var items = new RuntimeItem[20];
        for (int i = 0; i < 20; i++) {
            byte id = (byte)(100 + i);
            objList.Add(new ObjectInfo("x" + i) { Number = id, Name = "Trinket" + i, InventorySlots = 1, MaxAmount = 1 });
            items[i] = new RuntimeItem(id, 1, 0);
        }
        // Incoming 21st single-slot item, distinct object id so it can't stack with any of the 20.
        objList.Add(new ObjectInfo("x20") { Number = 120, Name = "Trinket20", InventorySlots = 1, MaxAmount = 1 });
        var objs = new ObjectInfoSet("O", objList);

        var dst = C(25, 1, items); // character inventory, capacity 25, already holding 20 single-slot items
        var src = C(4, 5, new RuntimeItem(120, 1, 0));
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, objs, ref gold);
        Assert.Equal(InventoryTransfer.Result.DoesNotFit, r);
        Assert.Single(src.Items); // unchanged
        Assert.Equal(20, dst.Items.Count); // unchanged
    }
}
