namespace GameData.Resources.Dialog;

using GameData.Resources.Layout;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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
///
/// <para><b>A settable-property class, deliberately.</b> Every other resource on the
/// mod-override path (<c>CreditsData</c>, <c>ChapterCatalog</c>, <c>UserInterface</c>,
/// <see cref="LayoutHint"/>, …) is a plain class with <c>{ get; set; }</c> and defaults, and
/// this one has to match: the override path deserializes a whole document with Newtonsoft, so a
/// type whose only way in is a positional constructor depends on parameter-name matching rather
/// than on property names. The property-by-property shape also means an override document may
/// name only the fields it wants to change — unnamed fields simply land on this type's own
/// defaults instead of failing to match a constructor.</para>
///
/// <para><b>The instance a <see cref="DialogStyleTable"/> lookup returns is the shared table
/// row</b>, not the defensive copy the previous record struct handed out. Treat a looked-up
/// style as read-only; mutating one would change the style every later dialog of that row
/// renders with.</para>
/// </summary>
public class DialogStyle {
    /// <summary>Chrome stripe fill pen (<c>field_1</c>).</summary>
    public byte FillPenColor { get; set; }

    /// <summary>Chrome border pen (<c>field_4</c>).</summary>
    public byte BorderPenColor { get; set; }

    /// <summary>Chrome 3D bevel pen (<c>field_5</c>) — the panel bevel, not the text shadow.</summary>
    public byte ShadowPenColor { get; set; }

    /// <summary>Main body-text pen (<c>field_2</c>).</summary>
    public byte BodyTextPenColor { get; set; }

    /// <summary>Body-text drop-shadow source (<c>field_3</c>); the pen drawn is this minus one.</summary>
    public byte TextShadowPenSource { get; set; }

    /// <summary>
    /// Where the dialog panel sits, as layout data rather than a fixed rectangle. The table's own
    /// rows state it as design-frame px in the canonical 1600×1200 space (VGA 320×200 scaled
    /// ×5 horizontal / ×6 vertical, converted by the extractor at build time) so the shipped
    /// dialogs land exactly where the original put them; an override author can restate it as
    /// percentages, or anchor it, and the panel reflows.
    ///
    /// <para>The ×5/×6 pair is what preserves the 4:3 "non-square pixel" correction the original
    /// relied on: the ×6 vertical factor keeps the implicit ×1.2 vertical stretch relative to the
    /// ×5 horizontal one. Downstream consumers never see VGA pixels.</para>
    ///
    /// <para><b>A per-entry resize discards this hint entirely.</b> When a DDX entry carries a
    /// <c>ResizeDialog</c> action, <c>dialog_getDialogArea</c> (0x485bc) uses the entry's rect in
    /// place of the style's — it does not merge the two. The port keeps that: the resize's own
    /// <see cref="LayoutHint"/> (see <c>ResizeDialogAction.ToLayoutHint</c>) replaces this one
    /// wholesale, so an override author who anchors, say, row 2's area gets that anchor thrown
    /// away for any entry carrying a resize. That is faithful, not a defect — the original
    /// replaced too — and it is why the resize speaks in <see cref="LayoutHint"/> as well: a
    /// total replacement in one vocabulary can never leave a px inset being measured from a
    /// percentage-valued anchor.</para>
    ///
    /// <para>Source: <c>dialogAction_ResizeDialog</c> at offset 12 of <c>dialogTypeData</c>
    /// (per style row), plus the per-entry <c>ResizeDialog</c> overrides resolved by
    /// <c>dialog_getDialogArea</c> at 0x485bc.</para>
    /// </summary>
    public LayoutHint DefaultArea { get; set; } = new();

