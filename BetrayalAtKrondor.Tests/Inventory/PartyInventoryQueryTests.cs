namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The party-wide questions the dialog gates ask — <c>evtcond_pty_inv_repair_cnt</c> and the item
/// checks that share <see cref="InventoryQuery.CountByKind"/>'s reading of "carries".
/// </summary>
public class PartyInventoryQueryTests {
    private const byte Breastplate = 44, Helmet = 45, Sword = 20, Waani = 0x65;

    private static ObjectInfoSet Objects() => new ObjectInfoSet("O", new List<ObjectInfo> {
        new ObjectInfo("O") { Number = Breastplate, Name = "plate", ObjectType = ObjectType.Armor },
        new ObjectInfo("O") { Number = Helmet, Name = "helm", ObjectType = ObjectType.Armor },
        new ObjectInfo("O") { Number = Sword, Name = "sword", ObjectType = ObjectType.Sword },
        new ObjectInfo("O") { Number = Waani, Name = "waani", ObjectType = ObjectType.Misc },
    });

    private static RuntimeContainer Pack(params (byte Id, byte Variable, ushort Flags)[] items) {
        var c = new RuntimeContainer();
        foreach ((byte id, byte v, ushort f) in items) {
            c.Items.Add(new RuntimeItem(id, v, f));
        }
        return c;
    }

    [Fact]
    public void AnyHoldsAsksTheWHOLEParty() {
        var packs = new List<RuntimeContainer> {
            Pack((Sword, 100, 0)),
            Pack((Waani, 1, 0)),
        };

        Assert.True(InventoryQuery.AnyHolds(packs, Waani));
        Assert.False(InventoryQuery.AnyHolds(packs, Helmet));
        Assert.False(InventoryQuery.AnyHolds(null, Waani));
    }

    [Fact]
    public void ASpentStackReadsAsPRESENT_notAbsent() {
        // *** The trap. *** CountByKind's own doc claimed a run-out stack "answers 0 and reads as
        // absent" until 2026-08-25; the original increments by ONE on condition == 0
        // (ITEMTBL.C:99-103), so it reads as present. I wrote this test the wrong way round from
        // that sentence and the code failed it — which is how the doc got corrected.
        //
        // It is also why QuarrelInventory.Count exists separately: ammunition genuinely must answer
        // 0 for an empty quiver, and it cannot reuse this.
        Assert.True(InventoryQuery.AnyHolds(new[] { Pack((Waani, 0, 0)) }, Waani));
        Assert.Equal(1, InventoryQuery.CountByKind(Pack((Waani, 0, 0)), Waani));
    }

    [Fact]
    public void DamagedArmourIsCountedACROSSTheParty() {
        var packs = new List<RuntimeContainer> {
            Pack((Breastplate, 100, 0)),          // pristine
            Pack((Helmet, 40, 0), (Sword, 5, 0)), // one dented helm; the sword is not armour
        };

        Assert.Equal(1, InventoryQuery.CountNeedingRepair(packs, Objects()));
    }

    [Fact]
    public void EQUIPPEDORNOT_aSpareInThePackCountsToo() {
        // *** The routine tests only the category and the condition. *** There is no equipped check,
        // unlike the combat wear routine next door — filtering to worn gear would hide the topic
        // from a party carrying a sack of dented armour.
        var worn = Pack((Breastplate, 40, (ushort)ItemFlags.Equipped));
        var spare = Pack((Breastplate, 40, 0));

        Assert.Equal(1, InventoryQuery.CountNeedingRepair(new[] { worn }, Objects()));
        Assert.Equal(1, InventoryQuery.CountNeedingRepair(new[] { spare }, Objects()));
    }

    [Fact]
    public void PRISTINEIsNotDamaged_theTestIsStrictlyBelow100() {
        Assert.Equal(0, InventoryQuery.CountNeedingRepair(
            new[] { Pack((Breastplate, InventoryQuery.PristineCondition, 0)) }, Objects()));
        Assert.Equal(1, InventoryQuery.CountNeedingRepair(
            new[] { Pack((Breastplate, InventoryQuery.PristineCondition - 1, 0)) }, Objects()));
    }

    [Fact]
    public void WithNoObjectTableNothingIsCounted() {
        // The category cannot be known without it, and guessing would report armour where there is
        // none. Answering 0 makes the gate fall back to the general rule.
        Assert.Equal(0, InventoryQuery.CountNeedingRepair(
            new[] { Pack((Breastplate, 10, 0)) }, null));
    }
}
