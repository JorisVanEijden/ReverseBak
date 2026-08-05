namespace GameData.Resources.Inventory;

using GameData.Resources.World;

/// <summary>
/// Resolves the INVMISC.BMX sprite index for the detail-window "container image" shown in the
/// loot/inventory screen, faithful to the world-item-subtype switch in the original's container-image
/// selector <c>sub_ovr158_59E</c> (@0x565ee). The image is keyed off the looted world item's
/// <see cref="WorldEntityType"/> byte (the original reads <c>worldItemIndexToPtr(container[8])[1]</c>
/// then <c>switch (subtype)</c>), so a chest (<see cref="WorldEntityType.Container"/>) shows a chest
/// and a corpse (<see cref="WorldEntityType.Corpse"/>) shows the dead-body sprite.
/// <para>
/// The original additionally has container-type special cases resolved <b>before</b> this switch —
/// the shared keys inventory (containerType 8) and the no-selection member inventory → 11,
/// npc/monster inventory (containerType 7) → 4, shop container → 7. The keys and member cases are
/// modelled (see the two constants below, applied by the inventory screen); the combat and shop
/// cases belong to the not-yet-built entry points. The world-clicked loot path (chest/corpse)
/// always falls through to this world-item-type switch.
/// </para>
/// </summary>
public static class ContainerImage {
    /// <summary>INVMISC.BMX#4 — the dead-body sprite (a corpse).</summary>
    public const int DeadBodyIndex = 4;

    /// <summary>INVMISC.BMX#11 — a ring of keys. Two cases return it
    /// (<c>invui_actor_inventory_kind</c> @0x565EE, INVENTOR.C:269): the member inventory while
    /// NOTHING is selected (the selector returns 0xB only when its caller passes <c>flag == 0</c>,
    /// computed as <c>highlight_slot != -1</c>), and — unconditionally, selection or not — the
    /// party's shared keys inventory itself (<see cref="Data.SaveGameContainerType.SharedKeys"/>,
    /// the DOS <c>RES_PICKLOCK_BUFFER</c> residence).</summary>
    public const int MemberInventoryIndex = 11;

    /// <summary>INVMISC.BMX#0 — the member-inventory image shown while an item IS selected: a bag.
    /// With <c>flag != 0</c> the selector falls past the 0xB branch and, for a party slot with no
    /// second inventory actor, returns 0. So the detail window swaps keys → bag on selection and
    /// back again when the selection clears.</summary>
    public const int MemberInventorySelectedIndex = 0;

    /// <summary>
    /// The INVMISC.BMX index for a world item of the given type, per <c>sub_ovr158_59E</c>'s
    /// 30-case jump table (@0x566bd). Unmapped/terrain types fall through to the original's default 0.
    /// </summary>
    public static int IndexForWorldEntityType(WorldEntityType type) {
        return (byte)type switch {
            6  => 1,   // Container (chest)
            10 => 10,  // Building
            12 => 3,   // Grave
            16 => 4,   // Corpse — the dead-body sprite
            17 => 2,   // Dirt
            24 => 8,   // Crystals
            26 or 27 or 28 => 6, // Bush group
            30 => 9,   // TreeStump
            35 => 5,   // DeadAnimal
            _  => 0,   // default (terrain / unmapped)
        };
    }
}
