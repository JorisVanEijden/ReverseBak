namespace GameData.Resources.Shop;

using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System;

/// <summary>
/// The tinker at a shop — <c>modalscreen_inventory_request</c> (MODALSCR.C:503), which the town
/// scene reaches on hotspot action code 16.
///
/// <para><b>It is the inventory screen in mode 2, not a shop.</b> The routine sets
/// <c>g_inventory_screen_mode = 2</c> and loads the same <c>req_inv.dat</c> / <c>INVENTOR.SCR</c>
/// the ordinary pack view uses: the player browses the party's own items and the mender mends them.
/// Nothing is bought.</para>
///
/// <para>Two numbers parameterise the whole thing, and both come from the location's shop block:
/// <see cref="Data.SaveGameContainerShopData.RepairCategories"/> (which categories this mender will
/// touch) and <see cref="Data.SaveGameContainerShopData.RepairCostMarkup"/> (what he charges over
/// the base rate). They are <c>bInvreq_arg_x</c> and <c>bInvreq_arg_y</c> in the original's
/// <c>ActorSubrec04_EventState</c> — the same two bytes, at the same offsets.</para>
///
/// <para>Pure functions over plain values. The screen owns the loop, the gold and the clock.</para>
/// </summary>
public static class ShopRepair {
    /// <summary>"That will be N. Do you agree?" — the quote, whose answer decides the repair.</summary>
    /// <remarks>0x1B7763. The original reads the player's answer out of event key 0x104.</remarks>
    public const int QuoteDialogId = 1800035;

    /// <summary>"He cannot mend that" — the item's category is not in this mender's mask.</summary>
    /// <remarks>0x1B7764. Also what an item of any OTHER category gets, which is most of a pack.</remarks>
    public const int CannotMendDialogId = 1800036;

    /// <summary>"This needs no repair" — the item is already at full condition.</summary>
    /// <remarks>0x1B7765. Reached only for an item the mender WOULD have taken.</remarks>
    public const int NeedsNoRepairDialogId = 1800037;

    /// <summary>The answer key <see cref="QuoteDialogId"/> writes: non-zero means the player agreed.</summary>
    public const int AgreedEventKey = 0x104;


    /// <summary>The object whose price sets the cost of restringing <see cref="BessyMaulerId"/>.</summary>
    public const int HeavyBowstringId = 0x4c;   // 76

    /// <summary>The object whose price sets the cost of restringing any other crossbow.</summary>
    public const int LightBowstringId = 0x4d;   // 77

    /// <summary>The one crossbow that takes the heavy string.</summary>
    public const int BessyMaulerId = 0x20;      // 32, "Bessy Mauler"

    /// <summary>
    /// Which categories a mask admits. Three bits, and the seven combinations are exactly the seven
    /// arms of the shopkeeper's dialog (1800038 branches on this value as Var 18): 1 "sharpens
    /// swords", 2 "fixes armor", 4 "repairs crossbows", and the sums for the pairs and the lot.
    /// </summary>
    [Flags]
    public enum Categories {
        None = 0,
        Swords = 1,
        Armour = 2,
        Crossbows = 4,
    }

    /// <summary>The mask bit an object type answers to, or <see cref="Categories.None"/>.</summary>
    /// <remarks>
    /// <b>Only three types can be mended at all</b>, and staves, jewellery and everything else fall
    /// through to <see cref="CannotMendDialogId"/> however generous the mask is.
    /// </remarks>
    public static Categories CategoryOf(ObjectType type) => type switch {
        ObjectType.Sword => Categories.Swords,
        ObjectType.Armor => Categories.Armour,
        ObjectType.Crossbow => Categories.Crossbows,
        _ => Categories.None,
    };

    /// <summary>Will this mender touch that item?</summary>
    public static bool Mends(int repairCategories, ObjectType type) {
        Categories c = CategoryOf(type);
        return c != Categories.None && ((Categories)repairCategories & c) == c;
    }

