namespace BetrayalAtKrondor.Tests.Inventory;
using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Pins the equip-first rules from docs/specs/inventory-item-handling.md §2-§3
/// (CanEquip @0x55fe2, canAutoEquipOnPickup @0x55414, transferItem @0x5555e,
/// Use_Item @0x58cbd equip branch).
/// </summary>
public class InventoryEquipTests {
    private const ushort Equipped = (ushort)ItemFlags.Equipped;
    private const byte SwordA = 1, SwordB = 2, Staff = 10, Crossbow = 32, Armor = 36, Trinket = 90;

    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("s1") { Number = SwordA, Name = "Broadsword", ObjectType = ObjectType.Sword, InventorySlots = 2, MaxAmount = 1 },
        new ObjectInfo("s2") { Number = SwordB, Name = "Rapier", ObjectType = ObjectType.Sword, InventorySlots = 2, MaxAmount = 1 },
        new ObjectInfo("st") { Number = Staff, Name = "Staff", ObjectType = ObjectType.Staff, InventorySlots = 4, MaxAmount = 1 },
        new ObjectInfo("cb") { Number = Crossbow, Name = "Crossbow", ObjectType = ObjectType.Crossbow, InventorySlots = 2, MaxAmount = 1 },
        new ObjectInfo("ar") { Number = Armor, Name = "Armor", ObjectType = ObjectType.Armor, InventorySlots = 4, MaxAmount = 1 },
        new ObjectInfo("tk") { Number = Trinket, Name = "Trinket", ObjectType = ObjectType.Misc, InventorySlots = 1, MaxAmount = 1 },
    });

    private static RuntimeContainer Member(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Inventory };
        c.Items.AddRange(items); return c;
    }
    private static RuntimeContainer Corpse(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Corpse };
        c.Items.AddRange(items); return c;
    }

    // --- CanEquip @0x55fe2 ---

    [Theory]
    [InlineData(ObjectType.Sword, false, true)]
    [InlineData(ObjectType.Crossbow, false, true)]
    [InlineData(ObjectType.Armor, false, true)]
    [InlineData(ObjectType.Staff, false, false)]
    [InlineData(ObjectType.Sword, true, false)]
    [InlineData(ObjectType.Crossbow, true, false)]
    [InlineData(ObjectType.Armor, true, true)]
    [InlineData(ObjectType.Staff, true, true)]
    [InlineData(ObjectType.Misc, false, false)]
    [InlineData(ObjectType.Key, true, false)]
    public void CanEquipCategory_Matrix(ObjectType type, bool caster, bool expected) =>
        Assert.Equal(expected, InventoryEquip.CanEquipCategory(type, caster));

    // --- Use_Item equip branch ---

    [Fact] public void Equip_ClearsSameCategoryOnly() {
        var m = Member(
            new RuntimeItem(SwordA, 100, Equipped),
            new RuntimeItem(Armor, 100, Equipped),
            new RuntimeItem(SwordB, 100, 0));
        InventoryEquip.Equip(m, 2, Objs());
        Assert.Equal(0, m.Items[0].ItemFlags & Equipped);        // old sword unequipped
        Assert.Equal(Equipped, m.Items[1].ItemFlags & Equipped); // armor untouched
        Assert.Equal(Equipped, m.Items[2].ItemFlags & Equipped); // new sword equipped
        Assert.True(m.Dirty);
    }

    [Fact] public void Equip_AlreadyEquipped_IsIdempotent() {
        var m = Member(new RuntimeItem(SwordA, 100, Equipped));
        InventoryEquip.Equip(m, 0, Objs());
        Assert.Equal(Equipped, m.Items[0].ItemFlags & Equipped);
    }

    // --- The equip-first invariant on arrival (canAutoEquipOnPickup @0x55414) ---

    [Fact] public void Sword_ToMemberWithEmptySwordSlot_AutoEquips() {
        var src = Corpse(new RuntimeItem(SwordA, 100, 0));
        var dst = Member();
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Equal(Equipped, dst.Items[0].ItemFlags & Equipped); // landed on the paperdoll
    }

    [Fact] public void Sword_ToMemberAlreadyArmed_ArrivesUnequipped() {
        var src = Corpse(new RuntimeItem(SwordB, 100, 0));
        var dst = Member(new RuntimeItem(SwordA, 100, Equipped));
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Equal(Equipped, dst.Items[0].ItemFlags & Equipped); // old sword still equipped
        Assert.Equal(0, dst.Items[1].ItemFlags & Equipped);        // spare goes to the grid
    }

    [Fact] public void Sword_ToCaster_ArrivesUnequipped() {
        var src = Corpse(new RuntimeItem(SwordA, 100, 0));
        var dst = Member();
        int gold = 0;
        InventoryTransfer.Move(src, 0, dst, Objs(), ref gold, targetIsCaster: true);
        Assert.Equal(0, dst.Items[0].ItemFlags & Equipped); // casters never equip swords
    }

    [Fact] public void Armor_AutoEquips_EvenWhenGridBudgetIsFull() {
        // 20 single-slot trinkets exhaust the member footprint budget; the armor still lands,
        // equipped, because classification runs before the fit test and equipped items sit
        // outside the budget (classifyItemPickup @0x55482).
        var items = new List<ObjectInfo>();
        var dst = Member();
        for (int i = 0; i < 20; i++) {
            byte id = (byte)(100 + i);
            items.Add(new ObjectInfo("x" + i) { Number = id, Name = "T" + i, ObjectType = ObjectType.Misc, InventorySlots = 1, MaxAmount = 1 });
            dst.Items.Add(new RuntimeItem(id, 1, 0));
        }
        items.Add(new ObjectInfo("ar") { Number = Armor, Name = "Armor", ObjectType = ObjectType.Armor, InventorySlots = 4, MaxAmount = 1 });
        var objs = new ObjectInfoSet("O", items);
        var src = Corpse(new RuntimeItem(Armor, 100, 0));
        int gold = 0;
        var r = InventoryTransfer.Move(src, 0, dst, objs, ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Equal(Equipped, dst.Items[20].ItemFlags & Equipped);
    }

    // --- Equipped cat-1/3: swap-only (transferItem @0x5555e) ---

    [Fact] public void EquippedSword_ToMemberWithEquippedSword_SwapsInPlace() {
        var a = Member(new RuntimeItem(SwordA, 100, Equipped));
        var b = Member(new RuntimeItem(SwordB, 50, Equipped));
        int gold = 0;
        var r = InventoryTransfer.Move(a, 0, b, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.SwappedEquipped, r);
        Assert.Equal(SwordB, a.Items[0].ObjectId);
        Assert.Equal(SwordA, b.Items[0].ObjectId);
        Assert.Equal(Equipped, a.Items[0].ItemFlags & Equipped); // both stay equipped
        Assert.Equal(Equipped, b.Items[0].ItemFlags & Equipped);
    }

    [Fact] public void EquippedSword_ToCorpse_Refused() {
        var a = Member(new RuntimeItem(SwordA, 100, Equipped));
        var corpse = Corpse();
        int gold = 0;
        var r = InventoryTransfer.Move(a, 0, corpse, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.MustKeepEquipped, r);
        Assert.Single(a.Items);   // nothing moved
        Assert.Empty(corpse.Items);
    }

    [Fact] public void EquippedSword_ToMemberWithoutEquippedSword_Refused() {
        // A caster holds a staff, not a sword — no same-category swap partner (CMBINV.C:762).
        var a = Member(new RuntimeItem(SwordA, 100, Equipped));
        var caster = Member(new RuntimeItem(Staff, 100, Equipped));
        int gold = 0;
        var r = InventoryTransfer.Move(a, 0, caster, Objs(), ref gold, targetIsCaster: true);
        Assert.Equal(InventoryTransfer.Result.MustKeepEquipped, r);
        Assert.Equal(SwordA, a.Items[0].ObjectId);
        Assert.Single(caster.Items);
    }

    // --- Equipped cat-2/4: removable, flag cleared, re-equips where empty ---

    [Fact] public void EquippedArmor_ToCorpse_MovesAndClearsFlag() {
        var a = Member(new RuntimeItem(Armor, 100, Equipped));
        var corpse = Corpse();
        int gold = 0;
        var r = InventoryTransfer.Move(a, 0, corpse, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(a.Items);
        Assert.Equal(0, corpse.Items[0].ItemFlags & Equipped);
    }

    [Fact] public void EquippedArmor_ToUnarmoredMember_ReEquipsThere() {
        var a = Member(new RuntimeItem(Armor, 100, Equipped));
        var b = Member();
        int gold = 0;
        var r = InventoryTransfer.Move(a, 0, b, Objs(), ref gold);
        Assert.Equal(InventoryTransfer.Result.Moved, r);
        Assert.Empty(a.Items);
        Assert.Equal(Equipped, b.Items[0].ItemFlags & Equipped);
    }
}
