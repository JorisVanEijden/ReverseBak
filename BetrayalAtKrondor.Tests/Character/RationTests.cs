namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The daily meal — <c>gstate_member_consume_rations</c> — and the consume-by-kind primitive
/// underneath it.
/// </summary>
public class RationTests {
    private static readonly Dictionary<int, ObjectInfo> Catalog = new Dictionary<int, ObjectInfo> {
        [UpkeepEngine.RationsObjectId] = Object(ObjectFlags.Stackable),
        [UpkeepEngine.PoisonedRationsObjectId] = Object(ObjectFlags.Stackable),
        [UpkeepEngine.SpoiledRationsObjectId] = Object(ObjectFlags.Stackable),
        [InventoryConsume.TorchObjectId] = Object(ObjectFlags.Stackable | ObjectFlags.LimitedUses
            | ObjectFlags.DiscardWhenEmpty),
        // An object that empties rather than vanishing.
        [200] = Object(ObjectFlags.LimitedUses),
    };

    private static ObjectInfo Object(ObjectFlags flags) =>
        new ObjectInfo("test") { Flags = flags };

    private static ObjectInfo Lookup(int id) => Catalog.TryGetValue(id, out ObjectInfo o) ? o : null;

    private static RuntimeContainer Pack(params (int ObjectId, byte Count)[] items) {
        var container = new RuntimeContainer { Capacity = 20 };
        foreach ((int objectId, byte count) in items) {
            container.Items.Add(new RuntimeItem((byte)objectId, count, 0));
        }
        return container;
    }

    private static int CountOf(RuntimeContainer pack, int objectId) {
        var total = 0;
        foreach (RuntimeItem item in pack.Items) {
            if (item.ObjectId == objectId) {
                total += item.Variable;
            }
        }
        return total;
    }

    // ---- the meal ---------------------------------------------------------

    [Fact]
    public void AGoodRationFeedsTheMemberAndClearsTheHunger() {
        RuntimeContainer pack = Pack((UpkeepEngine.RationsObjectId, 5));
        var conditions = new ActorConditions();
        conditions[ActorCondition.Starving] = 40;

        Meal meal = UpkeepEngine.ConsumeRations(pack, conditions, Lookup);

        Assert.Equal(Meal.Rations, meal);
        Assert.Equal(4, CountOf(pack, UpkeepEngine.RationsObjectId));
        Assert.Equal(0, conditions[ActorCondition.Starving]);
    }

    [Fact]
    public void GoodRationsAreEatenBeforeSpoiledOnes() {
        RuntimeContainer pack = Pack(
            (UpkeepEngine.SpoiledRationsObjectId, 3),
            (UpkeepEngine.RationsObjectId, 3));

        Meal meal = UpkeepEngine.ConsumeRations(pack, new ActorConditions(), Lookup);

        Assert.Equal(Meal.Rations, meal);
        Assert.Equal(3, CountOf(pack, UpkeepEngine.SpoiledRationsObjectId));
    }

    [Fact]
    public void SpoiledRationsFeedYouButMakeYouSick() {
        RuntimeContainer pack = Pack((UpkeepEngine.SpoiledRationsObjectId, 2));
        var conditions = new ActorConditions();
        conditions[ActorCondition.Starving] = 60;

        Meal meal = UpkeepEngine.ConsumeRations(pack, conditions, Lookup);

        Assert.Equal(Meal.SpoiledRations, meal);
        Assert.Equal(0, conditions[ActorCondition.Starving]);
        Assert.Equal(3, conditions[ActorCondition.Sick]);
    }

    [Fact]
    public void PoisonedRationsPoisonYouAndLeaveYouHungry() {
        // The cruel case: they are eaten last, and unlike the other two they do not clear Starving.
        RuntimeContainer pack = Pack((UpkeepEngine.PoisonedRationsObjectId, 1));
        var conditions = new ActorConditions();
        conditions[ActorCondition.Starving] = 60;

        Meal meal = UpkeepEngine.ConsumeRations(pack, conditions, Lookup);

        Assert.Equal(Meal.PoisonedRations, meal);
        Assert.Equal(4, conditions[ActorCondition.Poisoned]);
        Assert.Equal(60, conditions[ActorCondition.Starving]);
    }