    /// <summary>
    /// Left text inset as a percentage (0..100) of the panel width. The original
    /// <c>RenderDialogText</c> (0x48d7b) shrinks the panel rect into a text rect
    /// before laying out / wrapping the body text: <c>X += field_9</c> and
    /// <c>Width -= field_9 + field_A</c> at 0x49043‑0x4905f. <c>field_9</c> is the
    /// left inset in VGA pixels; this field stores it normalised against the row's
    /// shipped <c>DefaultArea</c> width. Without it, wrapped text runs flush to the
    /// panel border.
    ///
    /// <para>The normalisation happened at authoring time against the row's <b>shipped px
    /// width</b> — the numbers in the table's own comments. It stays a correct percentage of
    /// whatever width <see cref="DefaultArea"/> ends up resolving to (that is the point of
    /// storing a percentage), but once an override restates the area in percentages the stated
    /// derivation is no longer checkable against the model: there is no px width left in the data
    /// to divide by. The comments on each <see cref="DialogStyleTable"/> row record the divisor
    /// that was used.</para>
    /// </summary>
    public float TextPadLeftPct { get; set; }

    /// <summary>
    /// Right text inset as a percentage (0..100) of the panel width — the original's
    /// <c>field_A</c> (0x4905f), normalised against the row's shipped <c>DefaultArea</c> width
    /// exactly as <see cref="TextPadLeftPct"/> was (see its remarks on the derivation). Bounds
    /// the right edge of the wrap region.
    /// </summary>
    public float TextPadRightPct { get; set; }

    // The five properties below are DERIVED — read-only convenience over the pen fields above.
    // They are ignored by both serializers on purpose: DIALSTYL.json is not just an extractor
    // output, it is the document a mod author copies and edits, and a field that looks settable
    // but silently does nothing (no setter to deserialize into) is a trap. Change the pen; the
    // derived answer follows.
    //
    // Both attributes are needed. [JsonIgnore] here is System.Text.Json's — the extractor's
    // serializer, which is what keeps these off the emitted DIALSTYL.json. Newtonsoft (the
    // override-merge path's serializer, Unity-side) does not recognise that attribute at all, so
    // without [IgnoreDataMember] too (BCL, no new package needed — GameData has no Newtonsoft
    // reference and shouldn't gain one just for this) the merge baseline JObject would still carry
    // these keys, and an author writing e.g. "HasBorder": false would hit scalar-onto-scalar in
    // the diagnostics walk (silently "matched", no warning) while ToObject discards it for want of
    // a setter — the quietest possible authoring failure.
    /// <summary>
    /// True → renderer overdraws the panel with a stripe-textured fill (via
    /// <c>vga_sub_14DD7</c>). False → renderer blits the saved background
    /// from VGA buffer C into buffer 1, preserving what was underneath.
    /// </summary>
    [JsonIgnore]
    [IgnoreDataMember]
    public bool UsesTexturedFill => FillPenColor != 0;

    /// <summary>True → draw a filled border in <see cref="BorderPenColor"/>.</summary>
    [JsonIgnore]
    [IgnoreDataMember]
    public bool HasBorder => BorderPenColor != 0;

    /// <summary>
    /// True → draw the chrome's 4-line 3D bevel (highlight on top+right in
    /// <see cref="ShadowPenColor"/>, black on left+bottom) AND apply a 1-pixel
    /// inset to the fill/border. Suppressed when the area's <c>x == 0x0D</c>.
    /// This is panel chrome — it does NOT control the text drop-shadow (see
    /// <see cref="HasTextShadow"/>).
    /// </summary>
    [JsonIgnore]
    [IgnoreDataMember]
    public bool HasDropShadow => ShadowPenColor != 0;

    /// <summary>
    /// True → <c>RenderDialogText</c> paints a 1-pixel drop-shadow behind the
    /// body text (a back pass offset by (+1,+1) before the main pen draws over
    /// it). Driven by <c>dialogTypeData.field_3</c>: the engine runs the shadow
    /// pass only when <c>field_3 != 0</c> (0x48d7b @ 0x490bf).
    /// </summary>
    [JsonIgnore]
    [IgnoreDataMember]
    public bool HasTextShadow => TextShadowPenSource != 0;

    /// <summary>
    /// Palette pen for the body-text drop-shadow — the engine's
    /// <c>field_3 - 1</c> (0x490c8). Only meaningful when
    /// <see cref="HasTextShadow"/> is true.
    /// </summary>
    [JsonIgnore]
    [IgnoreDataMember]
    public byte TextShadowPenColor => (byte)(TextShadowPenSource - 1);
}
