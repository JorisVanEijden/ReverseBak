namespace GameData.Resources.Dialog.Actions;

using GameData.Resources.Layout;

/// <summary>
/// Per-entry override of the dialog panel rectangle, expressed in canonical
/// 1600×1200 pixels. Converted from the original VGA (320×200) payload in
/// <c>ResizeDialogActionBuilder</c> via <c>CanonicalSpace.Apply(Dialog)</c>;
/// downstream consumers see only canonical-space coordinates.
///
/// <para>The four ints are the extractor's emitted, already-canonical form and are what
/// <c>generated/DDX/*.json</c> carries — they are the serialized shape and must stay that way.
/// <see cref="ToLayoutHint"/> is a derived view of them, not a stored field, so nothing is added
/// to the JSON.</para>
/// </summary>
public class ResizeDialogAction : DialogActionBase {
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// This resize as a complete <see cref="LayoutHint"/> of design-frame px insets — the same
    /// vocabulary <c>DialogStyle.DefaultArea</c> speaks.
    ///
    /// <para><b>Why a hint and not just the four ints:</b> <c>dialog_getDialogArea</c> (0x485bc)
    /// uses an entry's resize rect <i>in place of</i> the style's area — it never merges the two.
    /// Handing back a whole hint keeps that replacement total: there is no component-by-component
    /// mixing, and no case where one of these px insets ends up being measured from an anchor the
    /// style declared in percent. The consequence, deliberate and faithful, is that an override
    /// author who anchors a style's area loses that anchor for every DDX entry carrying a resize
    /// — exactly as the original discarded the style's rect there.</para>
    ///
    /// <para>Every call returns a fresh hint, so a caller may tweak the result without the next
    /// dialog that reads this action seeing the change.</para>
    /// </summary>
    public LayoutHint ToLayoutHint() => LayoutHint.PxRect(Left, Top, Width, Height);
}
