namespace GameData.Resources.Data;

/// <summary>
/// The travel HUD's bookmark — <c>mainmenu_save_bookmark</c> (SCREENS/MAINMENU.C), reached from the
/// world loop's action 0x30.
/// </summary>
/// <remarks>
/// <b>It is a quick-save into slot 00 of the CURRENT save directory, not a separate autosave file.</b>
/// So a bookmark belongs to whichever saved game the player is in, and there is exactly one per
/// directory — taking a bookmark overwrites the previous one without asking.
/// </remarks>
public static class BookmarkSave {
    /// <summary>The slot a bookmark always writes.</summary>
    public const int Slot = 0;

    /// <summary>The name stamped into the save header.</summary>
    /// <remarks>
    /// <b>Not what the load screen shows.</b> The header carries this, and the file picker forces
    /// the display name "Bookmark" over it for slot 0 regardless. Two different strings for one
    /// slot, and neither is derived from the other.
    /// </remarks>
    public const string HeaderName = "Copied Bookmark";

    /// <summary>
    /// <b>A bookmark needs an active save directory and refuses without one.</b>
    /// </summary>
    /// <remarks>
    /// The whole routine is wrapped in a check on the current slot directory; with none it plays
    /// <see cref="NoSlotDialog"/> and does nothing. So the button cannot be used until the player has
    /// saved at least once — there is nowhere to put the file before then.
    /// </remarks>
    public static bool CanSave(bool hasActiveSaveDirectory) => hasActiveSaveDirectory;

    /// <summary>Shown when there is no save directory to bookmark into.</summary>
    public const int NoSlotDialog = 0x8f;

    /// <summary>Shown once the bookmark is written.</summary>
    public const int SavedDialog = 0x90;

    /// <summary>Shown when the world loop refuses the action outright.</summary>
    /// <remarks>
    /// The same guard that refuses camping and the main menu from the world loop — a bookmark is
    /// refused for the same reasons and with its own line.
    /// </remarks>
    public const int RefusedDialog = 0xe6;

    /// <summary>
    /// <b>A failed write says NOTHING.</b>
    /// </summary>
    /// <remarks>
    /// On a write failure the original clears its save-valid flag and returns — no dialog, no retry.
    /// Only the success path speaks. Worth preserving deliberately rather than "improving" with an
    /// error box: a port that adds one is adding a message the game never shows, and the failure it
    /// reports is one the player cannot act on anyway.
    /// </remarks>
    public static bool ReportsWriteFailure => false;

    // ---- the optional confirmation ---------------------------------------------------------------

    /// <summary>Prompt shown when the verify preference is on.</summary>
    public const int VerifyPromptDialog = 0x14c;

    /// <summary>Shown when the player declines the prompt.</summary>
    public const int VerifyDeclinedDialog = 0x14d;

    /// <summary>Shown when the player accepts it.</summary>
    public const int VerifyAcceptedDialog = 0x14e;

    /// <summary>
    /// Whether a keypress accepts the verify prompt.
    /// </summary>
    /// <remarks>
    /// <b>Three scancodes accept and everything else declines</b> — Enter and two others, rather than
    /// the usual any-key. A LEFT mouse button is folded in as Enter and a RIGHT one as scancode 1,
    /// which is not in the set and therefore declines: so right-click is a cancel here even though
    /// the prompt does not say so.
    /// </remarks>
    public static bool VerifyAccepts(int scanCode) =>
        scanCode == 0x1c || scanCode == 0x4c || scanCode == 0x52;

    /// <summary>Scan code a left click is reported as.</summary>
    public const int LeftClickScanCode = 0x1c;

    /// <summary>Scan code a right click is reported as.</summary>
    public const int RightClickScanCode = 1;

    // ---- the map mark ----------------------------------------------------------------------------

    /// <summary>
    /// The compass icon stored with a bookmark, from the camera's heading.
    /// </summary>
    /// <remarks>
    /// <c>(yaw &gt;&gt; 13) &lt;&lt; 2</c> — the heading's top three bits select one of eight
    /// facings, and the result is multiplied by four because the icon is an index into a table of
    /// four-frame groups. Shifting by 13 alone gives a value that indexes the wrong table.
    /// </remarks>
    public static int CompassIconFor(int normalisedYaw) => (normalisedYaw >> 13) << 2;

    /// <summary>Value stored for both map coordinates when the chapter has no map entry.</summary>
    /// <remarks>
    /// <b>Minus one, and the icon goes to zero with it.</b> A chapter whose map lookup fails still
    /// writes a bookmark — it simply has no position to show on the map screen — so a port must not
    /// treat the failed lookup as a reason to refuse the save.
    /// </remarks>
    public const int NoMapPosition = -1;
}
