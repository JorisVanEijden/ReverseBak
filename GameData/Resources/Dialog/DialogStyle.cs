namespace GameData.Resources.Dialog;

/// <summary>
/// Per-style chrome + text configuration for a DDX dialog. One row of the
/// original game's 7-row × 20-byte <c>dialogTypeData</c> table at 0x3a831.
///
/// Field offsets verified against the raw table bytes and the two renderers
/// that consume them — <c>dialog_DrawChrome</c> (0x48632) for the panel chrome
/// and <c>RenderDialogText</c> (0x48d7b) for the text:
/// <list type="bullet">
///   <item><c>field_1</c> → <see cref="FillPenColor"/> — chrome stripe fill.</item>
///   <item><c>field_2</c> → <see cref="BodyTextPenColor"/> — main body-text pen.
///     Every caller passes <c>textColor = -1</c> to <c>RenderDialogText</c>, so
///     the body text always falls back to this field (0x490ff).</item>
///   <item><c>field_3</c> → <see cref="TextShadowPenSource"/> — body-text drop
///     shadow. The engine renders a shadow pass only when this is non-zero, in
///     pen <c>field_3 - 1</c> (0x490bf‑0x490ca).</item>
///   <item><c>field_4</c> → <see cref="BorderPenColor"/> — chrome border.</item>
///   <item><c>field_5</c> → <see cref="ShadowPenColor"/> — chrome 3D bevel.
///     This is the panel bevel, NOT the text shadow (a common confusion: the
///     text shadow lives in <c>field_3</c>).</item>
/// </list>
///
/// All pen values are palette indices. The active runtime palette resolves
/// them to RGB — the cutscene palette for cutscene dialogs, OPTIONS.PAL for
/// the in-game / menu path. Keeping pens (not baked RGB) on the model is
/// deliberate: the same style renders different colours under different scene
/// palettes, exactly as the original indexed renderer did.
/// </summary>
/// <param name="TextPadLeftPct">
/// Left text inset as a percentage (0..100) of the panel width. The original
/// <c>RenderDialogText</c> (0x48d7b) shrinks the panel rect into a text rect
/// before laying out / wrapping the body text: <c>X += field_9</c> and
/// <c>Width -= field_9 + field_A</c> at 0x49043‑0x4905f. <c>field_9</c> is the
/// left inset in VGA pixels; this field stores it normalised against the
/// row's <c>DefaultArea</c> width so consumers never see VGA pixels. Without
/// it, wrapped text runs flush to the panel border.
/// </param>
/// <param name="TextPadRightPct">
/// Right text inset as a percentage (0..100) of the panel width — the
/// original's <c>field_A</c> (0x4905f), normalised against the row's
/// <c>DefaultArea</c> width. Bounds the right edge of the wrap region.
/// </param>
public record struct DialogStyle(
    byte FillPenColor,
    byte BorderPenColor,
    byte ShadowPenColor,
    byte BodyTextPenColor,
    byte TextShadowPenSource,
    DialogArea DefaultArea,
    float TextPadLeftPct = 0f,
    float TextPadRightPct = 0f
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
    /// True → draw the chrome's 4-line 3D bevel (highlight on top+right in
    /// <see cref="ShadowPenColor"/>, black on left+bottom) AND apply a 1-pixel
    /// inset to the fill/border. Suppressed when the area's <c>x == 0x0D</c>.
    /// This is panel chrome — it does NOT control the text drop-shadow (see
    /// <see cref="HasTextShadow"/>).
    /// </summary>
    public bool HasDropShadow => ShadowPenColor != 0;

    /// <summary>
    /// True → <c>RenderDialogText</c> paints a 1-pixel drop-shadow behind the
    /// body text (a back pass offset by (+1,+1) before the main pen draws over
    /// it). Driven by <c>dialogTypeData.field_3</c>: the engine runs the shadow
    /// pass only when <c>field_3 != 0</c> (0x48d7b @ 0x490bf).
    /// </summary>
    public bool HasTextShadow => TextShadowPenSource != 0;

    /// <summary>
    /// Palette pen for the body-text drop-shadow — the engine's
    /// <c>field_3 - 1</c> (0x490c8). Only meaningful when
    /// <see cref="HasTextShadow"/> is true.
    /// </summary>
    public byte TextShadowPenColor => (byte)(TextShadowPenSource - 1);
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
