namespace BetrayalAtKrondor.Tests.Inventory;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>Tests for the cmbinv_consolidate_stacks / cmbinv_combat_sort_initiative port
/// (CMBINV.C:570/545). Rations = flags 0x8800 (Stackable + count display), MaxAmount 14.</summary>
public class InventoryOrderTests {
    private const ushort Equipped = (ushort)GameData.ItemFlags.Equipped;

    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("rations") { Number = 72, Name = "Rations", InventorySlots = 1, MaxAmount = 14, Flags = (ObjectFlags)0x8800 },
        new ObjectInfo("staff") { Number = 9, Name = "Staff", InventorySlots = 4, ObjectType = GameData.ObjectType.Staff },
        new ObjectInfo("armor") { Number = 20, Name = "Armor", InventorySlots = 4, ObjectType = GameData.ObjectType.Armor },
        new ObjectInfo("torch") { Number = 84, Name = "Torch", InventorySlots = 1, MaxAmount = 25, Flags = (ObjectFlags)0x2810 },
    });

    private static RuntimeContainer C(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Inventory };
        c.Items.AddRange(items); return c;
    }

    [Fact] public void Consolidate_MergesPartialStacksOfSameItem() {
        // The Owyn bug: two rations stacks (4 + 7) must merge into one stack of 11.
        var c = C(new RuntimeItem(72, 4, 0), new RuntimeItem(72, 7, 0));
        bool changed = InventoryOrder.Consolidate(c, Objs(), equippedOrder: true);
        Assert.True(changed);
        Assert.Single(c.Items);
        Assert.Equal(11, c.Items[0].Variable);
    }

    [Fact] public void Consolidate_OverfullMerge_SplitsIntoMaxAndRemainder() {
        // 10 + 9 vs MaxAmount 14 → a full stack of 14 plus a remainder of 5 (CMBINV.C:603-604).
        var c = C(new RuntimeItem(72, 10, 0), new RuntimeItem(72, 9, 0));
        InventoryOrder.Consolidate(c, Objs(), equippedOrder: true);
        Assert.Equal(2, c.Items.Count);
        Assert.Contains(c.Items, i => i.Variable == 14);
        Assert.Contains(c.Items, i => i.Variable == 5);
    }

    [Fact] public void Consolidate_NonStackable_Untouched() {
        var c = C(new RuntimeItem(9, 1, 0), new RuntimeItem(9, 1, 0));
        InventoryOrder.Consolidate(c, Objs(), equippedOrder: true);
        Assert.Equal(2, c.Items.Count);
    }

    [Fact] public void Sort_EquippedOrder_FloatsEquippedByCategoryDescending() {
        // armor(4) before staff(3) before everything unequipped, regardless of list order.
        var c = C(new RuntimeItem(72, 4, 0), new RuntimeItem(9, 1, Equipped), new RuntimeItem(20, 1, Equipped));
        InventoryOrder.Sort(c, Objs(), equippedOrder: true);
        Assert.Equal(20, c.Items[0].ObjectId); // armor, category 4
        Assert.Equal(9, c.Items[1].ObjectId);  // staff, category 3
        Assert.Equal(72, c.Items[2].ObjectId); // unequipped last
    }

    [Fact] public void Sort_SameFootprint_AscendingIdThenAscendingCount() {
        var c = C(new RuntimeItem(84, 3, 0), new RuntimeItem(72, 9, 0), new RuntimeItem(72, 2, 0));
        InventoryOrder.Sort(c, Objs(), equippedOrder: false);
        Assert.Equal(72, c.Items[0].ObjectId);
        Assert.Equal(2, c.Items[0].Variable);
        Assert.Equal(9, c.Items[1].Variable);
        Assert.Equal(84, c.Items[2].ObjectId);
    }
}
