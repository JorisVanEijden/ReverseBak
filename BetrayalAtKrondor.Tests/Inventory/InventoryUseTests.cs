namespace BetrayalAtKrondor.Tests.Inventory;
using GameData;
using GameData.Resources.Data;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The item-on-item half of <c>Use_Item</c> — <c>itemuse_dispatch_on_target</c> @0x58cbd
/// (ITEMUSE.C:169-260, 428-440) plus the common tail at ITEMUSE.C:485-505. Spec:
/// docs/specs/inventory-item-handling.md §17.
///
/// <para>Every branch is keyed on <b>real object ids</b> (Coltari Poison 105, Quarrels 36-38,
/// Crystal Staff 1, Shell 16, …), so the ids are part of what these tests pin: a dispatch that
/// matched on a category alone would poison the wrong things.</para>
/// </summary>
public class InventoryUseTests {
    // Object ids, all from OBJINFO.DAT (generated/ObjectInfo/objinfo.json).
    private const byte CrystalStaff = 1, RawManna = 14, Shell = 16, GuardaRevanche = 22,
        ExoticSword = 23, Quarrels = 36, TsuraniQuarrels = 38, PoisonedQuarrels = 39,
        BessyMauler = 32, LightCrossbow = 33, TsuraniHeavy = 34, Rations = 72,
        PoisonedRations = 73, HeavyBowstring = 76, LightBowstring = 77, Whetstone = 85,
        ArmorersHammer = 75, AlthafainsIcer = 103, ClericalOilcloth = 104, ColtariPoison = 105,
        DragonStone = 106, Silverthorn = 112, Broadsword = 18, DragonPlate = 44, DalatailMilk = 114;

    // ItemSlot.flags bits the branches read and write (spec §1).
    private const ushort Broken = (ushort)ItemFlags.Broken;          // 0x10
    private const ushort Repairable = (ushort)ItemFlags.Repairable;  // 0x20
    private const ushort Equipped = (ushort)ItemFlags.Equipped;      // 0x40
    private const ushort Poisoned = (ushort)ItemFlags.Poisoned;      // 0x80
    private const ushort Frosted = (ushort)ItemFlags.Frosted;        // 0x400
    private const ushort Enhanced1 = (ushort)ItemFlags.Enhanced1;    // 0x800
    private const ushort CoatingMask = 0xE07F;                       // wEffect_arg_b on most coatings

    private const int UsedRecord = 1800002;      // 0x1B7742, the tail's "applied" record
    private const int NoEffectRecord = 1800003;  // 0x1B7743, the tail's outcome-0 record
    private const int NoRepairRecord = 1800030;  // 0x1B775E, "it needs no repair"

    // wEffect_arg_a / wEffect_arg_b live in ObjectInfo.Attributes / .UseEffectAttributeMask; the
    // former is typed ActorAttributeFlag, which for these categories is an ItemFlags set-mask or a
    // bare category number (see InventoryUse's remarks). Cast in, so the tests read as the record.
    private static ObjectInfo Obj(byte id, ObjectType type, ObjectFlags flags = 0,
        int argA = 0, int argB = 0) =>
        new ObjectInfo("O") {
            Number = id, Name = "obj" + id, ObjectType = type, Flags = flags,
            Attributes = (ActorAttributeFlag)argA, UseEffectAttributeMask = argB,
            InventorySlots = 1, MaxAmount = 1,
        };

