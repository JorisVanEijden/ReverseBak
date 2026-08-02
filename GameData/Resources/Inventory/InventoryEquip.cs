namespace GameData.Resources.Inventory;

using GameData.Resources.Data;
using GameData.Resources.Object;

/// <summary>
/// The equip rules for the four wearable categories (Sword, Crossbow, Staff, Armor), ported
/// from KRONDOR.EXE via the canassa reconstruction. Spec:
/// <c>docs/specs/inventory-item-handling.md</c> §2-§3. The load-bearing invariant is
/// "equip-first": a party member can never hold a weapon/armor in the grid while the matching
/// paperdoll slot is empty, because the first one to arrive auto-equips
/// (<see cref="CanAutoEquip"/>) and an equipped melee weapon/staff can only be swapped, never
/// removed (<see cref="InventoryTransfer.Move"/>).
/// </summary>
public static class InventoryEquip {
    private const ushort EquippedFlag = (ushort)ItemFlags.Equipped;

    /// <summary>
    /// <c>CanEquip</c> @0x55fe2 (CMBINV.C:1084): a caster may equip Staff and Armor; a
    /// non-caster Sword, Crossbow and Armor. Nothing else about the member is consulted.
    /// </summary>
    public static bool CanEquipCategory(ObjectType type, bool isCaster) =>
        isCaster
            ? type == ObjectType.Staff || type == ObjectType.Armor
            : type == ObjectType.Sword || type == ObjectType.Crossbow || type == ObjectType.Armor;

    /// <summary>
    /// <c>findEquippedItemOfCategory</c> @0x5518d: index of the equipped item of the given
    /// category, or -1 when that paperdoll slot is empty.
    /// </summary>
    public static int FindEquippedIndex(RuntimeContainer container, ObjectType category,
        ObjectInfoSet objects) {
        for (int i = 0; i < container.Items.Count; i++) {
            RuntimeItem item = container.Items[i];
            if ((item.ItemFlags & EquippedFlag) != 0
                && objects?.GetById(item.ObjectId)?.ObjectType == category) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// <c>canAutoEquipOnPickup</c> @0x55414: an arriving item auto-equips when the destination
    /// is a party member's inventory, the member can equip its category, and nothing of that
    /// category is currently equipped — the equip-first rule.
    /// </summary>
    public static bool CanAutoEquip(RuntimeContainer member, RuntimeItem item, bool isCaster,
        ObjectInfoSet objects) {
        if (member.ContainerType != SaveGameContainerType.Inventory) {
            return false;
        }
        ObjectInfo obj = objects?.GetById(item.ObjectId);
        return obj != null
            && CanEquipCategory(obj.ObjectType, isCaster)
            && FindEquippedIndex(member, obj.ObjectType, objects) < 0;
    }

    /// <summary>
    /// The equip half of <c>Use_Item</c> @0x58cbd (ITEMUSE.C:159-166): clear Equipped on every
    /// item of the same category in the container, then set it on this one. Idempotent for an
    /// already-equipped item, and silent in the original (outcome -2, no "used" text).
    /// </summary>
    public static void Equip(RuntimeContainer container, int index, ObjectInfoSet objects) {
        if (index < 0 || index >= container.Items.Count) {
            return;
        }
        RuntimeItem item = container.Items[index];
        ObjectInfo obj = objects?.GetById(item.ObjectId);
        if (obj == null) {
            return;
        }
        foreach (RuntimeItem other in container.Items) {
            if (objects.GetById(other.ObjectId)?.ObjectType == obj.ObjectType) {
                other.ItemFlags = (ushort)(other.ItemFlags & ~EquippedFlag);
            }
        }
        item.ItemFlags = (ushort)(item.ItemFlags | EquippedFlag);
        container.Dirty = true;
    }
}
