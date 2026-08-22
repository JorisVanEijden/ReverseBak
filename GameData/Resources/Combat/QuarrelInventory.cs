namespace GameData.Resources.Combat;

using GameData.Resources.Inventory;

/// <summary>
/// How much ammunition a combatant is carrying — <c>combat_actor_cnt_qrls_kind</c>
/// (canassa CACTOR.C:403).
///
/// <para>Feeds two things: the Shoot half of the HUD's capability cell
/// (<see cref="CombatCapability.CanShoot"/>, which asks for the total), and the SHOOT menu's per-kind
/// availability (<see cref="CombatMenuSlots.QuarrelIsAvailable"/>).</para>
/// </summary>
public static class QuarrelInventory {
    /// <summary>
    /// The object id holding each quarrel kind, in kind order.
    /// </summary>
    /// <remarks>
    /// <b>The ids are NOT in kind order, and the two out-of-order entries are easy to miss.</b> The
    /// original's switch maps <c>0x2a</c> to slot 3 and <c>0x27</c> to slot 4 — so a port that
    /// assumes 0x24..0x2b run straight through swaps two kinds, and an archer's bolts silently count
    /// as the wrong type.
    /// </remarks>
    public static readonly int[] ObjectIdByKind = { 0x24, 0x25, 0x26, 0x2a, 0x27, 0x28, 0x29, 0x2b };

    /// <summary>Ask for every kind at once — the original's <c>kind == -1</c>.</summary>
    public const int AllKinds = -1;

    /// <summary>The kind an object id holds, or -1 when it is not ammunition.</summary>
    public static int KindOf(int objectId) {
        for (var kind = 0; kind < ObjectIdByKind.Length; kind++) {
            if (ObjectIdByKind[kind] == objectId) {
                return kind;
            }
        }
        return -1;
    }

    /// <summary>
    /// Quarrels carried, of one kind or of every kind.
    /// </summary>
    /// <param name="container">The combatant's pack.</param>
    /// <param name="kind">A kind, or <see cref="AllKinds"/> for the total.</param>
    /// <remarks>
    /// <b>Deliberately not <see cref="InventoryQuery.CountByKind"/>, and the difference matters.</b>
    /// That helper treats an entry whose <c>Variable</c> is zero as ONE item, which is right for
    /// ordinary goods. Ammunition stores its quantity there, and the original sums the field raw —
    /// so <b>an empty quiver must answer 0, where CountByKind would answer 1</b> and hand the archer
    /// a shot they cannot take.
    ///
    /// <para>The original also allocates ten slots for eight kinds; the last two are never written
    /// and contribute nothing to the total.</para>
    /// </remarks>
    public static int Count(RuntimeContainer container, int kind = AllKinds) {
        if (container == null) {
            return 0;
        }

        var total = 0;
        foreach (RuntimeItem item in container.Items) {
            if (item == null) {
                continue;
            }
            int itemKind = KindOf(item.ObjectId);
            if (itemKind < 0) {
                continue;
            }
            if (kind == AllKinds || itemKind == kind) {
                total += item.Variable;
            }
        }
        return total;
    }
}
