namespace GameData.Resources.Shop;

using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System;
using System.Collections.Generic;

/// <summary>
/// Shop money: list prices, what an item is worth, what a shop pays for it, and haggling.
///
/// <para>Ported from the original's <c>SRC/SCREENS/SHOP.C</c> and <c>SRC/SCREENS/ITEMTBL.C</c>
/// (<c>shop_items_compute_actor_prices</c>, <c>itemtbl_compute_value</c>,
/// <c>itemtbl_slot_value_modifier</c>, <c>shop_sell_item</c>, <c>shop_haggle_attempt_purchase</c>,
/// <c>shop_rand_max_of_3</c>), cross-read against <c>docs/shop-pricing.md</c>. These are gameplay
/// numbers, so the integer truncation is deliberate and load-bearing: the original divides at each
/// step in 16/32-bit integer maths and the rounding is visible in the prices the player sees. Do not
/// "simplify" the arithmetic into one expression.</para>
///
/// <para>Pure functions over plain values — no game state, no RNG of its own. Randomness enters only
/// through the <c>rnd</c> delegate the caller supplies, so the whole thing is testable.</para>
/// </summary>
public static class ShopPricing {
    /// <summary>Number of object types the price table covers (the original's fixed 0x8a loop).</summary>
    public const int PriceTableLength = 0x8a;

    /// <summary>Object id of the Magical Scroll, which is priced by the spell it carries rather than
    /// by any list price.</summary>
    public const int MagicalScrollObjectId = 0x85;

    /// <summary>The zone-3 story shop's 6x exchange rate. Everywhere else the rate is 100.</summary>
    public const int InflatedExchangeRate = 600;

    private const int NormalExchangeRate = 100;

    /// <summary>Object flags that make an item priced by remaining charges. The original tests the
    /// pair as one mask (<c>wFlags &amp; 0xa000</c>).</summary>
    private const ObjectFlags ChargePricedMask = ObjectFlags.LimitedUses | ObjectFlags.B8000;

    /// <summary>
    /// A shop's list price for one object type — what the player pays to buy a fresh one.
    /// </summary>
    /// <param name="basePrice">The object's catalogue price (<see cref="ObjectInfo.Price"/>).</param>
    /// <param name="markupPercent">This shop's markup (shop block +1).</param>
    /// <param name="exchangeRatePercent">Normally 100; <see cref="InflatedExchangeRate"/> in the
    /// zone-3 story shop.</param>
    /// <remarks>Truncates twice, exactly as the original does — markup first, then exchange rate.
    /// Folding them into a single division gives different prices.</remarks>
    public static int ListPrice(int basePrice, int markupPercent, int exchangeRatePercent = NormalExchangeRate) {
        long withMarkup = (long)basePrice * (markupPercent + 100) / 100;
        return (int)(withMarkup * exchangeRatePercent / 100);
    }

    /// <summary>
    /// Builds the whole per-shop price table, one entry per object type, in object-id order.
    /// </summary>
    public static int[] BuildPriceTable(
        IReadOnlyList<int> basePrices, int markupPercent, int exchangeRatePercent = NormalExchangeRate) {
        if (basePrices == null) {
            throw new ArgumentNullException(nameof(basePrices));
        }
        int count = Math.Min(basePrices.Count, PriceTableLength);
        var table = new int[count];
        for (var objectId = 0; objectId < count; objectId++) {
            table[objectId] = ListPrice(basePrices[objectId], markupPercent, exchangeRatePercent);
        }
        return table;
    }

    /// <summary>
    /// Whether the zone-3 story shop is currently charging its inflated rate. Kept as a predicate so
    /// the story-flag reads stay with the caller that owns game state.
    /// </summary>
    public static int ExchangeRate(int zoneId, int shopType, bool inflationFlagSet, bool inflationEndedFlagSet) =>
        zoneId == 3 && shopType == 2 && inflationFlagSet && !inflationEndedFlagSet
            ? InflatedExchangeRate
            : NormalExchangeRate;

