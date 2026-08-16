namespace GameData.Resources.Character;

/// <summary>
/// The active party — the two or three characters actually travelling, as every screen that shows a
/// portrait row addresses them.
/// </summary>
/// <remarks>
/// This exists because the same two facts kept being restated: that the roster holds at most three,
/// and that a screen's portrait click areas are consecutive action ids mapping onto it. Each screen
/// picks its own first id (the casting ring starts at 128, the temple healer at 2), so the base is a
/// parameter and only the arithmetic is shared.
/// </remarks>
public static class ActiveParty {
    /// <summary>
    /// Slots in the active roster.
    /// </summary>
    /// <remarks>
    /// <c>TrapPuzzleBuilder.PartySlots</c> is the same fact wearing combat clothes — the markers
    /// -15/-16/-17 a puzzle can place. Left stated separately there because the puzzle expresses it
    /// as a marker range rather than as a roster, and merging the two would tie a healing screen to
    /// the trap-grid format.
    /// </remarks>
    public const int Slots = 3;

    /// <summary>
    /// The roster slot a consecutive portrait action id refers to, or -1 when it refers to none.
    /// </summary>
    /// <param name="actionId">The REQ entry's action id.</param>
    /// <param name="firstActionId">The action id of the first portrait on that screen.</param>
    public static int SlotForAction(int actionId, int firstActionId) {
        int slot = actionId - firstActionId;
        return slot >= 0 && slot < Slots ? slot : -1;
    }
}
