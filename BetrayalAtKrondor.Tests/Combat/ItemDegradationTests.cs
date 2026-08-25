namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// <c>cbstat_damage_equipped_items</c> — the wear the four constants on
/// <see cref="CombatFormulas"/> describe and that nothing had ever applied.
/// </summary>
public class ItemDegradationTests {
    private static ObjectInfo Item(ObjectType type, int chance = 100, int maxWear = 1,
        int floor = 0, ObjectFlags flags = ObjectFlags.Degradable) =>
        new ObjectInfo("O") {
            Number = 1, Name = "item", ObjectType = type, Flags = flags,
            DegradeChancePercent = chance, MaxWearPerDegrade = maxWear, MinimumQuality = floor,
        };

    /// <summary>Hands out the given rolls in order, then zero.</summary>
    private static System.Func<int, int> Rolls(params int[] values) {
        var i = 0;
        return _ => i < values.Length ? values[i++] : 0;
    }

    [Fact]
    public void AskingForASwordAlsoWearsASTAFF() {
        // altcategory = (category == 1) ? 3 : category. A staff is a melee weapon everywhere else
        // in combat too, so matching only the requested category leaves a caster's staff pristine.
        Assert.True(ItemDegradation.CategoryMatches(ObjectType.Sword, ObjectType.Staff));
        Assert.True(ItemDegradation.CategoryMatches(ObjectType.Sword, ObjectType.Sword));

        // And it is one-way: asking for a staff does not reach a sword.
        Assert.False(ItemDegradation.CategoryMatches(ObjectType.Staff, ObjectType.Sword));
        Assert.False(ItemDegradation.CategoryMatches(ObjectType.Armor, ObjectType.Sword));
    }

    [Fact]
    public void OnlyEQUIPPEDDegradableItemsWear() {
        ObjectInfo sword = Item(ObjectType.Sword);

        Assert.True(ItemDegradation.Wears(ObjectType.Sword, sword, ItemFlags.Equipped));
        // A spare in the pack is untouched however hard its owner fights.
        Assert.False(ItemDegradation.Wears(ObjectType.Sword, sword, 0));
        // A type without the flag has no real condition to wear.
        Assert.False(ItemDegradation.Wears(ObjectType.Sword,
            Item(ObjectType.Sword, flags: 0), ItemFlags.Equipped));
    }

    [Fact]
    public void TheSeverityIsAMULTIPLIEROnTheItemsOwnBite_notAPointCount() {
        // *** Read as points, a swing (256) would destroy any item on its first use. ***
        // roll 1..MaxWearPerDegrade, times severity, over 256.
        Assert.Equal(4, ItemDegradation.WearAmount(4, CombatFormulas.WeaponWearOnSwing, Rolls(3)));
        Assert.Equal(2, ItemDegradation.WearAmount(4, CombatFormulas.WeaponWearOnThrust, Rolls(3)));
        Assert.Equal(8, ItemDegradation.WearAmount(4, CombatFormulas.ArmorWearOnRangedHit, Rolls(3)));
    }

    [Fact]
    public void AnItemWhoseMaxIsOneTakesExactlyOneAndNeverRolls() {
        // The original guards with `amount > 1 ? RND(amount-1)+1 : 1`. RND(0) is a division by zero
        // in most implementations and a silent zero in the rest — which would make these items
        // indestructible.
        var rolled = false;
        int amount = ItemDegradation.WearAmount(1, CombatFormulas.WeaponWearOnSwing,
            _ => { rolled = true; return 0; });

        Assert.Equal(1, amount);
        Assert.False(rolled, "no roll is taken at all");
    }

    [Fact]
    public void MostAttacksWearNOTHING_theChanceIsPerItemType() {
        // Gear lasts through many fights. Wearing on every hit would have the party re-equipping
        // constantly.
        ObjectInfo sword = Item(ObjectType.Sword, chance: 10, maxWear: 4);

        ItemDegradation.Result r = ItemDegradation.Apply(sword, 100, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(50));   // 50 >= 10 -> no wear

        Assert.Equal(100, r.Condition);
        Assert.False(r.Broke);
    }

