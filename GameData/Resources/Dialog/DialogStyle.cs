namespace GameData.Resources.Dialog;

/// <summary>
/// Per-style chrome configuration for a DDX dialog. One row of the original
/// game's `dialogTypeData` table at 0x3a831, layout from `dialog?_sub_48632`
/// at 0x48632 (the only fields the chrome renderer actually reads are
/// FillPenColor, BorderPenColor, and ShadowPenColor; the other source-table
/// bytes are unused by the renderer pipeline).
/// </summary>
public record struct DialogStyle(
    byte FillPenColor,
    byte BorderPenColor,
    byte ShadowPenColor,
    byte BodyTextPenColor,
    DialogArea DefaultArea
) {
    /// <summary>
    /// True → renderer overdraws the panel with a stripe-textured fill (via
    /// <c>vga_sub_14DD7</c>). False → renderer blits the saved background
    /// from VGA buffer C into buffer 1, preserving what was underneath.
    /// </summary>
    public bool UsesTexturedFill => FillPenColor != 0;

    /// <summary>True → draw a filled border in <see cref="BorderPenColor"/>.</summary>
    public bool HasBorder => BorderPenColor != 0;

    /// <summary>
    /// True → draw a 1-pixel drop-shadow on the right and bottom edges in
    /// <see cref="ShadowPenColor"/>, AND apply a 1-pixel inset to the
    /// fill/border. Suppressed when the area's <c>x == 0x0D</c>.
    /// </summary>
    public bool HasDropShadow => ShadowPenColor != 0;
}

/// <summary>
/// Default screen rectangle for a DialogStyle, in original 320×200 VGA
/// coordinates. Stored as the union member <c>dialogAction_ResizeDialog</c>
/// (offset 12 of <c>dialogTypeData</c>); a per-entry <c>ResizeDialog</c>
/// action overrides this at runtime in <c>dialog_getDialogArea</c> at 0x485bc.
/// </summary>
public record struct DialogArea(ushort X, ushort Y, ushort Width, ushort Height);