    private static ObjectInfoSet Objs() => new ObjectInfoSet("O", new List<ObjectInfo> {
        Obj(CrystalStaff, ObjectType.Staff),
        Obj(RawManna, ObjectType.Usable, ObjectFlags.Stackable | ObjectFlags.B8000),
        Obj(Shell, ObjectType.Usable),
        Obj(GuardaRevanche, ObjectType.Sword),
        Obj(ExoticSword, ObjectType.Misc),
        Obj(Quarrels, ObjectType.Misc), Obj(37, ObjectType.Misc), Obj(TsuraniQuarrels, ObjectType.Misc),
        Obj(PoisonedQuarrels, ObjectType.Misc),
        Obj(BessyMauler, ObjectType.Crossbow), Obj(LightCrossbow, ObjectType.Crossbow),
        Obj(TsuraniHeavy, ObjectType.Crossbow),
        Obj(Rations, ObjectType.Food), Obj(PoisonedRations, ObjectType.Food),
        Obj(HeavyBowstring, ObjectType.BowString, ObjectFlags.ConsumedOnUse),
        Obj(LightBowstring, ObjectType.BowString, ObjectFlags.ConsumedOnUse),
        // Whetstone: arg_a is the target CATEGORY (1 = Sword), not an attribute.
        Obj(Whetstone, ObjectType.Repair, ObjectFlags.DiscardWhenEmpty | ObjectFlags.LimitedUses, argA: 1),
        Obj(ArmorersHammer, ObjectType.Repair, ObjectFlags.DiscardWhenEmpty | ObjectFlags.LimitedUses, argA: 4),
        Obj(AlthafainsIcer, ObjectType.Poison, ObjectFlags.DiscardWhenEmpty | ObjectFlags.LimitedUses,
            argA: Frosted, argB: CoatingMask),
        Obj(ClericalOilcloth, ObjectType.ClericalEnhancer, ObjectFlags.DiscardWhenEmpty | ObjectFlags.LimitedUses,
            argA: Enhanced1, argB: CoatingMask),
        Obj(ColtariPoison, ObjectType.Poison,
            ObjectFlags.DiscardWhenEmpty | ObjectFlags.Stackable | ObjectFlags.LimitedUses, argA: Poisoned),
        Obj(DragonStone, ObjectType.Enhancer, ObjectFlags.DiscardWhenEmpty | ObjectFlags.LimitedUses,
            argA: 0x200, argB: CoatingMask),
        Obj(Silverthorn, ObjectType.Poison,
            ObjectFlags.DiscardWhenEmpty | ObjectFlags.Stackable | ObjectFlags.LimitedUses,
            argA: Poisoned, argB: CoatingMask),
        Obj(Broadsword, ObjectType.Sword), Obj(DragonPlate, ObjectType.Armor),
        Obj(DalatailMilk, ObjectType.Potion),
    });

    private static RuntimeContainer Member(params RuntimeItem[] items) {
        var c = new RuntimeContainer { Capacity = 24, ContainerType = SaveGameContainerType.Inventory };
        c.Items.AddRange(items);
        return c;
    }

    private static RuntimeItem It(byte id, byte variable = 100, ushort flags = 0) =>
        new RuntimeItem(id, variable, flags);

    private static ItemUseResult Use(RuntimeContainer c, int source, int target) =>
        InventoryUse.Use(c, source, target, Objs());

    // --- category 9, poisons (ITEMUSE.C:169-186) ---

    [Fact]
    public void ColtariPoison_OnRations_RewritesThemToThePoisonedRations() {
        RuntimeContainer c = Member(It(ColtariPoison, 6), It(Rations, 14));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(PoisonedRations, c.Items[1].ObjectId);
        Assert.Equal(ItemUseOutcome.Handled, r.Outcome); // -1: no "you use it" text
        Assert.Equal(0, r.DialogId);
        Assert.True(c.Dirty);
    }

    // The 'i' leaf is exclusive: Coltari on anything but food does nothing at all, it does not
    // fall through to the blade-coating leaf below it.
    [Fact]
    public void ColtariPoison_OnASword_DoesNothing() { // and cannot poison quarrels either
        RuntimeContainer c = Member(It(ColtariPoison, 6), It(Broadsword));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(0, c.Items[1].ItemFlags);
        Assert.Equal(ItemUseOutcome.NoEffect, r.Outcome);
        Assert.Equal(NoEffectRecord, r.DialogId);
        Assert.Equal(ColtariPoison, r.DialogVar0);
    }

