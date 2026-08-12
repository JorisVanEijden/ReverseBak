namespace BetrayalAtKrondor.Tests.Shop;

using GameData;
using GameData.Resources.Inventory;
using GameData.Resources.Object;
using GameData.Resources.Shop;
using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Shop money (<c>SHOP.C</c> / <c>ITEMTBL.C</c>). The cases below pin the things a port gets wrong by
/// being tidy: the double truncation in the list price, blessing tiers that do not commute, the
/// already-in-stock escape that covers both sell gates, and the haggle guard that makes the whole
/// negotiation impossible in the inflated shop.
/// </summary>
public class ShopPricingTests {
    private static ObjectInfo Info(
        ObjectFlags flags = 0, int maxCharges = 0, ObjectType type = ObjectType.Misc, int price = 0) =>
        new ObjectInfo("test") { Flags = flags, MaxCharges = maxCharges, ObjectType = type, Price = price };

    private static RuntimeItem Item(byte objectId = 1, byte variable = 100, ItemFlags flags = 0) =>
        new RuntimeItem(objectId, variable, (ushort)flags);

    /// <summary>Deterministic stand-in for RND(n): replays a fixed script of results.</summary>
    private static Func<int, int> Rolls(params int[] results) {
        var i = 0;
        return _ => results[i++];
    }

    // ---- list price ----------------------------------------------------------------------

    [Fact]
    public void ListPriceAppliesTheShopsMarkup() {
        Assert.Equal(150, ShopPricing.ListPrice(basePrice: 100, markupPercent: 50));
    }

    [Fact]
    public void ListPriceTruncatesTwiceRatherThanOnce() {
        // markup first: 10 * 133/100 = 13 (not 13.3), then 13 * 600/100 = 78.
        // Folding into one expression would give 10 * 133 * 600 / 10000 = 79.
        Assert.Equal(78, ShopPricing.ListPrice(basePrice: 10, markupPercent: 33, exchangeRatePercent: 600));
    }

    [Fact]
    public void TheInflatedRateAppliesOnlyToTheStoryShopWhileTheFlagsSaySo() {
        Assert.Equal(600, ShopPricing.ExchangeRate(3, shopType: 2, inflationFlagSet: true, inflationEndedFlagSet: false));
        Assert.Equal(100, ShopPricing.ExchangeRate(3, shopType: 2, inflationFlagSet: true, inflationEndedFlagSet: true));
        Assert.Equal(100, ShopPricing.ExchangeRate(3, shopType: 2, inflationFlagSet: false, inflationEndedFlagSet: false));
        Assert.Equal(100, ShopPricing.ExchangeRate(4, shopType: 2, inflationFlagSet: true, inflationEndedFlagSet: false));
        Assert.Equal(100, ShopPricing.ExchangeRate(3, shopType: 1, inflationFlagSet: true, inflationEndedFlagSet: false));
    }

    [Fact]
    public void BuildPriceTableStopsAtTheObjectCount() {
        var basePrices = new List<int>();
        for (var i = 0; i < 200; i++) {
            basePrices.Add(10);
        }

        int[] table = ShopPricing.BuildPriceTable(basePrices, markupPercent: 0);

        Assert.Equal(ShopPricing.PriceTableLength, table.Length);
        Assert.All(table, entry => Assert.Equal(10, entry));
    }

    // ---- condition -----------------------------------------------------------------------

    [Fact]
    public void ANonDegradableItemIsAlwaysFullConditionWhateverItsVariableHolds() {
        // Variable is a charge count on such an item, so reading it as a percentage would be wrong.
        Assert.Equal(100, ShopPricing.ConditionPercent(Item(variable: 3), Info()));
    }

    [Fact]
    public void ADegradableItemReportsItsVariableAsThePercentage() {
        Assert.Equal(42, ShopPricing.ConditionPercent(Item(variable: 42), Info(ObjectFlags.Degradable)));
    }

    [Fact]
    public void BlessingLiftsConditionAboveFullSoABlessedItemIsWorthMore() {
        ObjectInfo info = Info(ObjectFlags.Degradable);
        Assert.Equal(150, ShopPricing.ConditionPercent(Item(variable: 100, flags: ItemFlags.Blessed1), info));
        Assert.Equal(175, ShopPricing.ConditionPercent(Item(variable: 100, flags: ItemFlags.Blessed2), info));
        Assert.Equal(200, ShopPricing.ConditionPercent(Item(variable: 100, flags: ItemFlags.Blessed3), info));
    }

