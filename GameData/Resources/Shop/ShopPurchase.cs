namespace GameData.Resources.Shop;

using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;

/// <summary>
/// What a purchase does beyond moving the item — the special cases inside <c>BuyItem</c> @0x5b6d2.
/// </summary>
/// <remarks>
/// Three shop goods are not what they appear to be on the shelf, and all three are decided by
/// OBJECT ID rather than by anything the shop knows:
/// <list type="bullet">
///   <item>a day's rations is sold as one thing and delivered as another;</item>
///   <item>the three tavern drinks are drunk at the counter and never reach a pack;</item>
///   <item>food bought by someone starving is eaten on the spot.</item>
/// </list>
/// </remarks>
public static class ShopPurchase {
    /// <summary>What the tavern sells a day's food as (object 134).</summary>
    /// <remarks>
    /// <b>Typed as a Drink, delivered as Rations.</b> The record even carries the substitution in
    /// its effect arguments (A=72, the Rations id; B=1, its variable), though the original hardcodes
    /// both rather than reading them — so the data and the code agree and neither is a guess.
    /// </remarks>
    public const int DaysRationsObjectId = 134;

    /// <summary>Quegian Brandy (135) — the first of the three drinks consumed at the counter.</summary>
    public const int FirstCounterDrinkObjectId = 135;

    /// <summary>Keshian Ale (137) — the last of them.</summary>
    public const int LastCounterDrinkObjectId = 137;

    /// <summary>Drunk 100 is the ceiling; at it the shopkeeper's companion calls a halt.</summary>
    public const int MaxDrunk = ActorConditions.MaxRank;

    /// <summary>The health-and-stamina lift a drink gives, in the engine's 1/256 units.</summary>
    private const int DrinkRestores = 3 * 256;

    /// <summary>How full a drink is allowed to refill the pool.</summary>
    private const int DrinkRestoreCapPercent = 60;

    /// <summary>
    /// The item the buyer actually receives for <paramref name="bought"/>.
    /// </summary>
    /// <remarks>
    /// Only a day's rations differs from what was on the shelf. The substitution happens BEFORE the
    /// room check and before the type tests, so everything downstream — including whether a starving
    /// buyer eats it immediately — reads the Rations record, not the Drink one it was sold under.
    /// </remarks>
    public static RuntimeItem Delivered(RuntimeItem bought) =>
        bought != null && bought.ObjectId == DaysRationsObjectId
            ? new RuntimeItem((byte)UpkeepEngine.RationsObjectId, 1, 0)
            : bought;

    /// <summary>
    /// Whether this object is drunk where it is bought rather than carried away.
    /// </summary>
    /// <remarks>
    /// <b>An id range, not the Drink object type.</b> Four records are typed Drink and only three
    /// are drinks — a day's rations is the fourth, and it sits at 134, immediately below the range.
    /// Testing the type here would have the tavern pour the party's food down someone's throat.
    /// </remarks>
    public static bool IsCounterDrink(int objectId) =>
        objectId >= FirstCounterDrinkObjectId && objectId <= LastCounterDrinkObjectId;

    /// <summary>
    /// Drinking one at the counter.
    /// </summary>
    /// <param name="drink">The drink's record; <c>EffectArgB</c> is how much drunker it makes you.</param>
    /// <returns>
    /// False when the drinker is already at <see cref="MaxDrunk"/> — nothing is applied and the
    /// caller must hand the money back, which is what the original does before saying so.
    /// </returns>
    /// <remarks>
    /// A drink is bought and gone: it never enters an inventory, so there is no room check and
    /// nothing to carry. Besides the drunkenness it settles the stomach — hunger cleared outright,
    /// and a small lift to health and stamina that cannot fill them past
    /// <see cref="DrinkRestoreCapPercent"/>%.
    /// </remarks>
    public static bool Drink(ObjectInfo drink, ActorConditions conditions,
        ActorStat health, ActorStat stamina) {
        if (drink == null || conditions == null) {
            return false;
        }
        if (conditions[ActorCondition.Drunk] >= MaxDrunk) {
            return false;
        }

        ConditionEngine.Apply(conditions, ActorCondition.Drunk, drink.EffectArgB,
            health, stamina, inCombat: false);
        ConditionEngine.Apply(conditions, ActorCondition.Starving, -100,
            health, stamina, inCombat: false);
        StatEngine.ModifyHealthPool(health, stamina, DrinkRestores, DrinkRestoreCapPercent, out _);

        return true;
    }
}