    /// <summary>
    /// The item's condition as a percentage, which is also its value multiplier. Can exceed 100:
    /// blessings raise it, so a blessed item is worth more than a pristine plain one.
    /// </summary>
    /// <remarks>
    /// Blessing tiers multiply the <i>running</i> value and each truncates, so they do not commute
    /// and cannot be collapsed into one factor. Broken wins outright and zeroes the result.
    /// </remarks>
    public static int ConditionPercent(RuntimeItem item, ObjectInfo info) {
        var itemFlags = (ItemFlags)item.ItemFlags;
        int value = (info.Flags & ObjectFlags.Degradable) != 0 ? item.Variable : 100;

        if ((itemFlags & ItemFlags.Blessed1) != 0) {
            value = value * 6 / 4;
        }
        if ((itemFlags & ItemFlags.Blessed2) != 0) {
            value = value * 7 / 4;
        }
        if ((itemFlags & ItemFlags.Blessed3) != 0) {
            value = value * 8 / 4;
        }
        if ((itemFlags & ItemFlags.Broken) != 0) {
            value = 0;
        }
        return value;
    }

    /// <summary>
    /// What one specific item is worth at this shop — the shop's list price for its type, adjusted
    /// for condition or remaining charges. Chains off the list price, so a dearer shop also values
    /// the player's goods more highly.
    /// </summary>
    /// <param name="listPrice">This shop's price-table entry for the item's type. A negative entry
    /// means "not for sale" and is returned unchanged.</param>
    /// <param name="spellPrice">For a Magical Scroll only: the price of the spell it carries
    /// (the spell-price table indexed by <see cref="RuntimeItem.Variable"/>). Ignored otherwise.</param>
    public static long ItemValue(RuntimeItem item, ObjectInfo info, int listPrice, int spellPrice) {
        if (item.ObjectId == MagicalScrollObjectId) {
            return spellPrice;
        }

        long value = listPrice;
        if (value >= 0) {
            if ((info.Flags & ObjectFlags.Degradable) != 0) {
                value = value * ConditionPercent(item, info) / 100;
            } else if ((info.Flags & ChargePricedMask) != 0 && info.MaxCharges > 0) {
                // MaxCharges is the denominator; the original does not guard it because every record
                // carrying these flags has a non-zero value. An override could ship 0, so we skip the
                // proration rather than divide by zero and keep the undiscounted list price.
                value = value * item.Variable / info.MaxCharges;
            }
        }

        // The floor applies only when the type is sellable at all — a negative list price stays
        // negative so callers can still recognise "not for sale".
        if (listPrice >= 0 && value <= 1) {
            value = 1;
        }
        return value;
    }

    /// <summary>
    /// What the shop pays the player for an item: its worth scaled by the shop's markdown, halved for
    /// armour, and never less than 1.
    /// </summary>
    public static long SellPrice(long itemValue, int markDownPercent, ObjectType objectType) {
        long price = itemValue * markDownPercent / 100;
        if (objectType == ObjectType.Armor) {
            price /= 2;
        }
        return price < 1 ? 1 : price;
    }

    /// <summary>
    /// Whether the shop will consider buying this item at all. Note the escape: a shop always buys
    /// something it already has in stock, even a worthless item or one outside the categories it
    /// trades in.
    /// </summary>
    /// <remarks>
    /// docs/shop-pricing.md described the zero-price check as an unconditional first gate. It is not:
    /// in <c>shop_sell_item</c> the already-in-stock escape covers the price test and the category
    /// test together, so a 0-price item is still accepted when the shop stocks that type.
    /// </remarks>
    public static bool WillBuy(int basePrice, ShopItemCategories itemCategories,
                               ShopItemCategories shopCategories, bool alreadyInStock) =>
        alreadyInStock || (basePrice != 0 && (itemCategories & shopCategories) != 0);

