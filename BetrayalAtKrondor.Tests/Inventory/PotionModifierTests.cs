namespace BetrayalAtKrondor.Tests.Inventory;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using Xunit;

/// <summary>
/// Drinking a stat potion — <c>ITEMUSE.C:302-329</c>, effect category 0x12 (TASK-262).
/// </summary>
/// <remarks>
/// The five shipped potions are the whole of this category, and every one of them did nothing at
/// all until the case existed: Dalatail Milk (+25 Defense), Fadamor's Formula (+10 Strength),
/// Lewton's Concentrate (+30 Casting), Redweed Brew (+20 Melee) and Truesight Tea (+25 Crossbow),
/// all for eight hours.
/// </remarks>
public class PotionModifierTests {
    private const int DefenseMask = 1 << (int)ActorAttribute.Defense;

    // Dalatail Milk as shipped: flags 0x0200 (Expires set, CombatOnly clear), Defense, +25, 8h.
    private static ObjectInfo DalatailMilk() => new ObjectInfo("OBJINFO.DAT") {
        Number = 114, Name = "Dalatail Milk", ObjectType = ObjectType.Potion,
        EffectArgA = 0x0200, EffectArgB = DefenseMask,
        UseEffectAmount = 25, EffectDurationHours = 8,
    };

    private static (RuntimeContainer Pack, ItemUseContext Context, ActorStatModifiers.Slot[] Slots)
        Drinker(uint gameTime = 1000) {
        var pack = new RuntimeContainer();
        pack.Items.Add(new RuntimeItem(114, 1, 0));
        var slots = new ActorStatModifiers.Slot[ActorStatModifiers.SlotsPerCharacter];
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 20, Max = 99 };
        }
        var context = new ItemUseContext(stats, partySlot: 1, readFlag: _ => 0,
            writeFlag: (_, _) => { }, random: _ => 0, statModifiers: slots, gameTime: gameTime);
        return (pack, context, slots);
    }

    private static ObjectInfoSet Catalog() =>
        new ObjectInfoSet("OBJINFO.DAT", new[] { DalatailMilk() });

    [Fact]
    public void APotionFillsASlotFromFieldsNamedForSomethingElse() {
        // *** THREE OF THE FOUR RECORD WORDS ARE READ FOR A DIFFERENT PURPOSE HERE. *** EffectArgA
        // is the FLAGS word, EffectArgB the STAT MASK, UseEffectAmount (canassa's
        // wEffect_chance_pct) the VALUE rather than a percentage, and EffectDurationHours (its
        // wEffect_stat_value) the DURATION rather than a stat. Reading them by their names puts a
        // chance where the value belongs.
        (RuntimeContainer pack, ItemUseContext ctx, ActorStatModifiers.Slot[] slots) = Drinker();

        ItemUseResult result = InventoryUse.Use(pack, 0, -1, Catalog(), ctx);

        Assert.Equal(ItemUseOutcome.Handled, result.Outcome);
        Assert.Equal(0x0200, slots[0].Flags);
        Assert.Equal(DefenseMask, slots[0].StatMask);
        Assert.Equal(25, slots[0].Value);
        Assert.Equal(1000u, slots[0].AppliedAt);
        Assert.Equal(ActorStatModifiers.ItemExpiryAt(1000, 8), slots[0].ExpiresAt);
    }

    [Fact]
    public void TheShippedFlagsMeanItLAPSESAndWorksOUTOfCombat() {
        // All five ship 0x0200: Expires SET, CombatOnly CLEAR. So an item's buff is the opposite of
        // a spell status on both counts — it wears off, and it applies while walking around.
        (RuntimeContainer pack, ItemUseContext ctx, ActorStatModifiers.Slot[] slots) = Drinker();
        InventoryUse.Use(pack, 0, -1, Catalog(), ctx);

        Assert.Equal(0, slots[0].Flags & ActorStatModifiers.SpellStatusFlags);
        Assert.NotEqual(0, slots[0].Flags & (int)ActorStatModifiers.ModifierFlags.Expires);
        // 20 + 25: it buffs OUT of combat, where a spell status would be skipped entirely.
        Assert.Equal(45, ActorStatModifiers.Apply(slots[0], 20, inCombat: false, gameTime: 1000,
            out bool expired));
        Assert.False(expired);
    }

    [Fact]
    public void ANY_ModifierOnThatStatRefusesIt_AndTheRefusalSPEAKS() {
        // *** STRICTER THAN THE SPELL RULE, AND IT TELLS THE PLAYER. *** Where two casts of a debuff
        // stack because the spell test exempts other spell statuses, a potion is blocked by ANY
        // non-empty slot on that stat — including a spell status — and plays a dialog rather than
        // failing quietly.
        (RuntimeContainer pack, ItemUseContext ctx, ActorStatModifiers.Slot[] slots) = Drinker();
        slots[0] = new ActorStatModifiers.Slot(ActorStatModifiers.SpellStatusFlags, DefenseMask,
            -5, 0, 0);

        ItemUseResult result = InventoryUse.Use(pack, 0, -1, Catalog(), ctx);

        Assert.Equal(ItemUseOutcome.NoEffect, result.Outcome);
        Assert.Equal(InventoryUse.PotionRefusedRecord, result.DialogId);
        Assert.Single(pack.Items);   // and it is NOT drunk
    }

    [Fact]
    public void ALapsedModifierDoesNotBlockAFreshDrink() {
        // The sweep runs before the dedupe, the same as the spell path, so a slot that has run out
        // is freed rather than standing in the way of the next potion.
        (RuntimeContainer pack, ItemUseContext ctx, ActorStatModifiers.Slot[] slots) =
            Drinker(gameTime: 100000);
        slots[0] = new ActorStatModifiers.Slot((int)ActorStatModifiers.ModifierFlags.Expires,
            DefenseMask, 25, 0, 10);

        ItemUseResult result = InventoryUse.Use(pack, 0, -1, Catalog(), ctx);

        Assert.Equal(ItemUseOutcome.Handled, result.Outcome);
        Assert.Empty(pack.Items);
    }
}
