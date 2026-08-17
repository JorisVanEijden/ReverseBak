namespace GameData.Resources.Inventory;

using GameData.Resources.Data;

/// <summary>
/// Which background the inventory screen draws behind its grid — <c>UI_DrawInventory</c> @0x56880.
/// </summary>
/// <remarks>
/// <b>The container type alone does not decide this.</b> The original tests two things:
/// <code>
///   cmp containerType, 1     ; a party member's own pack
///   jz  narrow
///   cmp byte_dseg_12C4, 0    ; the shop/picklock mode flag
///   jz  wide
///   ; falls through to narrow
/// </code>
/// so the narrow, split background is drawn when the container is a member's pack <b>OR</b> the
/// mode flag is set, and the wide continuous one only when NEITHER holds.
///
/// <para>Reading it as "wide whenever this is not a member's pack" is right for every screen that
/// leaves the flag clear, and wrong for the one that does not — see <see cref="ShopMode"/>.</para>
/// </remarks>
public static class InventoryPanelMode {
    /// <summary>
    /// The flag the original calls <c>byte_dseg_12C4</c>: the screen is showing a SCRATCH container
    /// rather than a real one.
    /// </summary>
    /// <remarks>
    /// <b>Set by the picklock screen</b> (<c>sub_ovr166_DF</c> @0x5be4f), which builds a temporary
    /// container from the party's shared stock and runs the ordinary inventory screen over it. That
    /// container is typed <c>SharedKeys</c>, not <c>Inventory</c>, so the container-type test alone
    /// would give it the WIDE loot background — where the original gives it the narrow one.
    ///
    /// <para>Every other writer clears it. So today, with no picklock screen in the port, passing
    /// <c>false</c> reproduces the original exactly; the moment that screen lands it must pass
    /// <c>true</c>, or its panel will be drawn as a loot window.</para>
    /// </remarks>
    public enum ShopMode {
        /// <summary>The flag is clear — every screen the port currently has.</summary>
        Off,

        /// <summary>The flag is set — the picklock screen's scratch container.</summary>
        On,
    }

    /// <summary>
    /// Whether the wide, continuous background is drawn (as opposed to the split member layout).
    /// </summary>
    public static bool UsesWideBackground(SaveGameContainerType containerType, ShopMode mode) =>
        containerType != SaveGameContainerType.Inventory && mode == ShopMode.Off;
}
