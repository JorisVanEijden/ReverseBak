namespace GameData.Resources.Dialog;

/// <summary>
/// Runtime state that the original game's <c>GetDialogTypeData</c> dispatcher
/// (at 0x4856c) consults before falling back to the source byte. These three
/// flags are global variables in the DOS binary — the Unity port must mirror
/// them so the same DDX entry renders in the same chrome under the same game
/// state.
/// </summary>
/// <param name="InGameContextActive">
/// Mirrors <c>dialog_word_3AC96</c>. Set when a dialog fires from the
/// in-world exploration loop (descriptions, actor speech, etc.). Forces the
/// effective style id to 5 *before* the actor and source-byte overrides apply
/// — those still run, so a non-zero <c>actorNr</c> or non-zero source byte
/// will still take precedence.
/// </param>
/// <param name="FullScreenFlag1Active">
/// Mirrors <c>bool_word_dseg_C08</c>. One of two flags that force the
/// effective style id to 6 (full-screen panel).
/// </param>
/// <param name="FullScreenFlag2Active">
/// Mirrors <c>byte_dseg_FBC</c>. The other full-screen flag — checked only if
/// <see cref="FullScreenFlag1Active"/> is false. Either flag is sufficient.
/// </param>
public record struct DialogContext(
    bool InGameContextActive,
    bool FullScreenFlag1Active,
    bool FullScreenFlag2Active
) {
    public static DialogContext None { get; } = new(false, false, false);
}
