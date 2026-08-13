namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The permanent stat gain reading a book confers (ITEMUSE.C). The case that matters is the
/// asymmetry: the first read pays out in full with no roll, and every later read is a gated,
/// tapering consolation.
/// </summary>
public class ItemStatEffectsTests {
    private const int ScoutingBit = 1 << (int)ActorAttribute.Scouting;

    private static ActorStat[] Stats(byte scouting = 20, byte max = 100) {
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 0, Max = max };
        }
        stats[(int)ActorAttribute.Scouting] = new ActorStat { Base = scouting, Max = max };
        return stats;
    }

    private static ObjectInfo Book(int mask = ScoutingBit, int firstAmount = 3,
        int chancePercent = 50, int laterValue = 100) =>
        new ObjectInfo("book") {
            ObjectType = ObjectType.Book,
            EffectArgA = mask,
            EffectArgB = firstAmount,
            UseEffectAmount = chancePercent,      // +0x42 — wEffect_chance_pct in this category
            EffectDurationHours = laterValue,     // +0x44 — wEffect_stat_value in this category
        };

    private static RuntimeItem Item(byte objectId = 5, byte condition = 1) =>
        new RuntimeItem(objectId, condition, 0);

    private sealed class Flags {
        private readonly Dictionary<int, int> _values = new Dictionary<int, int>();

        public int Read(int key) => _values.TryGetValue(key, out int v) ? v : 0;

        public void Write(int key, int value) => _values[key] = value;
    }

    // ---- the flag key ----------------------------------------------------------------

    [Fact]
    public void TheUsedFlagIsPerCharacterAndPerItem() {
        Assert.Equal(6476, ItemStatEffects.UsedFlagKey(partySlot: 1, objectId: 0));
        Assert.Equal(6481, ItemStatEffects.UsedFlagKey(partySlot: 1, objectId: 5));
        Assert.Equal(6496, ItemStatEffects.UsedFlagKey(partySlot: 2, objectId: 0));
    }

    [Fact]
    public void ButTheKeySpaceAliasesAndWeReproduceThat() {
        // Stride 20, object ids up to 137: character 2's item 0 is character 1's item 20. Faithful,
        // and deliberately not widened — existing saves carry the original's layout.
        Assert.Equal(
            ItemStatEffects.UsedFlagKey(partySlot: 1, objectId: 20),
            ItemStatEffects.UsedFlagKey(partySlot: 2, objectId: 0));
    }

    // ---- the first read --------------------------------------------------------------

    [Fact]
    public void TheFirstReadRaisesTheAttributeByTheFullAmountWithNoRoll() {
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();

        bool applied = ItemStatEffects.Apply(stats, 1, Item(), Book(firstAmount: 3),
            flags.Read, flags.Write, _ => throw new System.InvalidOperationException("must not roll"));

        Assert.True(applied);
        Assert.Equal(23, stats[(int)ActorAttribute.Scouting].Base);
    }

    [Fact]
    public void AndRecordsThatThisCharacterHasReadIt() {
        ActorStat[] stats = Stats();
        var flags = new Flags();

        ItemStatEffects.Apply(stats, 1, Item(objectId: 5), Book(), flags.Read, flags.Write, _ => 0);

        Assert.Equal(1, flags.Read(ItemStatEffects.UsedFlagKey(1, 5)));
    }

    [Fact]
    public void OnlyTheAttributesInTheMaskAreTouched() {
        ActorStat[] stats = Stats(scouting: 20);
        stats[(int)ActorAttribute.Stealth].Base = 40;
        var flags = new Flags();

        ItemStatEffects.Apply(stats, 1, Item(), Book(mask: ScoutingBit), flags.Read, flags.Write, _ => 0);

        Assert.Equal(23, stats[(int)ActorAttribute.Scouting].Base);
        Assert.Equal(40, stats[(int)ActorAttribute.Stealth].Base);
    }

    [Fact]
    public void AMaskCanRaiseSeveralAttributesAtOnce() {
        ActorStat[] stats = Stats(scouting: 20);
        stats[(int)ActorAttribute.Stealth].Base = 40;
        int mask = ScoutingBit | (1 << (int)ActorAttribute.Stealth);
        var flags = new Flags();

        ItemStatEffects.Apply(stats, 1, Item(), Book(mask: mask, firstAmount: 2),
            flags.Read, flags.Write, _ => 0);

        Assert.Equal(22, stats[(int)ActorAttribute.Scouting].Base);
        Assert.Equal(42, stats[(int)ActorAttribute.Stealth].Base);
    }

    // ---- every read after ------------------------------------------------------------

    [Fact]
    public void ALaterReadIsGatedOnAChanceAndGivesNothingWhenItFails() {
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();
        flags.Write(ItemStatEffects.UsedFlagKey(1, 5), 1);   // already read once

        bool applied = ItemStatEffects.Apply(stats, 1, Item(objectId: 5),
            Book(chancePercent: 50), flags.Read, flags.Write, _ => 50);   // roll >= chance

        Assert.False(applied);
        Assert.Equal(20, stats[(int)ActorAttribute.Scouting].Base);
    }

    [Fact]
    public void ALaterReadThatPassesTapersTowardTheMaximumInsteadOfPayingFlat() {
        // PercentOfRemaining: the raise is (100 - current) * value / 256, so it shrinks as the
        // attribute fills up. Two characters, same book, different starting points.
        ActorStat[] low = Stats(scouting: 20);
        ActorStat[] high = Stats(scouting: 90);
        var flags = new Flags();
        flags.Write(ItemStatEffects.UsedFlagKey(1, 5), 1);

        ItemStatEffects.Apply(low, 1, Item(objectId: 5), Book(chancePercent: 100, laterValue: 100),
            flags.Read, flags.Write, _ => 0);
        ItemStatEffects.Apply(high, 1, Item(objectId: 5), Book(chancePercent: 100, laterValue: 100),
            flags.Read, flags.Write, _ => 0);

        int lowGain = low[(int)ActorAttribute.Scouting].Base - 20;
        int highGain = high[(int)ActorAttribute.Scouting].Base - 90;
        Assert.True(lowGain > highGain, $"expected the lower stat to gain more ({lowGain} vs {highGain})");
    }

    // ---- refusals --------------------------------------------------------------------

    [Fact]
    public void SomebodyOutsideThePartyGetsNothing() {
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();

        Assert.False(ItemStatEffects.Apply(stats, 0, Item(), Book(), flags.Read, flags.Write, _ => 0));
        Assert.Equal(20, stats[(int)ActorAttribute.Scouting].Base);
    }

    [Fact]
    public void AnItemWithNoAttributeMaskDoesNothing() {
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();

        Assert.False(ItemStatEffects.Apply(stats, 1, Item(), Book(mask: 0),
            flags.Read, flags.Write, _ => 0));
    }

    [Fact]
    public void AnExhaustedItemDoesNothing() {
        // condition 0 — the book is used up.
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();

        Assert.False(ItemStatEffects.Apply(stats, 1, Item(condition: 0), Book(),
            flags.Read, flags.Write, _ => 0));
        Assert.Equal(0, flags.Read(ItemStatEffects.UsedFlagKey(1, 5)));
    }

    [Fact]
    public void AnAttributeTheCharacterDoesNotHaveStaysInert() {
        // StatEngine refuses a stat whose maximum is 0, so a book cannot grant a skill from nothing.
        ActorStat[] stats = Stats(scouting: 20);
        stats[(int)ActorAttribute.Scouting] = new ActorStat { Base = 0, Max = 0 };
        var flags = new Flags();

        ItemStatEffects.Apply(stats, 1, Item(), Book(), flags.Read, flags.Write, _ => 0);

        Assert.Equal(0, stats[(int)ActorAttribute.Scouting].Base);
    }

    // ---- through the item-use dispatch ------------------------------------------------

    private static ObjectInfoSet BookSet(ObjectInfo book) =>
        new ObjectInfoSet("O", new List<ObjectInfo> { book });

    private static RuntimeContainer Pack(RuntimeItem item) {
        var c = new RuntimeContainer {
            Capacity = 8,
            ContainerType = GameData.Resources.Data.SaveGameContainerType.Inventory,
        };
        c.Items.Add(item);
        return c;
    }

    private static ItemUseContext Context(ActorStat[] stats, Flags flags, int roll = 0) =>
        new ItemUseContext(stats, 1, flags.Read, flags.Write, _ => roll);

    [Fact]
    public void WithNoCharacterContextTheDispatchStaysSilentRatherThanClaimingNoEffect() {
        ObjectInfo book = Book();
        book.Number = 5;
        RuntimeContainer pack = Pack(Item(objectId: 5, condition: 4));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(book));

        Assert.Equal(ItemUseOutcome.NotPorted, result.Outcome);
        Assert.Equal(4, pack.Items[0].Variable);   // untouched
    }

    [Fact]
    public void ReadingABookRaisesTheStatAndSpendsTheRead() {
        ObjectInfo book = Book(firstAmount: 3);
        book.Number = 5;
        book.Flags = ObjectFlags.LimitedUses;      // charge-bearing, so the tail decrements
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();
        RuntimeContainer pack = Pack(Item(objectId: 5, condition: 4));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(book),
            Context(stats, flags));

        Assert.Equal(ItemUseOutcome.Applied, result.Outcome);
        Assert.Equal(23, stats[(int)ActorAttribute.Scouting].Base);
        Assert.Equal(3, pack.Items[0].Variable);   // the tail ran: one charge gone
        Assert.NotEqual(0, result.DialogId);       // and the "used" record is asked for
    }

    [Fact]
    public void AFailedRepeatReadStillSpendsTheRead() {
        // The original sets outcome 1 unconditionally, so the charge goes whether or not the roll
        // paid out. Wiring the outcome to the effect's success would quietly make books free to
        // re-read until they worked.
        ObjectInfo book = Book(chancePercent: 50);
        book.Number = 5;
        book.Flags = ObjectFlags.LimitedUses;
        ActorStat[] stats = Stats(scouting: 20);
        var flags = new Flags();
        flags.Write(ItemStatEffects.UsedFlagKey(1, 5), 1);   // already read once
        RuntimeContainer pack = Pack(Item(objectId: 5, condition: 4));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(book),
            Context(stats, flags, roll: 99));                // roll >= chance: nothing learned

        Assert.Equal(ItemUseOutcome.Applied, result.Outcome);
        Assert.Equal(20, stats[(int)ActorAttribute.Scouting].Base);  // learned nothing
        Assert.Equal(3, pack.Items[0].Variable);                     // paid anyway
    }

    // ---- category 20, the restoratives -----------------------------------------------

    private static ObjectInfo Restorative(int conditionIndex, int amount) =>
        new ObjectInfo("r") {
            Number = 7,
            ObjectType = ObjectType.MassRestorative,
            EffectArgA = conditionIndex,
            EffectArgB = amount,
        };

    private static ItemUseContext ContextWith(ActorStat[] stats, ActorConditions conditions, Flags flags) =>
        new ItemUseContext(stats, 1, flags.Read, flags.Write, _ => 0, conditions);

    [Fact]
    public void AHerbalPackSetsHealing() {
        // The shipped Herbal Pack is argA 4 (Healing), argB 100.
        var conditions = new ActorConditions();
        RuntimeContainer pack = Pack(Item(objectId: 7, condition: 1));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget,
            BookSet(Restorative((int)ActorCondition.Healing, 100)),
            ContextWith(Stats(), conditions, new Flags()));

        Assert.Equal(ItemUseOutcome.Applied, result.Outcome);
        Assert.Equal(100, conditions[ActorCondition.Healing]);
    }

    [Fact]
    public void AnAleCaskMakesYouDrunk() {
        // argA 3 (Drunk), argB 25 — and it stacks with what is already there.
        var conditions = new ActorConditions();
        conditions[ActorCondition.Drunk] = 10;
        RuntimeContainer pack = Pack(Item(objectId: 7, condition: 1));

        InventoryUse.Use(pack, 0, InventoryUse.NoTarget,
            BookSet(Restorative((int)ActorCondition.Drunk, 25)),
            ContextWith(Stats(), conditions, new Flags()));

        Assert.Equal(35, conditions[ActorCondition.Drunk]);
    }

    [Fact]
    public void ARestorativeIsSpentLikeAnyOtherUse() {
        var conditions = new ActorConditions();
        ObjectInfo rec = Restorative((int)ActorCondition.Healing, 100);
        rec.Flags = ObjectFlags.LimitedUses;
        RuntimeContainer pack = Pack(Item(objectId: 7, condition: 3));

        InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(rec),
            ContextWith(Stats(), conditions, new Flags()));

        Assert.Equal(2, pack.Items[0].Variable);
    }

    [Fact]
    public void WithNoConditionsToActOnTheCategoryStaysSilent() {
        // A container view has no character, so there is nothing to restore.
        RuntimeContainer pack = Pack(Item(objectId: 7, condition: 1));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget,
            BookSet(Restorative((int)ActorCondition.Healing, 100)));

        Assert.Equal(ItemUseOutcome.NotPorted, result.Outcome);
    }

    // ---- category 19, the restorative ------------------------------------------------

    private static ObjectInfo Potion(int healBase, int spread) =>
        new ObjectInfo("p") {
            Number = 9,
            ObjectType = ObjectType.Restorative,
            EffectArgA = healBase,
            EffectArgB = spread,
        };

    private static ActorStat[] Wounded(byte health = 10, byte stamina = 10, byte max = 50) {
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 0, Max = max };
        }
        stats[(int)ActorAttribute.Health] = new ActorStat { Base = health, Max = max };
        stats[(int)ActorAttribute.Stamina] = new ActorStat { Base = stamina, Max = max };
        return stats;
    }

    [Fact]
    public void ADoseHealsThePoolAndEasesEveryAfflictionButHealing() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Poisoned] = 20;
        conditions[ActorCondition.Starving] = 8;
        conditions[ActorCondition.Healing] = 30;
        ActorStat[] stats = Wounded(health: 10, stamina: 10);
        RuntimeContainer pack = Pack(Item(objectId: 9, condition: 3));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget,
            BookSet(Potion(healBase: 6, spread: 0)),
            ContextWith(stats, conditions, new Flags()));

        Assert.Equal(ItemUseOutcome.Handled, result.Outcome);
        Assert.Equal(15, conditions[ActorCondition.Poisoned]);   // -5
        Assert.Equal(3, conditions[ActorCondition.Starving]);    // -5
        Assert.Equal(30, conditions[ActorCondition.Healing]);    // untouched
        Assert.True(stats[(int)ActorAttribute.Health].Base + stats[(int)ActorAttribute.Stamina].Base > 20);
    }

    [Fact]
    public void AnAfflictionCannotBeDrivenBelowNothing() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Sick] = 2;
        RuntimeContainer pack = Pack(Item(objectId: 9, condition: 3));

        InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Potion(6, 0)),
            ContextWith(Wounded(), conditions, new Flags()));

        Assert.Equal(0, conditions[ActorCondition.Sick]);
    }

    [Fact]
    public void ADoseSpendsOneChargeAndTheLastDoseRemovesTheItem() {
        RuntimeContainer pack = Pack(Item(objectId: 9, condition: 2));
        var flags = new Flags();

        ItemUseResult first = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Potion(6, 0)),
            ContextWith(Wounded(), new ActorConditions(), flags));
        Assert.False(first.SourceRemoved);
        Assert.Equal(1, pack.Items[0].Variable);

        ItemUseResult second = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Potion(6, 0)),
            ContextWith(Wounded(), new ActorConditions(), flags));
        Assert.True(second.SourceRemoved);
        Assert.Empty(pack.Items);
    }

    [Fact]
    public void TheSpreadIsRolledOnTopOfTheBaseHeal() {
        // argA 6, argB 2 -> 6 + rnd(2). The shipped "Restoratives" is exactly this.
        var conditions = new ActorConditions();
        ActorStat[] low = Wounded(health: 10, stamina: 10);
        ActorStat[] high = Wounded(health: 10, stamina: 10);
        RuntimeContainer a = Pack(Item(objectId: 9, condition: 3));
        RuntimeContainer b = Pack(Item(objectId: 9, condition: 3));

        InventoryUse.Use(a, 0, InventoryUse.NoTarget, BookSet(Potion(6, 2)),
            new ItemUseContext(low, 1, new Flags().Read, new Flags().Write, _ => 0, conditions));
        InventoryUse.Use(b, 0, InventoryUse.NoTarget, BookSet(Potion(6, 2)),
            new ItemUseContext(high, 1, new Flags().Read, new Flags().Write, _ => 1, new ActorConditions()));

        int lowPool = low[(int)ActorAttribute.Health].Base + low[(int)ActorAttribute.Stamina].Base;
        int highPool = high[(int)ActorAttribute.Health].Base + high[(int)ActorAttribute.Stamina].Base;
        Assert.True(highPool > lowPool, $"a higher roll should heal more ({highPool} vs {lowPool})");
    }

    [Fact]
    public void ARestorativeNeedsACharacterToRestore() {
        RuntimeContainer pack = Pack(Item(objectId: 9, condition: 3));

        Assert.Equal(ItemUseOutcome.NotPorted,
            InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Potion(6, 0))).Outcome);
    }

    // ---- category 13, the scroll -----------------------------------------------------

    private static ObjectInfo Scroll() =>
        new ObjectInfo("s") { Number = 11, ObjectType = ObjectType.MagicalScroll };

    private static ItemUseContext ScrollContext(ushort[] known) =>
        new ItemUseContext(Stats(), 1, new Flags().Read, new Flags().Write, _ => 0, null, known);

    [Fact]
    public void TheSpellNumberIsTheScrollsOwnConditionByte() {
        ushort[] known = GameData.Resources.Spells.SpellBook.Empty();
        RuntimeContainer pack = Pack(new RuntimeItem(11, 20, 0));   // spell 20

        InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Scroll()), ScrollContext(known));

        Assert.True(GameData.Resources.Spells.SpellBook.IsKnown(known, 20));
        Assert.False(GameData.Resources.Spells.SpellBook.IsKnown(known, 19));
    }

    [Fact]
    public void ReadingAScrollLearnsTheSpellAndSpendsIt() {
        ushort[] known = GameData.Resources.Spells.SpellBook.Empty();
        ObjectInfo rec = Scroll();
        rec.Flags = ObjectFlags.ConsumedOnUse;
        RuntimeContainer pack = Pack(new RuntimeItem(11, 7, 0));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(rec),
            ScrollContext(known));

        Assert.Equal(ItemUseOutcome.Applied, result.Outcome);
        Assert.True(result.SourceRemoved);
        Assert.Empty(pack.Items);
    }

    [Fact]
    public void ButASpellYouAlreadyKnowDoesNothingAndKEEPSTheScroll() {
        // The whole point of the return value: a wasted read must not eat the scroll.
        ushort[] known = GameData.Resources.Spells.SpellBook.Empty();
        GameData.Resources.Spells.SpellBook.Learn(known, 7);
        ObjectInfo rec = Scroll();
        rec.Flags = ObjectFlags.ConsumedOnUse;
        RuntimeContainer pack = Pack(new RuntimeItem(11, 7, 0));

        ItemUseResult result = InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(rec),
            ScrollContext(known));

        Assert.Equal(ItemUseOutcome.NoEffect, result.Outcome);
        Assert.False(result.SourceRemoved);
        Assert.Single(pack.Items);
    }

    [Fact]
    public void AContainerWithNoSpellbookStaysSilent() {
        RuntimeContainer pack = Pack(new RuntimeItem(11, 7, 0));

        Assert.Equal(ItemUseOutcome.NotPorted,
            InventoryUse.Use(pack, 0, InventoryUse.NoTarget, BookSet(Scroll())).Outcome);
    }
}
