namespace GameData.Resources.Menu;

/// <summary>
/// Which button a menu or world click was made with —
/// <c>screen_input_poll_confirm_cancel</c> (SCREEN.C:114), held in the drag state that
/// <c>menupage_state_0e7c</c> returns.
/// </summary>
/// <remarks>
/// <b>This is the "unestablished menu-page state" several handlers were blocked on, and it is
/// nothing more than the button.</b> It reads as a mode because the call sites test it against a
/// bare 1 or 2 far from where it is set, and because it is polled once per frame into a global
/// rather than passed along with the click.
/// </remarks>
public static class MenuClickButton {
    /// <summary>Nothing is held.</summary>
    public const int None = 0;

    /// <summary>The primary button — left mouse, or the keys that stand in for it.</summary>
    public const int Primary = 1;

    /// <summary>The secondary button — right mouse, or the key that stands in for it.</summary>
    public const int Secondary = 2;

    /// <summary>
    /// <b>The numeric keypad substitutes for the mouse.</b>
    /// </summary>
    /// <remarks>
    /// Keypad 5 and keypad 0 count as the primary button and keypad + as the secondary, polled in
    /// the same expression as the mouse rather than translated somewhere earlier. So a keyboard-only
    /// player has both buttons, and any port that reads only real mouse buttons quietly removes
    /// that.
    /// </remarks>
    public static readonly int[] PrimaryScanCodes = { 0x4c, 0x52 };

    /// <inheritdoc cref="PrimaryScanCodes"/>
    public static readonly int[] SecondaryScanCodes = { 0x4e };

    /// <summary>
    /// <b>Primary wins when both are held.</b>
    /// </summary>
    /// <remarks>
    /// The original's test is a single if/else-if with the primary first, so holding both answers
    /// primary rather than either "both" or the more recent.
    /// </remarks>
    public static int Resolve(bool primaryHeld, bool secondaryHeld) =>
        primaryHeld ? Primary
        : secondaryHeld ? Secondary
        : None;

    /// <summary>
    /// Whether a click counts as the acting one, as against describing.
    /// </summary>
    /// <remarks>
    /// <b>NOTHING HELD IS NOT THE SAME AS THE SECONDARY BUTTON.</b> Callers written as
    /// <c>state != 1</c> lump the two together — which is right for them, because a fixed-object
    /// click is only reached with a button down. A caller that can be reached with none held must
    /// not copy that shape.
    /// </remarks>
    public static bool IsActing(int state) => state == Primary;
}