    [Fact]
    public void StackedBlessingsTruncateAtEachTierInsteadOfMultiplyingOut() {
        // 33 -> *6/4 = 49 (not 49.5) -> *7/4 = 85 (not 86.6). One combined factor would give 86.
        int actual = ShopPricing.ConditionPercent(
            Item(variable: 33, flags: ItemFlags.Blessed1 | ItemFlags.Blessed2), Info(ObjectFlags.Degradable));

        Assert.Equal(85, actual);
    }

    [Fact]
    public void BrokenBeatsEveryBlessing() {
        int actual = ShopPricing.ConditionPercent(
            Item(variable: 100, flags: ItemFlags.Blessed3 | ItemFlags.Broken), Info(ObjectFlags.Degradable));

        Assert.Equal(0, actual);
    }

    // ---- item value ----------------------------------------------------------------------

    [Fact]
    public void AScrollIsPricedByItsSpellNotByAnyListPrice() {
        long value = ShopPricing.ItemValue(
            Item(objectId: (byte)ShopPricing.MagicalScrollObjectId, variable: 7),
            Info(), listPrice: 999, spellPrice: 250);

        Assert.Equal(250, value);
    }

    [Fact]
    public void AWornItemIsWorthItsConditionShareOfTheListPrice() {
        long value = ShopPricing.ItemValue(
            Item(variable: 50), Info(ObjectFlags.Degradable), listPrice: 200, spellPrice: 0);

        Assert.Equal(100, value);
    }

    [Fact]
    public void AChargedItemIsProratedByRemainingCharges() {
        long value = ShopPricing.ItemValue(
            Item(variable: 10), Info(ObjectFlags.LimitedUses, maxCharges: 40), listPrice: 200, spellPrice: 0);

        Assert.Equal(50, value);
    }

    [Fact]
    public void AChargedItemWithNoDeclaredMaximumKeepsItsListPriceRatherThanDividingByZero() {
        // Not reachable with shipped data; an override could ship it.
        long value = ShopPricing.ItemValue(
            Item(variable: 10), Info(ObjectFlags.LimitedUses, maxCharges: 0), listPrice: 200, spellPrice: 0);

        Assert.Equal(200, value);
    }

    [Fact]
    public void AnUnsellableTypeKeepsItsNegativeMarkerInsteadOfBeingFlooredToOne() {
        long value = ShopPricing.ItemValue(Item(), Info(), listPrice: -1, spellPrice: 0);

        Assert.Equal(-1, value);
    }

    [Fact]
    public void AWorthlessButSellableItemStillFetchesOne() {
        long value = ShopPricing.ItemValue(
            Item(variable: 1), Info(ObjectFlags.Degradable), listPrice: 10, spellPrice: 0);

        Assert.Equal(1, value);
    }

    // ---- sell price ----------------------------------------------------------------------

    [Fact]
    public void TheShopPaysItsMarkdownShareOfTheItemsWorth() {
        Assert.Equal(30, ShopPricing.SellPrice(itemValue: 100, markDownPercent: 30, ObjectType.Misc));
    }

    [Fact]
    public void ArmourFetchesHalfOfWhatAnythingElseWould() {
        Assert.Equal(15, ShopPricing.SellPrice(itemValue: 100, markDownPercent: 30, ObjectType.Armor));
    }

    [Fact]
    public void TheShopNeverOffersNothing() {
        Assert.Equal(1, ShopPricing.SellPrice(itemValue: 1, markDownPercent: 0, ObjectType.Misc));
    }

    // ---- will-buy gate -------------------------------------------------------------------

    [Fact]
    public void AShopBuysWithinItsCategoriesOnly() {
        Assert.True(ShopPricing.WillBuy(10, ShopItemCategories.Swords, ShopItemCategories.Swords, alreadyInStock: false));
        Assert.False(ShopPricing.WillBuy(10, ShopItemCategories.Potions, ShopItemCategories.Swords, alreadyInStock: false));
    }

    [Fact]
    public void AlreadyStockingTheTypeOverridesBothTheCategoryAndThePriceGate() {
        // The escape covers the two refusal conditions together, not just the category one: a shop
        // that already stocks a type takes another even if it is worthless and out of category.
        Assert.True(ShopPricing.WillBuy(0, ShopItemCategories.Potions, ShopItemCategories.Swords, alreadyInStock: true));
        Assert.False(ShopPricing.WillBuy(0, ShopItemCategories.Swords, ShopItemCategories.Swords, alreadyInStock: false));
    }

    // ---- rolls ---------------------------------------------------------------------------

    [Fact]
    public void ABestOfThreeRollTakesTheHighestOfItsThreeTries() {
        Assert.Equal(7, ShopPricing.MaxOfThreeRolls(10, Rolls(2, 7, 5)));
    }