    [Fact]
    public void TheItemIsMarkedEvenWhenItDoesNotWear_whichIsTheCDBuildsArm() {
        // The floppy gates the whole branch on the roll; ours stamps the bit first.
        ObjectInfo sword = Item(ObjectType.Sword, chance: 0);

        ItemDegradation.Result r = ItemDegradation.Apply(sword, 100, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(50));

        Assert.True((r.Flags & ItemDegradation.UsedInAnger) != 0);
        Assert.True((r.Flags & ItemFlags.Repairable) == 0, "and NOT repairable — it never wore");
    }

    [Fact]
    public void AWornItemBecomesRepairable() {
        ObjectInfo sword = Item(ObjectType.Sword, chance: 100, maxWear: 4);

        ItemDegradation.Result r = ItemDegradation.Apply(sword, 100, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(0, 3));

        Assert.Equal(96, r.Condition);
        Assert.True((r.Flags & ItemFlags.Repairable) != 0);
    }

    [Fact]
    public void ACrossbowCanSNAPRatherThanWearDown_andItIsTheOneAudibleItemFailure() {
        // Category 2 only: a bow near the end of its life is living on a coin toss.
        ObjectInfo bow = Item(ObjectType.Crossbow, chance: 100, maxWear: 2);

        // rolls: 0 passes the chance gate, 1 makes the bite 2 (20 -> 18), 40 is the snap roll.
        ItemDegradation.Result r = ItemDegradation.Apply(bow, 20, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(0, 1, 40));   // worn 18 <= RND(50)=40

        Assert.True(r.Snapped);
        Assert.Equal(0, r.Condition);
        Assert.True(r.Broke);
        Assert.Equal(0x43, ItemDegradation.SnapSoundId);
    }

    [Fact]
    public void ASWORDNeverSnaps_howeverLowItGets() {
        // The snap test is on category 2 alone; generalising it would make every weapon fail
        // suddenly instead of wearing out.
        ObjectInfo sword = Item(ObjectType.Sword, chance: 100, maxWear: 2);

        ItemDegradation.Result r = ItemDegradation.Apply(sword, 20, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(0, 1, 40));

        Assert.False(r.Snapped);
        // It wore down by the ordinary bite and stopped there.
        Assert.Equal(18, r.Condition);
    }

    [Fact]
    public void TheFLOORIsAppliedAFTERTheSnap_soAFlooredBowSnapsButDoesNotBreak() {
        // The original writes 0, clamps to MinimumQuality, THEN tests for broken. Faithful, and it
        // looks like a bug from either end alone.
        ObjectInfo bow = Item(ObjectType.Crossbow, chance: 100, maxWear: 2, floor: 5);

        ItemDegradation.Result r = ItemDegradation.Apply(bow, 20, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(0, 1, 40));

        Assert.True(r.Snapped);
        Assert.Equal(5, r.Condition);
        Assert.False(r.Broke, "the floor caught it before the broken test");
    }

    [Fact]
    public void WearingToZeroBreaksTheItem() {
        ObjectInfo sword = Item(ObjectType.Sword, chance: 100, maxWear: 2);

        ItemDegradation.Result r = ItemDegradation.Apply(sword, 1, ItemFlags.Equipped,
            CombatFormulas.WeaponWearOnSwing, Rolls(0, 1));

        Assert.Equal(0, r.Condition);
        Assert.True(r.Broke);
        Assert.True((r.Flags & ItemFlags.Broken) != 0);
    }

    [Fact]
    public void TheFourSeveritiesAreTheOnesTheCallersPass() {
        // swing (attacker, 1, 0x100), thrust (attacker, 1, 0x80), armour-on-melee (target, 4,
        // 0x100), armour-on-ranged (actor, 4, 0x200). A crossbow shot wears the bow at 0x100 too.
        Assert.Equal(0x100, CombatFormulas.WeaponWearOnSwing);
        Assert.Equal(0x80, CombatFormulas.WeaponWearOnThrust);
        Assert.Equal(0x100, CombatFormulas.ArmorWearOnMeleeHit);
        Assert.Equal(0x200, CombatFormulas.ArmorWearOnRangedHit);
        Assert.Equal(ItemDegradation.SeverityUnit, CombatFormulas.WeaponWearOnSwing);
    }
}
