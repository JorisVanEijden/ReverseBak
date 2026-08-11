namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Equipment modifiers — <c>stat_actor_recalc_equip_bonuses</c>. The object numbers and amounts
/// here are the shipped ones from OBJINFO.DAT; only six objects in the game carry a modifier.
/// </summary>
public class ItemModifierTests {
    private const int StaffOfMacros = 4;
    private const int AmuletOfTheUprightMan = 5;
    private const int IdolOfLassur = 12;
    private const int Weedwalkers = 0x5a;

    private static readonly Dictionary<int, ObjectInfo> Catalog = new Dictionary<int, ObjectInfo> {
        [StaffOfMacros] = Object(ActorAttributeFlag.AccuracyCasting, 20),
        [AmuletOfTheUprightMan] = Object(ActorAttributeFlag.LockPicking, 15),
        [IdolOfLassur] = Object(
            ActorAttributeFlag.Defense | ActorAttributeFlag.AccuracyCrossbow
            | ActorAttributeFlag.AccuracyMelee | ActorAttributeFlag.AccuracyCasting
            | ActorAttributeFlag.Assessment | ActorAttributeFlag.ArmorCraft
            | ActorAttributeFlag.WeaponCraft | ActorAttributeFlag.Barding
            | ActorAttributeFlag.Haggling | ActorAttributeFlag.LockPicking
            | ActorAttributeFlag.Scouting | ActorAttributeFlag.Stealth, -20),
        [Weedwalkers] = Object(ActorAttributeFlag.Stealth, 30),
        [99] = Object(0, 0), // an ordinary object with no modifier
    };

    private static ObjectInfo Object(ActorAttributeFlag mask, int amount) =>
        new ObjectInfo("test") { EquipAttributeMask = mask, EquipModifierAmount = amount };

    private static ObjectInfo Lookup(int id) => Catalog.TryGetValue(id, out ObjectInfo o) ? o : null;

    private static ActorStat[] Stats() {
        var stats = new ActorStat[16];
        for (int i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 50, Max = 100 };
        }
        return stats;
    }

    private static sbyte ModifierOn(ActorStat[] stats, ActorAttribute attribute) =>
        stats[(int)attribute].Modifier;

    [Fact]
    public void CarryingAModifierObjectBonusesItsAttribute() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { StaffOfMacros }, Lookup);

        Assert.Equal(20, ModifierOn(stats, ActorAttribute.AccuracyCasting));
        Assert.Equal(0, ModifierOn(stats, ActorAttribute.Stealth));
    }

    [Fact]
    public void ItIsCarryingThatCounts_NotWearing() {
        // The routine walks the whole inventory; there is no equipped/stowed distinction in it.
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { 99, 99, AmuletOfTheUprightMan, 99 }, Lookup);

        Assert.Equal(15, ModifierOn(stats, ActorAttribute.LockPicking));
    }

    [Fact]
    public void OneObjectCanPenaliseAWholeSpreadOfAttributes() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { IdolOfLassur }, Lookup);

        Assert.Equal(-20, ModifierOn(stats, ActorAttribute.Defense));
        Assert.Equal(-20, ModifierOn(stats, ActorAttribute.Stealth));
        Assert.Equal(-20, ModifierOn(stats, ActorAttribute.Haggling));
        Assert.Equal(0, ModifierOn(stats, ActorAttribute.Health));
        Assert.Equal(0, ModifierOn(stats, ActorAttribute.Speed));
    }

    [Fact]
    public void ModifiersFromDifferentObjectsStackOnTheSameAttribute() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats,
            new[] { AmuletOfTheUprightMan, IdolOfLassur }, Lookup);

        Assert.Equal(-5, ModifierOn(stats, ActorAttribute.LockPicking)); // +15 and -20
    }

    [Fact]
    public void ASecondPairOfWeedwalkersIsWorthNothing() {
        // The 1.02 CD build counts object 0x5a at most once — a stacking exploit patched in that
        // release. The floppy build would have given +60 here.
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats,
            new[] { Weedwalkers, Weedwalkers, Weedwalkers }, Lookup);

        Assert.Equal(30, ModifierOn(stats, ActorAttribute.Stealth));
    }

    [Fact]
    public void TheWeedwalkersRuleDoesNotLeakToOtherObjects() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats,
            new[] { StaffOfMacros, StaffOfMacros }, Lookup);

        Assert.Equal(40, ModifierOn(stats, ActorAttribute.AccuracyCasting));
    }

    [Fact]
    public void RecalculatingFromScratchNeverDoubleCounts() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { StaffOfMacros }, Lookup);
        StatEngine.RecalculateItemModifiers(stats, new[] { StaffOfMacros }, Lookup);

        Assert.Equal(20, ModifierOn(stats, ActorAttribute.AccuracyCasting));
    }

    [Fact]
    public void DroppingTheObjectTakesTheBonusAwayAgain() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { StaffOfMacros }, Lookup);
        StatEngine.RecalculateItemModifiers(stats, new int[0], Lookup);

        Assert.Equal(0, ModifierOn(stats, ActorAttribute.AccuracyCasting));
    }

    [Fact]
    public void AnUnknownObjectIdIsIgnored() {
        ActorStat[] stats = Stats();

        StatEngine.RecalculateItemModifiers(stats, new[] { 200, StaffOfMacros }, Lookup);

        Assert.Equal(20, ModifierOn(stats, ActorAttribute.AccuracyCasting));
    }

    [Fact]
    public void AModifierShowsUpInWhatTheAttributeReadsAs() {
        ActorStat[] stats = Stats();
        StatEngine.RecalculateItemModifiers(stats, new[] { StaffOfMacros }, Lookup);
        ActorStat health = new ActorStat { Base = 100, Max = 100 };

        int value = StatEngine.Get(stats[(int)ActorAttribute.AccuracyCasting],
            ActorAttribute.AccuracyCasting, health);

        Assert.Equal(70, value); // base 50 + 20
    }
}
