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
/// Screen rectangle for a DialogStyle, expressed as percentages (0..100) of
/// the game viewport. The extractor converts from the original 320×200 VGA
/// coordinates at build time (<c>X / 320 * 100</c>, <c>Y / 200 * 100</c>);
/// downstream consumers never see VGA pixels. The 4:3 aspect-ratio
/// "non-square pixel" correction the original game relied on falls out
/// automatically: dividing Y by 200 (the VGA vertical resolution) and
/// rendering into a 4:3 viewport is equivalent to the implicit ×1.2
/// vertical stretch.
///
/// Source: <c>dialogAction_ResizeDialog</c> at offset 12 of
/// <c>dialogTypeData</c> (per style row), plus per-entry <c>ResizeDialog</c>
/// overrides resolved by <c>dialog_getDialogArea</c> at 0x485bc.
/// </summary>
public record struct DialogArea(float LeftPct, float TopPct, float WidthPct, float HeightPct);
