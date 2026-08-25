namespace GameData.Resources.Inventory;

using System.Collections.Generic;

/// <summary>
/// Read-only questions about what a container holds — the original's
/// <c>itemtbl_inv_count_by_kind</c>, which spell components, ration counts, dialog conditions and
/// the shop stock check all share.
/// </summary>
public static class InventoryQuery {
    /// <summary>
    /// How many of an object a container holds.
    ///
    /// <para><b>This counts charges, not items.</b> An entry with a non-zero
    /// <see cref="RuntimeItem.Variable"/> contributes that value; an entry whose Variable is zero
    /// counts as ONE. So a quiver of 20 arrows answers 20.</para>
    ///
    /// <para><b>*** CORRECTED 2026-08-25. *** This doc used to end "a stack whose charges have run
    /// out answers 0 and reads as absent", which is exactly backwards</b> — the original increments
    /// by one on <c>condition == 0</c> (ITEMTBL.C:99-103) and so does the code below. A spent stack
    /// reads as PRESENT. The wrong sentence sat directly above the right code and cost a test
    /// written against it; it is also why <see cref="Combat.QuarrelInventory.Count"/> exists
    /// separately, since ammunition genuinely must answer 0 for an empty quiver.</para>
    ///
    /// <para>The original's parameter is named <c>kind</c>, which invites reading it as an item
    /// category; the body matches it against <c>item_id</c>, so it is an object id.</para>
    /// </summary>
    public static int CountByKind(RuntimeContainer container, int objectId) {
        if (container == null) {
            return 0;
        }
        var total = 0;
        foreach (RuntimeItem item in container.Items) {
            if (item.ObjectId != objectId) {
                continue;
            }
            total += item.Variable != 0 ? item.Variable : 1;
        }
        return total;
    }

    // ---------------------------------------------------------------- across the whole party

    /// <summary>
    /// Whether ANY of the given packs holds an object — the form the dialog item gates ask in.
    /// </summary>
    /// <remarks>
    /// <b>"Carries" is <see cref="CountByKind"/> &gt; 0, charges and all</b>, so a stack whose
    /// charges have run out reads as absent here too. Sharing that rule is the point: a gate that
    /// counted entries instead would offer a topic about an item the party can no longer use.
    /// </remarks>
    /// <remarks>
    /// <b>The original's party-wide form returns more than a count.</b>
    /// <c>itemtbl_partySize_by_kind</c> (ITEMTBL.C:109) also writes <c>nEvtArgActor0</c> — WHICH
    /// member has one — for the dialog to name them. Nothing consumes that yet, so only the
    /// boolean is modelled; a caller that needs the name should extend this rather than re-walk
    /// the packs.
    /// </remarks>
    public static bool AnyHolds(IEnumerable<RuntimeContainer> packs, int objectId) {
        if (packs == null) {
            return false;
        }
        foreach (RuntimeContainer pack in packs) {
            if (CountByKind(pack, objectId) > 0) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Condition at or above which an item does not need repairing.</summary>
    /// <remarks>Strictly below this counts — <c>condition &lt; 100</c>, so a pristine 100 does not.</remarks>
    public const int PristineCondition = 100;

    /// <summary>
    /// How many pieces of armour across the party need repair — <c>evtcond_pty_inv_repair_cnt</c>
    /// (canassa DIALOG/EVTCOND.C:21).
    /// </summary>
    /// <param name="packs">Every active member's pack.</param>
    /// <param name="objects">Item type records, for the category.</param>
    /// <remarks>
    /// <b>EQUIPPED OR NOT.</b> The routine tests only the category and the condition — there is no
    /// equipped check, unlike the combat wear routine next door — so a spare breastplate at the
    /// bottom of a pack counts just as much as the one being worn. A port that filtered to equipped
    /// gear would hide the topic from a party carrying a sack of dented armour.
    ///
    /// <para>The same walk answers a second, unrelated question for a different caller (object 48
    /// at condition 70 or better), which is why the original returns two counts. Only the repair
    /// count is modelled here; the other has no consumer yet.</para>
    ///
    /// <para><b>Recorded, not modelled:</b> with its third argument set the routine also repairs —
    /// condition to 100 and the <see cref="ItemFlags.Repairable"/> bit cleared — and it multiplies
    /// the quoted gold cost by this count, so <b>the price is per damaged piece</b>. That belongs
    /// with whoever builds the repair service.</para>
    /// </remarks>
    public static int CountNeedingRepair(IEnumerable<RuntimeContainer> packs,
        Object.ObjectInfoSet objects) {
        if (packs == null || objects == null) {
            return 0;
        }
        var total = 0;
        foreach (RuntimeContainer pack in packs) {
            if (pack == null) {
                continue;
            }
            foreach (RuntimeItem item in pack.Items) {
                if (item == null) {
                    continue;
                }
                Object.ObjectInfo info = objects.GetById(item.ObjectId);
                if (info != null && info.ObjectType == ObjectType.Armor
                    && item.Variable < PristineCondition) {
                    total++;
                }
            }
        }
        return total;
    }
}