    // Silverthorn, not Coltari: the 'i' leaf above is exclusive, so the one poison that turns
    // rations is also the one poison that cannot touch quarrels.
    [Theory]
    [InlineData(Quarrels, PoisonedQuarrels)]
    [InlineData(TsuraniQuarrels, 41)]
    public void APoisonCarryingThePoisonedBit_ShiftsQuarrelsToTheirPoisonedTwin(byte from, byte to) {
        RuntimeContainer c = Member(It(Silverthorn, 8), It(from, 25));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(to, c.Items[1].ObjectId);
        Assert.Equal(25, c.Items[1].Variable); // the stack survives the swap
        Assert.Equal(ItemUseOutcome.Handled, r.Outcome);
    }

    // Althafain's Icer has arg_a = Frosted, not Poisoned, so it is not a quarrel poison.
    [Fact]
    public void APoisonWithoutThePoisonedBit_LeavesQuarrelsAlone() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(Quarrels, 25));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(Quarrels, c.Items[1].ObjectId);
        Assert.Equal(ItemUseOutcome.NoEffect, r.Outcome);
    }

    [Fact]
    public void AlthafainsIcer_CoatsABlade_ReplacingAnyOtherCoating() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(Broadsword, 100, (ushort)(Equipped | Poisoned)));
        ItemUseResult r = Use(c, 0, 1);
        // flags &= 0xE07F (drops the old coating, keeps Equipped) then |= Frosted.
        Assert.Equal((ushort)(Equipped | Frosted), c.Items[1].ItemFlags);
        Assert.Equal(ItemUseOutcome.Applied, r.Outcome);
        Assert.Equal(UsedRecord, r.DialogId);
    }

    [Fact]
    public void APoisonDoesNotCoatArmor() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(DragonPlate));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    // --- category 10 antidotes / enhancers, target half (ITEMUSE.C:188-206) ---

    [Fact]
    public void AnEnhancer_CoatsArmorButNotABlade() {
        RuntimeContainer c = Member(It(DragonStone, 6), It(DragonPlate), It(Broadsword));
        Assert.Equal(ItemUseOutcome.Applied, Use(c, 0, 1).Outcome);
        Assert.Equal(0x200, c.Items[1].ItemFlags);
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 2).Outcome);
    }

    // --- category 11 clerical enhancers (ITEMUSE.C:208-217) ---

    [Theory]
    [InlineData(Broadsword)]
    [InlineData(DragonPlate)]
    public void AClericalEnhancer_CoatsBothBladesAndArmor(byte target) {
        RuntimeContainer c = Member(It(ClericalOilcloth, 4), It(target));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(Enhanced1, c.Items[1].ItemFlags);
        Assert.Equal(ItemUseOutcome.Applied, r.Outcome);
    }

    [Fact]
    public void AClericalEnhancer_DoesNothingToACrossbow() {
        RuntimeContainer c = Member(It(ClericalOilcloth, 4), It(LightCrossbow));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    // --- category 12 bowstrings (ITEMUSE.C:255-272) ---

    [Fact]
    public void ALightBowstring_RestringsALightCrossbowAndClearsItsWear() {
        RuntimeContainer c = Member(It(LightBowstring), It(LightCrossbow, 40, (ushort)(Broken | Repairable | Equipped)));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(100, c.Items[0].Variable); // the list closed up: the crossbow moved to slot 0
        Assert.Equal(Equipped, c.Items[0].ItemFlags);
        Assert.Equal(ItemUseOutcome.Applied, r.Outcome);
        Assert.True(r.SourceRemoved); // ConsumedOnUse: fitting a string spends it
        Assert.Single(c.Items);
    }

    [Theory]
    [InlineData(LightBowstring, BessyMauler, false)]
    [InlineData(LightBowstring, TsuraniHeavy, false)]
    [InlineData(HeavyBowstring, BessyMauler, true)]
    [InlineData(HeavyBowstring, TsuraniHeavy, true)]
    [InlineData(HeavyBowstring, LightCrossbow, false)]
    public void BowstringWeightMustMatchTheCrossbow(byte str, byte bow, bool fits) {
        RuntimeContainer c = Member(It(str), It(bow, 40));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(fits ? ItemUseOutcome.Applied : ItemUseOutcome.NoEffect, r.Outcome);
    }

    [Fact]
    public void ABowstringDoesNothingToANonCrossbow() {
        RuntimeContainer c = Member(It(LightBowstring), It(Broadsword));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    // --- category 8 repair kits (ITEMUSE.C:219-241) ---

    [Fact]
    public void AWhetstoneOnASwordThatNeedsNoRepair_SaysSo() {
        RuntimeContainer c = Member(It(Whetstone, 20), It(Broadsword, 90));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(NoRepairRecord, r.DialogId);
        Assert.Equal(Broadsword, r.DialogVar0); // the record names the TARGET, not the kit
        Assert.Equal(ItemUseOutcome.Handled, r.Outcome);
        Assert.Equal(20, c.Items[0].Variable); // refusal returns before the tail: no charge spent
    }

    // The actual repair reads the member's ArmorCraft/WeaponCraft through the stat runtime
    // (base + permanent + timed modifiers), which the remake has no model for yet.
    [Fact]
    public void RepairingSomethingRepairable_IsNotPortedYet() {
        RuntimeContainer c = Member(It(Whetstone, 20), It(Broadsword, 40, Repairable));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(ItemUseOutcome.NotPorted, r.Outcome);
        Assert.Equal(0, r.DialogId);
        Assert.Equal(40, c.Items[1].Variable);
    }

    [Fact]
    public void AWhetstoneDoesNotTouchArmor_AndAHammerDoesNotTouchBlades() {
        RuntimeContainer c = Member(It(Whetstone, 20), It(DragonPlate, 40, Repairable));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
        RuntimeContainer d = Member(It(ArmorersHammer, 20), It(Broadsword, 40, Repairable));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(d, 0, 1).Outcome);
    }

    // Broken (0x10) is past repairing — the whole branch is skipped, so it reads as "no effect".
    [Fact]
    public void ABrokenItemCannotBeRepaired() {
        RuntimeContainer c = Member(It(Whetstone, 20), It(Broadsword, 0, (ushort)(Broken | Repairable)));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    // --- category 25, the two target-directed specials (ITEMUSE.C:428-459) ---

    [Fact]
    public void RawManna_TopsUpTheCrystalStaff_AndIsSpentWhole() {
        RuntimeContainer c = Member(It(RawManna, 20), It(CrystalStaff, 50));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(70, c.Items[0].Variable); // the staff moved into slot 0 when the manna went
        Assert.True(r.SourceRemoved);
        Assert.Equal(UsedRecord, r.DialogId);
        Assert.Equal(ItemUseOutcome.Handled, r.Outcome);
    }

    [Fact]
    public void RawManna_OnlySpendsWhatTheStaffCanHold() {
        RuntimeContainer c = Member(It(RawManna, 80), It(CrystalStaff, 50));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(30, c.Items[0].Variable);  // 80 - (100 - 50)
        Assert.Equal(100, c.Items[1].Variable);
        Assert.False(r.SourceRemoved);
        Assert.Equal(UsedRecord, r.DialogId);
    }

    [Fact]
    public void RawManna_OnAFullStaff_DoesNothingAndSaysNothing() {
        RuntimeContainer c = Member(It(RawManna, 20), It(CrystalStaff, 100));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(20, c.Items[0].Variable);
        Assert.Equal(0, r.DialogId);
        Assert.Equal(ItemUseOutcome.Handled, r.Outcome);
    }

    [Fact]
    public void RawManna_OnAnythingElse_HasNoEffect() {
        RuntimeContainer c = Member(It(RawManna, 20), It(Broadsword));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    [Fact]
    public void TheShell_TurnsEveryExoticSwordInTheInventoryIntoAGuardaRevanche() {
        RuntimeContainer c = Member(It(Shell), It(ExoticSword), It(Broadsword), It(ExoticSword));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(GuardaRevanche, c.Items[1].ObjectId);
        Assert.Equal(GuardaRevanche, c.Items[3].ObjectId);
        Assert.Equal(ItemUseOutcome.Applied, r.Outcome);
        Assert.Equal(UsedRecord, r.DialogId);
    }

    [Fact]
    public void TheShell_WithNoExoticSwordAround_HasNoEffect() {
        RuntimeContainer c = Member(It(Shell), It(Broadsword));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 1).Outcome);
    }

    // --- categories with no port yet, and the no-target form ---

    [Fact]
    public void ACategoryWithNoPortedBranch_ReportsNotPorted_NotNoEffect() {
        // A potion is category 0x12, a real branch in the original (the timed-modifier table).
        // Reporting "nothing happens" would be a lie the player can see.
        RuntimeContainer c = Member(It(DalatailMilk), It(Broadsword));
        ItemUseResult r = Use(c, 0, 1);
        Assert.Equal(ItemUseOutcome.NotPorted, r.Outcome);
        Assert.Equal(0, r.DialogId);
    }

    [Fact]
    public void UsingATargetedCategoryWithNoTarget_HasNoEffect() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(Broadsword));
        ItemUseResult r = InventoryUse.Use(c, 0, InventoryUse.NoTarget, Objs());
        Assert.Equal(ItemUseOutcome.NoEffect, r.Outcome);
        Assert.Equal(NoEffectRecord, r.DialogId);
    }

    [Fact]
    public void AnItemIsNeverUsedOnItself() {
        RuntimeContainer c = Member(It(ColtariPoison, 6));
        Assert.Equal(ItemUseOutcome.NoEffect, Use(c, 0, 0).Outcome);
    }

    // --- the common tail's consumption rules (ITEMUSE.C:490-503) ---

    [Fact]
    public void AChargeBearingItemSpendsOneChargePerUse() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(Broadsword));
        Use(c, 0, 1);
        Assert.Equal(7, c.Items[0].Variable);
    }

    [Fact]
    public void ItsLastChargeRemovesIt_WhenTheRecordSaysDiscardWhenEmpty() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 1), It(Broadsword));
        ItemUseResult r = Use(c, 0, 1);
        Assert.True(r.SourceRemoved);
        Assert.Single(c.Items);
    }

    [Fact]
    public void ItsLastChargeOnlyEmptiesIt_WhenTheRecordDoesNot() {
        var objects = new ObjectInfoSet("O", new List<ObjectInfo> {
            Obj(AlthafainsIcer, ObjectType.Poison, ObjectFlags.LimitedUses, Frosted, CoatingMask),
            Obj(Broadsword, ObjectType.Sword),
        });
        RuntimeContainer c = Member(It(AlthafainsIcer, 1), It(Broadsword));
        ItemUseResult r = InventoryUse.Use(c, 0, 1, objects);
        Assert.False(r.SourceRemoved);
        Assert.Equal(0, c.Items[0].Variable);
    }

    [Fact]
    public void NothingIsSpentWhenNothingHappened() {
        RuntimeContainer c = Member(It(AlthafainsIcer, 8), It(DragonPlate));
        Use(c, 0, 1);
        Assert.Equal(8, c.Items[0].Variable);
        Assert.False(c.Dirty);
    }

    // --- the drag gesture's source filter (INVENTOR.C:797-806) ---

    [Theory]
    [InlineData(ObjectType.Repair, true)]
    [InlineData(ObjectType.Poison, true)]
    [InlineData(ObjectType.Enhancer, true)]
    [InlineData(ObjectType.ClericalEnhancer, true)]
    [InlineData(ObjectType.BowString, true)]
    [InlineData(ObjectType.Usable, true)]
    [InlineData(ObjectType.Sword, false)]
    [InlineData(ObjectType.Key, false)]
    [InlineData(ObjectType.Potion, false)]
    [InlineData(ObjectType.MagicalScroll, false)]
    public void OnlyCategories8To12AndUsableCanBeDraggedOntoAnotherItem(ObjectType type, bool can) =>
        Assert.Equal(can, InventoryUse.CanUseOnAnotherItem(type));
}