    /// <summary>Max of three rolls of <c>rnd(modulus)</c>, or 0 when the modulus is 0. Skill checks
    /// use this rather than a flat roll, which biases results towards the top of the range.</summary>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    public static int MaxOfThreeRolls(int modulus, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        var max = 0;
        if (modulus != 0) {
            for (var i = 0; i < 3; i++) {
                int roll = rnd(modulus);
                if (roll > max) {
                    max = roll;
                }
            }
        }
        return max;
    }

    /// <summary>
    /// One haggle attempt on the buy side. This is the only place the Haggling skill moves a price;
    /// markup and markdown are flat per-shop constants.
    /// </summary>
    /// <param name="currentPrice">The shop's current price-table entry for this object type.</param>
    /// <param name="unhaggledPrice">What that entry would be if it had never been haggled, i.e.
    /// <c>basePrice × (markup+100)/100</c>. The original compares against this to allow only one
    /// successful haggle per object type.
    /// <para><b>Computed without the exchange rate</b>, which is not an oversight to fix: in the
    /// zone-3 6x shop the table entry never equals it, so haggling silently cannot engage there.
    /// Pass the value the original would compute, or you will change that behaviour.</para></param>
    /// <param name="haggleSkill">The haggling party member's Haggling skill (stat 12).</param>
    /// <param name="shopkeeperSkill">Shop block +4.</param>
    /// <param name="maxDiscountPercent">Shop block +2. Zero disables haggling at this shop.</param>
    /// <param name="refuseChancePercent">Shop block +5 — the chance a failed haggle makes the
    /// shopkeeper refuse to sell this type at all for the rest of the visit.</param>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    public static HaggleOutcome Haggle(
        long currentPrice, long unhaggledPrice, int haggleSkill, int shopkeeperSkill,
        int maxDiscountPercent, int refuseChancePercent, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }

        // Only negotiable while the price is still the untouched list value, and only where the shop
        // allows a discount at all.
        bool negotiable = maxDiscountPercent != 0 && currentPrice == unhaggledPrice;

        if (negotiable) {
            int partyRoll = MaxOfThreeRolls(haggleSkill, rnd);
            int merchantRoll = MaxOfThreeRolls(shopkeeperSkill, rnd);

            if (partyRoll > merchantRoll) {
                int discount = MaxOfThreeRolls(maxDiscountPercent / 2 + (partyRoll - merchantRoll), rnd);
                if (discount > maxDiscountPercent) {
                    discount = maxDiscountPercent;
                }
                long newPrice = currentPrice - currentPrice * discount / 100;
                if (newPrice < 1) {
                    newPrice = 1;
                }
                return HaggleOutcome.Won(newPrice, discount);
            }

            return Failed(currentPrice, partyRoll, refuseChancePercent, rnd);
        }

        // Not negotiable. The original still runs both failure rolls here, but the consolation roll
        // reads an uninitialised local (partyRoll is only assigned inside the negotiable branch), so
        // its result is whatever happened to be on the stack. That cannot be reproduced, and inventing
        // a value would hand out XP for an attempt the player never got to make — so only the
        // refuse-to-sell roll is kept, which is well-defined.
        return Failed(currentPrice, partyRoll: null, refuseChancePercent, rnd);
    }

    private static HaggleOutcome Failed(long price, int? partyRoll, int refuseChancePercent, Func<int, int> rnd) {
        bool consolationXp = partyRoll.HasValue && rnd(100) < (100 - partyRoll.Value) / 5;
        bool refused = rnd(100) < refuseChancePercent;
        return HaggleOutcome.Lost(price, consolationXp, refused);
    }
}

/// <summary>Result of one <see cref="ShopPricing.Haggle"/> attempt.</summary>
public sealed class HaggleOutcome {
    private HaggleOutcome(bool succeeded, long price, int discountPercent, bool partyXp, bool haggerXp, bool refusedToSell) {
        Succeeded = succeeded;
        Price = price;
        DiscountPercent = discountPercent;
        PartyHagglingXp = partyXp;
        HagglerHagglingXp = haggerXp;
        ShopkeeperRefusedToSell = refusedToSell;
    }

    /// <summary>True when the player out-rolled the shopkeeper and the price came down.</summary>
    public bool Succeeded { get; }

    /// <summary>The price after the attempt — unchanged unless <see cref="Succeeded"/>.</summary>
    public long Price { get; }

    /// <summary>Percentage knocked off, 0 on failure.</summary>
    public int DiscountPercent { get; }

    /// <summary>Award 1 Haggling experience across the party.</summary>
    public bool PartyHagglingXp { get; }

    /// <summary>Award 1 Haggling experience to the member who did the haggling. Only a win gives
    /// this; the failure consolation is party-wide only.</summary>
    public bool HagglerHagglingXp { get; }

    /// <summary>The shopkeeper has taken offence and will not sell this object type any more. The
    /// caller should mark the price-table entry -1.</summary>
    public bool ShopkeeperRefusedToSell { get; }

    internal static HaggleOutcome Won(long price, int discountPercent) =>
        new HaggleOutcome(true, price, discountPercent, partyXp: true, haggerXp: true, refusedToSell: false);

    internal static HaggleOutcome Lost(long price, bool consolationXp, bool refused) =>
        new HaggleOutcome(false, price, 0, consolationXp, haggerXp: false, refused);
}
