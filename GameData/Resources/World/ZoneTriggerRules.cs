namespace GameData.Resources.World;

/// <summary>
/// When stepping onto a zone-boundary hotspot actually takes the party across.
/// Faithful port of <c>zoneTrigger_phase1</c> @0x74a82, the gate in front of
/// <c>zoneTrigger_phase2</c> @0x74af2.
/// </summary>
/// <remarks>
/// <b>A boundary asks before it moves you.</b> The crossing is two phases, and the first is a
/// prompt whose answer decides whether the second runs at all — so walking into the edge of a zone
/// is an offer, not a trapdoor. A port that treated the hotspot as "step here, change zone" would
/// let the party fall out of the map by brushing a border.
/// </remarks>
public static class ZoneTriggerRules {
    /// <summary>
    /// The dialog result that means "cross". Anything else stays put.
    /// </summary>
    /// <remarks>
    /// The polarity is not a guess: phase 1 sets its proceed flag with
    /// <c>or ax, ax / jnz</c> after the dialog call, so zero and only zero continues.
    /// </remarks>
    public const int ProceedResult = 0;

    /// <summary>
    /// Whether this boundary can ever be crossed.
    /// </summary>
    /// <remarks>
    /// <b>A record with no prompt never crosses.</b> That reads backwards — one would expect a
    /// missing question to mean "just go" — but phase 1 leaves its proceed flag clear on that path,
    /// so the boundary is inert. Every one of the 39 shipped records names a prompt, so the inert
    /// case is unreachable with shipped data; it is modelled because a mod that clears the field
    /// would otherwise get a silent teleport where the original gives nothing.
    /// </remarks>
    public static bool CanCross(uint confirmDialogId) => confirmDialogId != 0;

    /// <summary>
    /// Whether the party crosses, given the prompt's answer.
    /// </summary>
    /// <param name="confirmDialogId">The record's <c>DialogId1</c>.</param>
    /// <param name="dialogResult">What showing it returned.</param>
    public static bool CrossesAfterPrompt(uint confirmDialogId, int dialogResult) =>
        CanCross(confirmDialogId) && dialogResult == ProceedResult;

    /// <summary>
    /// Whether the party crosses, given a confirm dialog's boolean answer.
    /// </summary>
    /// <remarks>
    /// The remake's confirm path already collapses the original's <c>result == 0</c> to a bool with
    /// <b>the same polarity</b> — its true is literally <c>chosen == 0</c> — so this overload and
    /// the int one agree by construction rather than by coincidence. Worth stating, because a
    /// confirm helper that returned true for "No" would invert every boundary in the game and
    /// nothing would look wrong.
    /// </remarks>
    public static bool CrossesAfterPrompt(uint confirmDialogId, bool accepted) =>
        CanCross(confirmDialogId) && accepted;

    /// <summary>
    /// Whether phase 2 says anything before moving the party.
    /// </summary>
    /// <remarks>
    /// A statement rather than a question — its result is discarded. Zero in all 39 shipped
    /// records, so nothing in the shipped game uses it.
    /// </remarks>
    public static bool AnnouncesArrival(uint arrivalDialogId) => arrivalDialogId != 0;
}
