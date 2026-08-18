namespace GameData.Resources.Character;

/// <summary>
/// Marking a rating for study — the character sheet's per-skill emphasis, and what the mark on its
/// bar means. Read from <c>charscreen_info_loop</c> @0x58378 (ovr160).
/// </summary>
/// <remarks>
/// <b>The mark at the end of a bar is not decoration.</b> The end marker
/// <see cref="CharacterSheetRow.BarEndMarkerIcon"/> is drawn exactly when this flag is set
/// (<c>UI_show_attribute</c> reads the same global at 0x57e24), so the sheet is showing which
/// ratings the character is concentrating on. A renderer that drew the marker for its own reasons
/// — or never — would be telling the player the wrong thing about their own choices.
///
/// <para><b>Clicking a rating is what sets it.</b> The row's own click area toggles this; the help
/// line says so in as many words ("Left clicking on a skill can emphasize or de-emphasize how much
/// the character focuses on learning that skill"). It is the input side of
/// <c>StatEngine.Modify</c>'s <c>studyBonusPer52</c>, which is otherwise a parameter with nobody to
/// supply it.</para>
/// </remarks>
public static class SkillEmphasis {
    /// <summary>
    /// Base of the per-attribute emphasis flags.
    /// </summary>
    /// <remarks>
    /// A second array beside the "changed since you last looked" flags
    /// (<see cref="CharacterSheetRow.ChangedFlagBase"/>), with the same per-actor stride and the
    /// same indexing. Two arrays, two meanings, 120 apart — reading one for the other would make
    /// every improvement look like a study choice.
    /// </remarks>
    public const int FlagBase = 6230;

    /// <inheritdoc cref="CharacterSheetRow.AttributesPerActor"/>
    public const int AttributesPerActor = CharacterSheetRow.AttributesPerActor;

    /// <summary>The flag key for one actor's attribute.</summary>
    public static int FlagFor(int actorNumber, int attributeNumber) =>
        FlagBase + (actorNumber * AttributesPerActor) + attributeNumber;

    /// <summary>Whether a rating is marked for study.</summary>
    public static bool IsEmphasised(int flagValue) => flagValue != 0;

    /// <summary>
    /// What a click writes back.
    /// </summary>
    /// <remarks>
    /// <b>A plain boolean toggle</b> — <c>value = (old == 0) ? 1 : 0</c> at 0x5859c, so a flag left
    /// holding some other number by a save comes back as 1 rather than being incremented. There is
    /// no limit on how many ratings a character may emphasise at once; the original counts nothing.
    /// </remarks>
    public static int Toggled(int flagValue) => flagValue == 0 ? 1 : 0;

    /// <summary>
    /// Whether this rating can be marked at all.
    /// </summary>
    /// <param name="maximum">The actor's maximum for it.</param>
    /// <remarks>
    /// <b>Tested on the MAXIMUM, and the click is dropped when it is zero</b> (0x5857d) — the same
    /// "never had it" case that prints N/A instead of a percentage. So a rating the character does
    /// not possess cannot be studied, and the click does nothing rather than saying why.
    /// </remarks>
    public static bool CanEmphasise(int maximum) => maximum != 0;

    /// <summary>The line shown when a rating row is asked about rather than clicked.</summary>
    public const int HelpDialog = 323;

    /// <summary>Action id of the first rating row on the sheet.</summary>
    public const int FirstRowActionId = 128;

    /// <summary>The attribute a rating row stands for.</summary>
    /// <remarks>
    /// The rows cover the lower half only, so row 0 is attribute
    /// <see cref="CharacterSheetLayout.LowerHalfFirstAttribute"/> — which is why the original adds
    /// -124 to the action id where the help arm adds -128 (0x5856e against 0x58551): the toggle
    /// wants the ATTRIBUTE and the help wants the ROW.
    /// </remarks>
    public static int AttributeForRow(int rowIndex) =>
        rowIndex + CharacterSheetLayout.LowerHalfFirstAttribute;

    /// <summary>The row a rating-row action id stands for, or -1 for any other id.</summary>
    public static int RowForAction(int actionId) {
        int row = actionId - FirstRowActionId;

        return row >= 0 && row < CharacterSheetLayout.LowerHalfAttributeCount ? row : -1;
    }
}