    /// <summary>
    /// What the repair costs, in royals.
    /// </summary>
    /// <remarks>
    /// <b>Two formulas, and the crossbow's is the odd one.</b> A sword or a piece of armour is
    /// priced by how worn it is — <c>basePrice * markup * (100 - condition) / 10000</c>, so a nearly
    /// pristine blade is nearly free and the shop's markup scales the lot. A crossbow is not
    /// repaired but <b>restrung</b>: the price is twice the catalogue price of the bowstring it
    /// takes, flat, with no reference to its condition and none to the shop's markup at all.
    ///
    /// <para>Integer division at the end, as the original's <c>/ 10000L</c> is — the truncation is
    /// visible in the quote the player is shown.</para>
    /// </remarks>
    /// <param name="item">The item being priced.</param>
    /// <param name="record">Its object record (for <see cref="ObjectInfo.Price"/> and type).</param>
    /// <param name="repairCostMarkup">The shop block's markup, percent.</param>
    /// <param name="lookup">Object records by id, for the crossbow's bowstring.</param>
    public static int PriceFor(RuntimeItem item, ObjectInfo record, int repairCostMarkup,
        Func<int, ObjectInfo> lookup) {
        if (item == null || record == null) {
            return 0;
        }
        if (record.ObjectType == ObjectType.Crossbow) {
            ObjectInfo str = lookup?.Invoke(
                item.ObjectId == BessyMaulerId ? HeavyBowstringId : LightBowstringId);
            return (str?.Price ?? 0) * 2;
        }

        return (int)((long)record.Price * repairCostMarkup * (100 - Condition(item)) / 10000L);
    }

    /// <summary>
    /// How long the visit costs, as a <b>mask</b> of what was mended — 4 for a sword, 8 for armour,
    /// 2 for a crossbow, OR-ed together across the whole visit.
    /// </summary>
    /// <remarks>
    /// <b>The original ORs these and then counts the result down</b>
    /// (<c>timeFlags |= di; ... while (timeFlags-- != 0) gstate_advance_time(0x708, ...)</c>), so the
    /// accumulated bitmask is read as a number of hours — <see cref="GameState.GameTime.UnitsPerHour"/>
    /// is 1800 and 0x708 is 1800. Two consequences the player actually feels, both faithful:
    /// mending three swords costs the same four hours as mending one, and a sword plus a piece of
    /// armour costs twelve rather than eight or four.
    ///
    /// <para>Whether the authors meant a mask or a count is not decidable from the code; what it
    /// DOES is a count, so that is what this reproduces. Do not "fix" it into a per-item sum.</para>
    /// </remarks>
    public static int TimeBitFor(ObjectType type) => type switch {
        ObjectType.Sword => 4,
        ObjectType.Armor => 8,
        ObjectType.Crossbow => 2,
        _ => 0,
    };

    /// <summary>Hours the accumulated mask costs — the mask, read as a number.</summary>
    public static int HoursFor(int accumulatedTimeMask) =>
        accumulatedTimeMask < 0 ? 0 : accumulatedTimeMask;

    /// <summary>
    /// What the player is told when the item is clicked, before any money changes hands.
    /// </summary>
    public enum Outcome {
        /// <summary>Not something this mender works on — <see cref="CannotMendDialogId"/>.</summary>
        CannotMend,

        /// <summary>His kind of work, but the item is unworn — <see cref="NeedsNoRepairDialogId"/>.</summary>
        NeedsNoRepair,

        /// <summary>Quote it and wait for the answer — <see cref="QuoteDialogId"/>.</summary>
        Quote,
    }

    /// <summary>Which of the three the click leads to.</summary>
    /// <remarks>
    /// The category test comes FIRST and the condition test second, which is why a pristine sword at
    /// a mender who only does armour is told "he cannot mend that" rather than "it needs no repair".
    /// </remarks>
    public static Outcome OutcomeFor(RuntimeItem item, ObjectInfo record, int repairCategories) {
        if (item == null || record == null || !Mends(repairCategories, record.ObjectType)) {
            return Outcome.CannotMend;
        }
        // *** `!=`, NOT `<`. *** The original's test is `slot_ptr->condition != 100`, and the
        // price formula below it is `(100 - condition)`, so a condition ABOVE 100 would be quoted a
        // negative price and pay the player. Nothing shipped can produce one — degradation only
        // subtracts and every repair path clamps to exactly 100 — so this is left as the original
        // has it rather than papered over with a clamp that would hide a real divergence if one
        // ever appeared.
        return Condition(item) == InventoryQuery.PristineCondition
            ? Outcome.NeedsNoRepair : Outcome.Quote;
    }

    /// <summary>
    /// Hand the item back mended: full condition, and the damaged flag cleared.
    /// </summary>
    /// <remarks>
    /// <see cref="ItemFlags.Repairable"/> is a STATE ("this is damaged"), not a capability, so
    /// clearing it is what "no longer needs work" looks like — the same bit
    /// <c>party_surveyArmourAndOptionallyRepair</c> clears in the field.
    /// </remarks>
    public static void Apply(RuntimeItem item) {
        if (item == null) {
            return;
        }
        item.Variable = InventoryQuery.PristineCondition;
        item.ItemFlags &= unchecked((ushort)~(ushort)ItemFlags.Repairable);
    }

    // Condition lives in the slot's Variable byte for everything the mender handles — the same
    // field the inventory screen prints as a percentage.
    private static int Condition(RuntimeItem item) => item.Variable;
}