    [Fact]
    public void AZeroSkillRollsNothingAtAll() {
        // Guarded in the original: RND(0) is never called.
        Assert.Equal(0, ShopPricing.MaxOfThreeRolls(0, _ => throw new InvalidOperationException("must not roll")));
    }

    // ---- haggling ------------------------------------------------------------------------

    [Fact]
    public void OutRollingTheShopkeeperBringsThePriceDownAndEarnsExperience() {
        // party best-of-3 = 60, merchant = 10, then the discount roll yields 20.
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 100, unhaggledPrice: 100, haggleSkill: 80, shopkeeperSkill: 20,
            maxDiscountPercent: 40, refuseChancePercent: 0,
            rnd: Rolls(60, 0, 0, 10, 0, 0, 20, 0, 0));

        Assert.True(outcome.Succeeded);
        Assert.Equal(20, outcome.DiscountPercent);
        Assert.Equal(80, outcome.Price);
        Assert.True(outcome.PartyHagglingXp);
        Assert.True(outcome.HagglerHagglingXp);
    }

    [Fact]
    public void TheDiscountCannotExceedWhatTheShopAllows() {
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 100, unhaggledPrice: 100, haggleSkill: 90, shopkeeperSkill: 10,
            maxDiscountPercent: 10, refuseChancePercent: 0,
            rnd: Rolls(80, 0, 0, 5, 0, 0, 99, 0, 0));

        Assert.True(outcome.Succeeded);
        Assert.Equal(10, outcome.DiscountPercent);
        Assert.Equal(90, outcome.Price);
    }

    [Fact]
    public void LosingTheRollLeavesThePriceAloneAndCanOffendTheShopkeeper() {
        // party 10 vs merchant 50, then the consolation roll fails and the refuse roll lands.
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 100, unhaggledPrice: 100, haggleSkill: 20, shopkeeperSkill: 80,
            maxDiscountPercent: 40, refuseChancePercent: 30,
            rnd: Rolls(10, 0, 0, 50, 0, 0, 99, 10));

        Assert.False(outcome.Succeeded);
        Assert.Equal(100, outcome.Price);
        Assert.False(outcome.PartyHagglingXp);
        Assert.True(outcome.ShopkeeperRefusedToSell);
    }

    [Fact]
    public void AFailedHaggleStillTeachesTheParticularlyUnlucky() {
        // partyRoll 10 -> consolation chance (100-10)/5 = 18, and the roll comes in under it.
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 100, unhaggledPrice: 100, haggleSkill: 20, shopkeeperSkill: 80,
            maxDiscountPercent: 40, refuseChancePercent: 0,
            rnd: Rolls(10, 0, 0, 50, 0, 0, 5, 50));

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.PartyHagglingXp);
        Assert.False(outcome.HagglerHagglingXp);
    }

    [Fact]
    public void APriceThatHasAlreadyBeenHaggledCannotBeHaggledAgain() {
        // The current price no longer matches the untouched list value, so no skill roll happens at
        // all - only the refuse roll, which is why a single roll script suffices here.
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 80, unhaggledPrice: 100, haggleSkill: 99, shopkeeperSkill: 1,
            maxDiscountPercent: 40, refuseChancePercent: 0,
            rnd: Rolls(0));

        Assert.False(outcome.Succeeded);
        Assert.Equal(80, outcome.Price);
        Assert.False(outcome.PartyHagglingXp);
    }

    [Fact]
    public void AShopThatAllowsNoDiscountNeverNegotiates() {
        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: 100, unhaggledPrice: 100, haggleSkill: 99, shopkeeperSkill: 1,
            maxDiscountPercent: 0, refuseChancePercent: 0,
            rnd: Rolls(0));

        Assert.False(outcome.Succeeded);
        Assert.Equal(100, outcome.Price);
    }

    [Fact]
    public void TheInflatedShopCannotBeHaggledBecauseItsPricesNeverMatchTheUnhaggledValue() {
        // Faithful quirk, not a bug to fix: the original recomputes the comparison price without the
        // exchange rate, so the guard can never pass while the 6x rate is in force.
        int listed = ShopPricing.ListPrice(100, markupPercent: 0, exchangeRatePercent: 600);
        int unhaggled = ShopPricing.ListPrice(100, markupPercent: 0);

        HaggleOutcome outcome = ShopPricing.Haggle(
            currentPrice: listed, unhaggledPrice: unhaggled, haggleSkill: 99, shopkeeperSkill: 1,
            maxDiscountPercent: 40, refuseChancePercent: 0, rnd: Rolls(0));

        Assert.False(outcome.Succeeded);
        Assert.Equal(600, outcome.Price);
    }
}
