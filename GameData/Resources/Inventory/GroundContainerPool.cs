namespace GameData.Resources.Inventory;

using GameData.Resources.Data;
using GameData.Resources.Object;
using System.Collections.Generic;

/// <summary>
/// The ground-bag slot pool — faithful port of <c>actorspawn_enc_location</c> (ACTSPAWN.C:170),
/// the fallback half of the discard path in <c>cmbinv_actor_drop_item_at_pos</c> (CMBINV.C:1047).
///
/// <para>Dropping an item never allocates a container. Every travel zone ships a fixed set of
/// <see cref="SaveGameContainerType.Free"/> records — parked at zone 255 / (0,0) with capacity 20
/// and the <see cref="SaveGameContainerDataType.SelfSpawn"/> flag already set — and a discard
/// claims one, stamping it into a <see cref="SaveGameContainerType.Bag"/> at the party's feet.
/// The record count and every record's serialized size are therefore invariant, which is what
/// keeps the save write-back an exact in-place patch.</para>
///
/// <para>When a zone's pool is exhausted the engine recycles instead of refusing: it takes the
/// least-recently-touched self-spawning record and discards its contents. Bags holding a
/// <see cref="SaveGameContainerDataType.HoldsProtectedItem"/> item sort last, so quest items are
/// the last thing destroyed.</para>
/// </summary>
public static class GroundContainerPool {
    /// <summary>TBL entity 166 ("bag") — the shape a claimed slot renders as in the world.
    /// The engine writes 0xA6 outside combat and 0x89 inside it (ACTSPAWN.C:242); combat is out
    /// of scope, so only the travel value is used.</summary>
    public const short BagWorldItemId = 0xA6;

    /// <summary>The chapter band a claim stamps: <c>chap_band = '\n'</c> = 0x0A, i.e. min 0 /
    /// max 10 — visible in every chapter. The pool records already ship with it.</summary>
    public const int BagMinChapter = 0;
    public const int BagMaxChapter = 10;

    /// <summary>The object-record flag that makes a container count as holding a protected item
    /// (CMBINV.C:819, <c>itemtbl_record_ptr(slot)-&gt;wFlags &amp; 2</c>).</summary>
    private const int ProtectedItemObjectFlag = (int)ObjectFlags.NotEquipable;

    /// <summary>
    /// Pick the record a discard should claim in this zone: the first
    /// <see cref="SaveGameContainerType.Free"/> one, else the least-recently-touched
    /// <see cref="SaveGameContainerDataType.SelfSpawn"/> one to recycle. Returns null only when the
    /// zone has neither — the engine would then dereference a zeroed header, so refusing is the
    /// honest equivalent.
    /// </summary>
    /// <param name="recycled">True when the returned record is a live bag being reused, so the
    /// caller must clear its contents (and knows an existing pile was destroyed).</param>
    public static RuntimeContainer SelectSlot(IReadOnlyList<RuntimeContainer> zoneContainers,
        out bool recycled) {
        recycled = false;
        if (zoneContainers == null) {
            return null;
        }

        foreach (RuntimeContainer c in zoneContainers) {
            if (c.ContainerType == SaveGameContainerType.Free) {
                return c;
            }
        }

        // No free slot: the least-recently-touched self-spawning record wins. The engine compares
        // the last-touch times as UNSIGNED 32-bit after ORing 0x80000000 into a protected bag's,
        // which is what pushes those past every unprotected one. Ties keep the first record, since
        // the scan only replaces on a strict improvement.
        RuntimeContainer best = null;
        uint bestKey = uint.MaxValue;
        foreach (RuntimeContainer c in zoneContainers) {
            if ((c.DataTypes & SaveGameContainerDataType.SelfSpawn) == 0) {
                continue;
            }
            uint key = unchecked((uint)(c.Timestamp ?? 0));
            if ((c.DataTypes & SaveGameContainerDataType.HoldsProtectedItem) != 0) {
                key |= 0x80000000u;
            }
            if (key < bestKey) {
                bestKey = key;
                best = c;
            }
        }
        recycled = best != null;
        return best;
    }

    /// <summary>
    /// Stamp a selected slot into a ground bag at (<paramref name="x"/>, <paramref name="y"/>) in
    /// <paramref name="zone"/> — the assignment block at ACTSPAWN.C:237-254. A recycled bag's
    /// previous contents are dropped here, matching <c>result-&gt;itemCount = 0</c>.
    /// </summary>
    public static void Claim(RuntimeContainer slot, int zone, int x, int y, int gameTime) {
        if (slot == null) {
            return;
        }
        slot.Items.Clear();
        slot.Zone = zone;
        slot.X = x;
        slot.Y = y;
        slot.MinChapter = BagMinChapter;
        slot.MaxChapter = BagMaxChapter;
        slot.WorldItemId = BagWorldItemId;
        slot.ContainerType = SaveGameContainerType.Bag;
        slot.DataTypes |= SaveGameContainerDataType.SelfSpawn;
        slot.OwnerActorNumber = 0;
        Touch(slot, gameTime);
        slot.Dirty = true;
        slot.HeaderDirty = true;
    }

    /// <summary>
    /// Return an emptied self-spawning bag to the pool —
    /// <c>actorspawn_destroy_and_persist</c>'s <c>(flags &amp; 0x80) &amp;&amp; itemCount == 0</c>
    /// branch, which sets residence back to RES_FREE and drops the bag out of the visible pool.
    /// Returns true when the record was freed, so the caller can despawn its world entity.
    /// </summary>
    public static bool ReleaseIfEmpty(RuntimeContainer container) {
        if (container == null
            || container.ContainerType != SaveGameContainerType.Bag
            || (container.DataTypes & SaveGameContainerDataType.SelfSpawn) == 0
            || container.Items.Count != 0) {
            return false;
        }
        container.ContainerType = SaveGameContainerType.Free;
        container.Dirty = true;
        container.HeaderDirty = true;
        return true;
    }

    /// <summary>
    /// Recompute <see cref="SaveGameContainerDataType.HoldsProtectedItem"/> over the whole item
    /// array (CMBINV.C:810). Run after any change to a bag's contents; it clears the bit first, so
    /// removing the last protected item drops it again.
    /// </summary>
    public static void RecomputeHoldsProtectedItem(RuntimeContainer container, ObjectInfoSet objects) {
        if (container == null) {
            return;
        }
        SaveGameContainerDataType before = container.DataTypes;
        container.DataTypes &= ~SaveGameContainerDataType.HoldsProtectedItem;
        foreach (RuntimeItem item in container.Items) {
            ObjectInfo rec = objects?.GetById(item.ObjectId);
            if (rec != null && ((int)rec.Flags & ProtectedItemObjectFlag) != 0) {
                container.DataTypes |= SaveGameContainerDataType.HoldsProtectedItem;
                break;
            }
        }
        if (container.DataTypes != before) {
            container.HeaderDirty = true;
            container.Dirty = true;
        }
    }

    /// <summary>Stamp the last-touch time, which the recycler orders by
    /// (<c>actorspawn_persist_to_temp</c> writes it on every flush).</summary>
    public static void Touch(RuntimeContainer container, int gameTime) {
        if (container == null
            || (container.DataTypes & SaveGameContainerDataType.Timestamp) == 0) {
            return;
        }
        container.Timestamp = gameTime;
        container.HeaderDirty = true;
    }
}
