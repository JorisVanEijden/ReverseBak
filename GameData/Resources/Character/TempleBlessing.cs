namespace GameData.Resources.Character;

/// <summary>
/// The temple's weapon-blessing service — <c>modalscreen_req_inv_run</c>
/// (<c>SRC/SCREENS/MODALSCR.C</c>), reached from a temple's dialog loop
/// (<c>SRC/SCREENS/TOWNSCN.C</c>).
///
/// <para><b>It is not a screen of its own.</b> The temple runs the ordinary inventory screen in a
/// second mode, where clicking an item offers to bless it instead of using it — the same trick the
/// picklock screen uses. Anything built for this should extend the inventory screen, not sit beside
/// it.</para>
///
/// <para>The bonus a blessing grants is elsewhere and already ported
/// (<c>CombatFormulas.ApplyEquippedBlessing</c>, <c>ShopPricing</c>). This is only the transaction:
/// what it costs, what may be blessed, and what the flags end up as.</para>
/// </summary>
public static class TempleBlessing {
    /// <summary>All three blessing bits — the test for "already blessed, at any tier".</summary>
    public const ItemFlags AnyBlessing = ItemFlags.Blessed1 | ItemFlags.Blessed2 | ItemFlags.Blessed3;

    /// <summary>The lowest tier's bit; higher tiers are this shifted left.</summary>
    public const int FirstTierFlag = (int)ItemFlags.Blessed1;

    /// <summary>Blessing tiers a temple can offer.</summary>
    public const int TierCount = 3;

    /// <summary>Dialog offering to replace an existing blessing.</summary>
    public const long AlreadyBlessedDialogId = 0x13d66f;

    /// <summary>Dialog quoting the price.</summary>
    public const long PriceOfferDialogId = 0x13d670;

    /// <summary>Dialog refusing an item that cannot be blessed.</summary>
    public const long CannotBlessDialogId = 0x13d671;

    /// <summary>The save-state flag the price dialog leaves the player's answer in.</summary>
    public const int AcceptedFlag = 0x104;

    /// <summary>
    /// Whether the temple will bless this kind of item.
    /// </summary>
    /// <remarks>
    /// <b>Swords and armour only.</b> Not crossbows and not staves — so a magician's staff can never
    /// be blessed, though the blessing bonus itself applies to whatever is equipped. Anything else
    /// gets <see cref="CannotBlessDialogId"/> and no price.
    /// </remarks>
    public static bool CanBless(ObjectType category) =>
        category == ObjectType.Sword || category == ObjectType.Armor;

    /// <summary>Whether the item already carries a blessing of any tier.</summary>
    public static bool IsBlessed(ItemFlags flags) => (flags & AnyBlessing) != 0;

    /// <summary>
    /// What the temple charges to bless an item.
    /// </summary>
    /// <param name="basePrice">The item's base price from the item table.</param>
    /// <param name="pricePercent">
    /// The temple's percentage of that base price. canassa calls this <c>qty_mult</c>; it is not a
    /// quantity multiplier.
    /// </param>
    /// <param name="surcharge">
    /// The temple's flat fee, in <b>tens</b>. canassa calls this <c>tax</c>. Both come from the
    /// temple actor's event-state record, so different temples charge differently.
    /// </param>
    /// <remarks>
    /// The percentage part truncates before the flat fee is added, so the two do not commute.
    /// </remarks>
    public static long Price(int basePrice, int pricePercent, int surcharge) =>
        ((long)basePrice * pricePercent / 100) + ((long)surcharge * 10);

    /// <summary>
    /// The flags an item ends up with after being blessed at <paramref name="tier"/>.
    /// </summary>
    /// <param name="tier">The tier this temple grants, 1-3. Out-of-range tiers leave the item alone.</param>
    /// <remarks>
    /// <b>Blessings do not stack, and a lower tier REPLACES a higher one.</b> The original clears all
    /// three bits before setting the new one, so paying a tier-1 temple to bless a tier-3 sword makes
    /// it worse — which is exactly why the screen asks <see cref="AlreadyBlessedDialogId"/> first
    /// rather than silently upgrading. A port that OR-ed the new bit in would quietly turn every
    /// re-blessing into a free upgrade.
    /// </remarks>
    public static ItemFlags Bless(ItemFlags flags, int tier) {
        if (tier < 1 || tier > TierCount) {
            return flags;
        }
        return (flags & ~AnyBlessing) | (ItemFlags)(FirstTierFlag << (tier - 1));
    }

    /// <summary>
    /// The value the price dialog is handed to pick its wording — <c>nEvtArgCount</c>, set to 1 for
    /// armour and 0 for a sword.
    /// </summary>
    public static int OfferWordingFor(ObjectType category) => category == ObjectType.Armor ? 1 : 0;
}
