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
    public bool Dirty;

    /// <summary>
    /// This container carries a shop sub-record — the DOS
    /// <c>container_GetOffsetToData(container, dataType_Shop) != NULL</c> test that several screens
    /// branch on (e.g. <c>sub_ovr157_4E3</c> @0x549BA suppresses the inventory's More Info menu for
    /// a shop). Only the presence of the record is modelled; its contents are task-39.
    /// </summary>
    public bool IsShop;

    public static RuntimeContainer FromSnapshot(SaveGameContainerData snap) {
        var rc = new RuntimeContainer {
            Capacity = snap.Capacity,
            ContainerType = snap.ContainerType,
            OwnerActorNumber = snap.Location.ActorNumber,
            IsShop = snap.ShopData != null,
        };
        foreach (SaveGameInventoryItemData it in snap.Items) {
            // Only the first NumberOfItems are live; the rest are trailing capacity slots.
            if (rc.Items.Count >= snap.NumberOfItems) { break; }
            rc.Items.Add(new RuntimeItem(it.ObjectId, it.Variable, it.ItemFlags));
        }
        return rc;
    }
}