    [Fact]
    public void SpoiledRationsAreStillPreferredToPoisonedOnes() {
        RuntimeContainer pack = Pack(
            (UpkeepEngine.PoisonedRationsObjectId, 2),
            (UpkeepEngine.SpoiledRationsObjectId, 2));

        Assert.Equal(Meal.SpoiledRations,
            UpkeepEngine.ConsumeRations(pack, new ActorConditions(), Lookup));
    }

    [Fact]
    public void AnEmptyPackMeansGoingHungry() {
        var conditions = new ActorConditions();

        Meal meal = UpkeepEngine.ConsumeRations(Pack(), conditions, Lookup);

        Assert.Equal(Meal.WentHungry, meal);
        Assert.Equal(5, conditions[ActorCondition.Starving]);
    }

    [Fact]
    public void HungerAccumulatesDayAfterDay() {
        var conditions = new ActorConditions();
        RuntimeContainer empty = Pack();

        for (var day = 0; day < 4; day++) {
            UpkeepEngine.ConsumeRations(empty, conditions, Lookup);
        }

        Assert.Equal(20, conditions[ActorCondition.Starving]);
    }

    [Fact]
    public void EatingTheLastRationTakesItOutOfThePack() {
        RuntimeContainer pack = Pack((UpkeepEngine.RationsObjectId, 1));

        UpkeepEngine.ConsumeRations(pack, new ActorConditions(), Lookup);

        Assert.Empty(pack.Items);
    }

    // ---- the primitive ----------------------------------------------------

    [Fact]
    public void ConsumingFromAStackTakesExactlyOne() {
        RuntimeContainer pack = Pack((UpkeepEngine.RationsObjectId, 9));

        Assert.True(InventoryConsume.TryConsumeOne(pack, UpkeepEngine.RationsObjectId, Lookup));

        Assert.Equal(8, pack.Items[0].Variable);
        Assert.Single(pack.Items);
    }

    [Fact]
    public void ConsumingMarksThePackForSaving() {
        RuntimeContainer pack = Pack((UpkeepEngine.RationsObjectId, 3));
        pack.Dirty = false;

        InventoryConsume.TryConsumeOne(pack, UpkeepEngine.RationsObjectId, Lookup);

        Assert.True(pack.Dirty);
    }

    [Fact]
    public void ATorchIsNeverConsumedThisWay() {
        // Otherwise the daily meal could burn the party's light source.
        RuntimeContainer pack = Pack((InventoryConsume.TorchObjectId, 5));

        Assert.False(InventoryConsume.TryConsumeOne(pack, InventoryConsume.TorchObjectId, Lookup));
        Assert.Equal(5, CountOf(pack, InventoryConsume.TorchObjectId));
    }

    [Fact]
    public void AnItemThatEmptiesRatherThanVanishingStaysInThePack() {
        RuntimeContainer pack = Pack((200, 1));

        Assert.True(InventoryConsume.TryConsumeOne(pack, 200, Lookup));

        Assert.Single(pack.Items);
        Assert.Equal(0, pack.Items[0].Variable);
    }

    [Fact]
    public void AnAlreadyEmptyOneIsNotAMeal_AndTheSearchGoesOn() {
        RuntimeContainer pack = Pack((200, 0), (200, 4));

        Assert.True(InventoryConsume.TryConsumeOne(pack, 200, Lookup));

        Assert.Equal(0, pack.Items[0].Variable);
        Assert.Equal(3, pack.Items[1].Variable);
    }

    [Fact]
    public void NothingOfThatKindMeansNothingTaken() {
        RuntimeContainer pack = Pack((UpkeepEngine.RationsObjectId, 2));

        Assert.False(InventoryConsume.TryConsumeOne(pack, UpkeepEngine.PoisonedRationsObjectId, Lookup));
        Assert.Equal(2, CountOf(pack, UpkeepEngine.RationsObjectId));
    }
}
