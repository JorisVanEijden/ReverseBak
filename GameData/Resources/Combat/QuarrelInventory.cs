namespace GameData.Resources.Combat;

using GameData.Resources.Inventory;
using GameData.Resources.Object;
using System;

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


    /// <summary>
    /// The creature that shoots without carrying anything — <b>creature 0x1a</b>.
    /// </summary>
    /// <remarks>
    /// <c>combataiturn_sel_consum_qrl</c>'s first line: <c>if (creatureType == 0x1a) return 9;</c>.
    /// It returns before the count is read and before anything is consumed, and <b>9 is outside the
    /// 0..7 kind range</b> — so this creature's ammunition is innate rather than carried. A port that
    /// runs it through the ordinary path finds an empty pack and refuses every shot.
    /// </remarks>
    public const int InnateAmmoCreature = 0x1a;

    /// <summary>The kind that creature is given. Deliberately out of range.</summary>
    public const int InnateAmmoKind = 9;

    /// <summary>No kind could be used.</summary>
    public const int NoKind = -1;

    /// <summary>
    /// Which kind a shot will actually use — <c>combataiturn_sel_consum_qrl</c>'s selection half.
    /// </summary>
    /// <param name="creatureType">The shooter's creature type.</param>
    /// <param name="requestedKind">The chosen kind, or <see cref="AllKinds"/> to let it pick.</param>
    /// <param name="countOfKind">How many of a kind the shooter carries.</param>
    /// <returns>The kind to fire, or <see cref="NoKind"/> when there is nothing to fire.</returns>
    /// <remarks>
    /// <b>An unspecified kind scans 7 DOWN TO 0 and takes the first it finds</b> — the
    /// HIGHEST-numbered kind carried, not the lowest. The kinds run cheapest-first, so scanning
    /// upward would have the AI spend its best ammunition last instead of first.
    ///
    /// <para><b>A requested kind is NOT re-scanned.</b> If the player picked a kind and has none of
    /// it, the answer is <see cref="NoKind"/> — the routine does not fall back to another. Only
    /// <see cref="AllKinds"/> searches.</para>
    /// </remarks>
    public static int SelectKind(int creatureType, int requestedKind, Func<int, int> countOfKind) {
        if (creatureType == InnateAmmoCreature) {
            return InnateAmmoKind;
        }
        if (countOfKind == null) {
            return NoKind;
        }

        int picked = requestedKind;
        if (requestedKind == AllKinds) {
            for (int kind = ObjectIdByKind.Length - 1; kind >= 0; kind--) {
                if (countOfKind(kind) != 0) {
                    picked = kind;
                    break;
                }
            }
        }

        if (picked < 0 || picked >= ObjectIdByKind.Length) {
            return NoKind;
        }
        return countOfKind(picked) != 0 ? picked : NoKind;
    }

    /// <summary>
    /// Whether firing this kind takes one out of the pack.
    /// </summary>
    /// <remarks>
    /// <b>The innate-ammunition creature spends nothing</b>: its early return happens before the
    /// item-id lookup, and the consume is guarded on <c>item_id != -1</c>. Everything else spends
    /// one of <see cref="ObjectIdByKind"/>.
    /// </remarks>
    public static bool Spends(int selectedKind) =>
        selectedKind >= 0 && selectedKind < ObjectIdByKind.Length;

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

    /// <summary>Creature type whose shots come out of nowhere. See <see cref="Pick"/>.</summary>
    public const int FreeAmmoCreatureType = 0x1a;

    /// <summary>The kind <see cref="FreeAmmoCreatureType"/> always fires.</summary>
    /// <remarks>
    /// Outside the eight real kinds on purpose — it indexes nothing, so it cannot be looked up in a
    /// pack or spent. Treat it as "a quarrel appeared", not as an entry in
    /// <see cref="ObjectIdByKind"/>.
    /// </remarks>
    public const int FreeAmmoKind = 9;

    /// <summary>No quarrel could be fired.</summary>
    public const int NoQuarrel = -1;

    /// <summary>
    /// Picks the quarrel an archer fires and, unless told otherwise, takes it out of the pack.
    /// </summary>
    /// <param name="container">The archer's pack.</param>
    /// <param name="creatureType">
    /// The shooter's creature type. Only <see cref="FreeAmmoCreatureType"/> is special.
    /// </param>
    /// <param name="preferredKind">
    /// A kind to insist on, or <see cref="AllKinds"/> (the default) to let the scan choose.
    /// </param>
    /// <param name="spend">Whether the pick also consumes the quarrel.</param>
    /// <param name="lookup">Object id → record, for <see cref="InventoryConsume.TryConsumeOne"/>.</param>
    /// <returns>The kind fired, or <see cref="NoQuarrel"/> when the archer has none.</returns>
    /// <remarks>
    /// Ported from <c>combat_actor_pickQuarrelKind</c> @0x66309, the ammunition half of a monster's
    /// ranged turn (<c>monster_crossbowShotByTargetMode</c> calls it as "no preference, spend it"
    /// and treats <see cref="NoQuarrel"/> as "no shot").
    ///
    /// <para><b>The scan runs kind 7 down to 0, so an archer burns its BEST ammunition first</b> —
    /// Enchanted before Poisoned Tsurani before … before plain Quarrels. Scanning upward instead
    /// hoards the good quarrels and fires them never.</para>
    ///
    /// <para><b>Selecting and spending are one step.</b> The original takes the quarrel on the same
    /// call that chooses it, so splitting them apart moves when an archer runs dry.</para>
    /// </remarks>
    public static int Pick(RuntimeContainer container, int creatureType,
        int preferredKind = AllKinds, bool spend = true, Func<int, ObjectInfo> lookup = null) {
        // Checked before the pack is: this creature never looks, and never spends.
        if (creatureType == FreeAmmoCreatureType) {
            return FreeAmmoKind;
        }

        int kind = preferredKind;
        if (kind == AllKinds) {
            for (int k = ObjectIdByKind.Length - 1; k >= 0; k--) {
                if (Count(container, k) != 0) {
                    kind = k;
                    break;
                }
            }
        }

        // Covers three cases at once: the scan found nothing (kind is still AllKinds), the caller
        // named a kind outside the table, and the caller named one it has none of.
        if (kind < 0 || kind >= ObjectIdByKind.Length || Count(container, kind) == 0) {
            return NoQuarrel;
        }

        if (spend) {
            InventoryConsume.TryConsumeOne(container, ObjectIdByKind[kind], lookup);
        }
        return kind;
    }
}
