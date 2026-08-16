namespace GameData.Resources.Dialog;

using GameData.Money;

/// <summary>
/// The two object ids a dialog's <c>GiveItem</c> uses to mean <b>money</b> rather than an item.
/// </summary>
/// <remarks>
/// <b>Money is not an inventory item, and that is the whole point of this class.</b> A GiveItem
/// naming one of these two ids adds to the purse and touches no pack, so it needs none of the
/// actor-inventory machinery every other GiveItem does — which is exactly the distinction that got
/// lost: the chapter-setup path deferred <i>all</i> GiveItems on the grounds that they need an
/// inventory mutator, and the party therefore started the game with nothing instead of the money
/// the original hands them.
///
/// <para>Note the asymmetry with a real item: for money <c>Amount</c> is a COUNT, while for an item
/// it is the condition of the single item given.</para>
/// </remarks>
public static class DialogMoneyGrant {
    /// <summary>Object id that means sovereigns rather than an item.</summary>
    public const int SovereignObjectId = 53;

    /// <summary>Object id that means royals rather than an item.</summary>
    public const int RoyalObjectId = 54;

    /// <summary>Whether this object id is money, and so goes to the purse.</summary>
    public static bool IsMoney(int objectId) =>
        objectId == SovereignObjectId || objectId == RoyalObjectId;

    /// <summary>
    /// The royals a money grant is worth — the unit the purse is kept in.
    /// </summary>
    /// <param name="objectId">The grant's object id.</param>
    /// <param name="amount">The count. Zero for anything that is not money.</param>
    public static int RoyalsFor(int objectId, int amount) => objectId switch {
        SovereignObjectId => amount * MoneyFormatter.RoyalsPerSovereign,
        RoyalObjectId => amount,
        _ => 0,
    };
}
