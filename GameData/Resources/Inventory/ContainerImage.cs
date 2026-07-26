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
/// cheat chest (containerType 8) and default member inventory → 11, npc/monster inventory
/// (containerType 7) → 4, shop container → 7. Those belong to the not-yet-built general-inventory /
/// shop entry points; the world-clicked loot path (chest/corpse) always falls through to this
/// world-item-type switch, so only that mapping is modelled here.
/// </para>
/// </summary>
public static class ContainerImage {
    /// <summary>INVMISC.BMX#4 — the dead-body sprite (a corpse).</summary>
    public const int DeadBodyIndex = 4;

    /// <summary>INVMISC.BMX#11 — the default member-inventory image, shown in the detail window when
    /// the screen is opened on a party member with no loot container (the containerType special case
    /// resolved before the world-item-type switch).</summary>
    public const int MemberInventoryIndex = 11;

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
