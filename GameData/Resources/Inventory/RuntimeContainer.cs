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

    public static RuntimeContainer FromSnapshot(SaveGameContainerData snap) {
        var rc = new RuntimeContainer {
            Capacity = snap.Capacity,
            ContainerType = snap.ContainerType,
            OwnerActorNumber = snap.Location.ActorNumber,
        };
        foreach (SaveGameInventoryItemData it in snap.Items) {
            // Only the first NumberOfItems are live; the rest are trailing capacity slots.
            if (rc.Items.Count >= snap.NumberOfItems) { break; }
            rc.Items.Add(new RuntimeItem(it.ObjectId, it.Variable, it.ItemFlags));
        }
        return rc;
    }
}
