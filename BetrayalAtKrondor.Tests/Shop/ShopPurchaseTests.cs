namespace BetrayalAtKrondor.Tests.Shop;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Shop;
using Xunit;

/// <summary>
/// The three goods that are not what the shelf says: a day's rations, the counter drinks, and
/// what a drink does to whoever bought it.
/// </summary>
public class ShopPurchaseTests {
    private static ObjectInfo Drink(int number, int drunkAmount) =>
        new ObjectInfo("test") { Number = number, ObjectType = ObjectType.Drink, EffectArgB = drunkAmount };

    private static ActorStat Pool(int max, int current) =>
        new ActorStat { Max = (byte)max, Base = (byte)current };

    [Fact]
    public void DaysRations_IsDeliveredAsOneRation() {
        RuntimeItem delivered = ShopPurchase.Delivered(
            new RuntimeItem((byte)ShopPurchase.DaysRationsObjectId, 0, 0));

        Assert.Equal(UpkeepEngine.RationsObjectId, delivered.ObjectId);
        Assert.Equal(1, delivered.Variable);
    }

    [Fact]
    public void EverythingElse_IsDeliveredUnchanged() {
        var sword = new RuntimeItem(18, 90, 4);
        Assert.Same(sword, ShopPurchase.Delivered(sword));
    }

    /// <summary>
    /// The range is the point: a day's rations is TYPED as a drink and sits directly below the
    /// three real ones, so a type test here would have the tavern drink the party's food.
    /// </summary>
    [Theory]
    [InlineData(ShopPurchase.DaysRationsObjectId, false)]
    [InlineData(135, true)]   // Quegian Brandy
    [InlineData(136, true)]   // Ale
    [InlineData(137, true)]   // Keshian Ale
    [InlineData(138, false)]
    public void CounterDrinks_AreAnIdRange(int objectId, bool expected) =>
        Assert.Equal(expected, ShopPurchase.IsCounterDrink(objectId));

    [Fact]
    public void Drinking_MakesYouDrunk_ClearsHunger_AndLiftsThePool() {
        var conditions = new ActorConditions();
        ConditionEngine.Apply(conditions, ActorCondition.Starving, 40);
        ActorStat health = Pool(50, 10);
        ActorStat stamina = Pool(50, 10);

        Assert.True(ShopPurchase.Drink(Drink(136, 12), conditions, health, stamina));

        Assert.Equal(12, conditions[ActorCondition.Drunk]);
        Assert.Equal(0, conditions[ActorCondition.Starving]);
        // The pool is health+stamina summed and refilled together, health first.
        Assert.True(health.Base + stamina.Base > 20);
    }

    [Fact]
    public void AtMaxDrunk_NothingHappensAndTheCallerMustRefund() {
        var conditions = new ActorConditions();
        ConditionEngine.Apply(conditions, ActorCondition.Drunk, ShopPurchase.MaxDrunk);
        ConditionEngine.Apply(conditions, ActorCondition.Starving, 40);
        ActorStat health = Pool(50, 10);

        Assert.False(ShopPurchase.Drink(Drink(136, 12), conditions, health, Pool(50, 10)));

        // Still hungry, still at the ceiling — a refused drink leaves no trace but the money back.
        Assert.Equal(40, conditions[ActorCondition.Starving]);
        Assert.Equal(ShopPurchase.MaxDrunk, conditions[ActorCondition.Drunk]);
    }

    // ---- the Near-death ceiling applies to a drink too ----

    [Fact]
    public void ADrinkCannotLiftANearDeathDrinkerPastTheirCeiling() {
        // SHOP.C:153 is stat_combatant_modify(&characters[partySlot], 0x10, 0x300, 0x3c) -- a real
        // party character, so charSlot != 0 and STAT.C:206-211 REPLACES the 60% target with
        // ((100 - rank) * 0x1e) / 100 + 1. At rank 90 that is 4, so a 3-point drink on a pool of 4
        // may add nothing at all.
        //
        // The ceiling lives inside stat_combatant_modify, so the original cannot opt out of it.
        // Here it is a defaulted argument -- the same shape that let a whole camp rest skip it
        // (TASK-605).
        var health = new ActorStat { Base = 4, Effective = 4, Max = 60 };
        var stamina = new ActorStat { Base = 0, Effective = 0, Max = 40 };
        var conditions = new ActorConditions();
        ConditionEngine.Apply(conditions, ActorCondition.NearDeath, 90);

        Assert.True(ShopPurchase.Drink(Drink(number: 1, drunkAmount: 1), conditions, health, stamina));

        // ((100 - 90) * 30) / 100 + 1 = 4. Already there, so the drink lifts nothing.
        Assert.Equal(4, StatEngine.HealthPool(health, stamina));
    }

    [Fact]
    public void ADrinkStillRestoresSomeoneWhoIsNOTNearDeath() {
        // The control: without it the case above is satisfied by a drink that heals nobody.
        var health = new ActorStat { Base = 4, Effective = 4, Max = 60 };
        var stamina = new ActorStat { Base = 0, Effective = 0, Max = 40 };
        var conditions = new ActorConditions();

        Assert.True(ShopPurchase.Drink(Drink(number: 1, drunkAmount: 1), conditions, health, stamina));

        Assert.Equal(7, StatEngine.HealthPool(health, stamina));
    }
}
