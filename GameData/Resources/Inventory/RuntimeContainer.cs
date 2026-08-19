namespace GameData.Resources.Inventory;

using GameData.Resources.Data;
using System.Collections.Generic;

/// <summary>
/// Mutable runtime container (DOS `container`): the corpse/world container OR a character's
/// Inventory container, edited in place during looting (matching the engine's mutate + dirty flag).
/// </summary>
public sealed class RuntimeContainer {
    public List<RuntimeItem> Items { get; } = new List<RuntimeItem>();
    public int Capacity;
    public SaveGameContainerType ContainerType;
    public short OwnerActorNumber;

    /// <summary>
    /// The party has opened this container's lock this session.
    /// </summary>
    /// <remarks>
    /// <b>Runtime only — this does NOT survive a save.</b> Lock state lives on the immutable
    /// <c>SaveGameContainerData</c> snapshot, and the save writer re-emits only a container's ITEM
    /// bytes (see <c>ContainerGeometry</c>, which computes the lock subrecord's offset but never
    /// rewrites it). So a chest picked open reads as locked again after a reload.
    ///
    /// <para>Kept here rather than faked on the snapshot because the snapshot is deliberately
    /// immutable — it is what the writer diffs against — and because a runtime flag states the
    /// limitation instead of hiding it. Persisting it is writer work, not a flag rename.</para>
    /// </remarks>
    public bool Unlocked;

    private bool _dirty;

    /// <summary>
    /// The contents changed and the save writer must re-emit them — the engine's
    /// <c>needsFlush</c>. Raising it also stamps <see cref="Timestamp"/> when
    /// <see cref="TouchClock"/> is attached, because in this engine "dirty" is exactly the moment
    /// the container was last used.
    /// </summary>
    public bool Dirty {
        get => _dirty;
        set {
            _dirty = value;
            if (value && TouchClock != null) {
                GroundContainerPool.Touch(this, TouchClock());
            }
        }
    }

    /// <summary>
    /// Supplies the current game time for the last-touch stamp; attached by the session when it
    /// builds the runtime containers. Left null, no stamping happens at all — which is what pure
    /// fixtures want, and keeps a clockless caller from writing a zero over a real timestamp.
    ///
    /// <para>This exists because every mutation already funnels through <see cref="Dirty"/>: the
    /// alternative was threading a clock into a dozen <c>InventoryTransfer</c> / <c>InventoryUse</c>
    /// call sites that have no business knowing about time.</para>
    /// </summary>
    public System.Func<int> TouchClock;

    /// <summary>
    /// The record's placement, mutable because claiming a ground bag rewrites it in place —
    /// <c>actorspawn_enc_location</c> stamps zone/x/y/world-item-id/chapter-band over a
    /// <see cref="SaveGameContainerType.Free"/> slot rather than allocating a new record. For
    /// every other container these keep their authored values for the record's whole life.
    /// </summary>
    public int Zone;
    public int X;
    public int Y;
    public short WorldItemId;
    public int MinChapter;
    public int MaxChapter;

    /// <summary>The <c>Actor::flags</c> byte. The low six bits size the record and never change;
    /// <see cref="SaveGameContainerDataType.HoldsWeapon"/> is recomputed on every content change
    /// and <see cref="SaveGameContainerDataType.SelfSpawn"/> marks a record that frees itself when
    /// emptied.</summary>
    public SaveGameContainerDataType DataTypes;

    /// <summary>The container's own interaction dialog (the SUBREC_INTERACT_MSG record), which
    /// overrides the interaction profile's action dialog when non-zero. Authored data that never
    /// changes at runtime; carried here so a live container is a complete substitute for its save
    /// snapshot when deciding what a click on it says.</summary>
    public uint? DialogId;

    /// <summary>The SUBREC_LAST_TOUCH game time, present only when
    /// <see cref="SaveGameContainerDataType.Timestamp"/> is set. The ground-bag recycler picks the
    /// smallest one, so it must be stamped on every claim.</summary>
    public int? Timestamp;

    /// <summary>Set when a field outside the item array changed, so the save writer patches the
    /// 16-byte header (and the timestamp subrecord) as well as the items. Only a ground-bag
    /// claim or release raises it; looting alone leaves it false.</summary>
    public bool HeaderDirty;

    /// <summary>
    /// This container carries a shop sub-record — the DOS
    /// <c>container_GetOffsetToData(container, dataType_Shop) != NULL</c> test that several screens
    /// branch on (e.g. <c>sub_ovr157_4E3</c> @0x549BA suppresses the inventory's More Info menu for
    /// a shop). Only the presence of the record is modelled; its contents are task-39.
    /// </summary>
    public bool IsShop;

    /// <summary>
    /// The container's shop sub-record, live — prices, an inn's rate, a tavern's entertainment
    /// fund, a temple's tier.
    /// </summary>
    /// <remarks>
    /// <b>A copy, not the snapshot's own instance.</b> The parsed save is the immutable record of
    /// what was loaded; this is what gameplay changes, the same split the items and the actor stats
    /// already have. Null when the container carries no shop record at all.
    ///
    /// <para>It is mutable because some of it is <b>spent</b>: a tavern's entertainment fund is
    /// zeroed by the one performance it pays for, and a shop's restock chapter moves. Without a live
    /// copy those are lost at the door and the tavern pays again on the next visit.</para>
    /// </remarks>
    public SaveGameContainerShopData Shop;

    /// <summary>A working copy of a shop record, so spending from it cannot reach the snapshot.</summary>
    private static SaveGameContainerShopData Copy(SaveGameContainerShopData shop) =>
        shop == null
            ? null
            : new SaveGameContainerShopData(shop.ShopType, shop.MarkupPercentage,
                shop.MaxHagglingDiscount, shop.MarkDownPercentage, shop.ShopkeeperSkill,
                shop.TeleportParam, shop.BardingDifficulty, shop.BardingReward,
                shop.BaseBardingReward, shop.LastRestockChapter, shop.InnRestHours,
                shop.InnCostPerNight, shop.RepairCategories, shop.RepairCostMarkup,
                shop.ShopCategories);

    public static RuntimeContainer FromSnapshot(SaveGameContainerData snap) {
        var rc = new RuntimeContainer {
            Capacity = snap.Capacity,
            ContainerType = snap.ContainerType,
            OwnerActorNumber = snap.Location.ActorNumber,
            IsShop = snap.ShopData != null,
            Shop = Copy(snap.ShopData),
            Zone = snap.Location.Zone,
            X = snap.Location.X,
            Y = snap.Location.Y,
            WorldItemId = snap.Location.WorldItemId,
            MinChapter = snap.Location.MinChapter,
            MaxChapter = snap.Location.MaxChapter,
            DataTypes = snap.DataTypes,
            Timestamp = snap.Timestamp,
            DialogId = snap.DialogData?.DialogId,
        };
        foreach (SaveGameInventoryItemData it in snap.Items) {
            // Only the first NumberOfItems are live; the rest are trailing capacity slots.
            if (rc.Items.Count >= snap.NumberOfItems) { break; }
            rc.Items.Add(new RuntimeItem(it.ObjectId, it.Variable, it.ItemFlags));
        }
        return rc;
    }
}
