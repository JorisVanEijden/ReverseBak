namespace GameData.Resources.Dialog.Actions;

using GameData.Resources.Layout;

/// <summary>
/// Per-entry override of the dialog panel rectangle, expressed in canonical
/// 1600×1200 <see cref="LayoutLength"/> insets. The extractor's own payload is converted from the
/// original VGA (320×200) <c>ushort</c>s in <c>ResizeDialogActionBuilder</c> via
/// <c>CanonicalSpace.Apply(Dialog)</c>, which scales only the <c>Px</c> case; downstream consumers
/// see only canonical-space coordinates.
///
/// <para><b>The extractor only ever emits px.</b> The binary holds raw VGA <c>ushort</c>s and
/// there is no percentage anywhere in the original data — none is recoverable from a DDX file.
/// A percent-valued resize can only ever come from a hand-authored override, exercising the
/// same <see cref="LayoutLength"/> vocabulary <c>DialogStyle.DefaultArea</c> already speaks.</para>
///
/// <para><b>Decision revised 2026-08-05:</b> this type previously stored the four insets as plain
/// <c>int</c>s, and both this comment and
/// <c>ResizeDialogActionTests.SerializedShape_IsStillTheFourInts_WithNoLayoutHintField</c> declared
/// that shape permanent. That call is reversed: <c>DIALSTYL.json</c>'s style rows already
/// serialize <see cref="LayoutLength"/> as <c>"40px"</c>, so keeping this type on bare ints made
/// the two representations of the same "design-frame rectangle" concept diverge instead of match.
/// The four fields are now <see cref="LayoutLength"/>, and <c>generated/DDX/*.json</c> changes
/// from <c>"Left": 300</c> to <c>"Left": "300px"</c> for every emitted entry — a deliberate,
/// one-time shape change, not drift.</para>
/// </summary>
public class ResizeDialogAction : DialogActionBase {
    public LayoutLength Left { get; set; }
    public LayoutLength Top { get; set; }
    public LayoutLength Width { get; set; }
    public LayoutLength Height { get; set; }

    /// <summary>
    /// This resize as a complete <see cref="LayoutHint"/> — the same vocabulary
    /// <c>DialogStyle.DefaultArea</c> speaks. Now a plain wrap: the four fields already carry
    /// their own units, so this no longer constructs <see cref="LayoutLength.Px(float)"/> values
    /// itself, it just hands them to a fresh <see cref="LayoutHint"/> (every other hint field
    /// keeps its faithful default: absolute, top-left anchored, no far-edge opinion).
    ///
    /// <para><b>Why a hint and not just the four lengths:</b> <c>dialog_getDialogArea</c> (0x485bc)
    /// uses an entry's resize rect <i>in place of</i> the style's area — it never merges the two.
    /// Handing back a whole hint keeps that replacement total: there is no component-by-component
    /// mixing, and no case where one of these insets ends up being measured from an anchor the
    /// style declared differently. The consequence, deliberate and faithful, is that an override
    /// author who anchors a style's area loses that anchor for every DDX entry carrying a resize
    /// — exactly as the original discarded the style's rect there.</para>
    ///
    /// <para>Every call returns a fresh hint, so a caller may tweak the result without the next
    /// dialog that reads this action seeing the change.</para>
    /// </summary>
    public LayoutHint ToLayoutHint() => new() {
        Left = Left,
        Top = Top,
        Width = Width,
        Height = Height,
    };
}
