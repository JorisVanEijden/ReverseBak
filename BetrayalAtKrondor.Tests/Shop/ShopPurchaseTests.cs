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
}
