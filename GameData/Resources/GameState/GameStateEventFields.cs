namespace GameData.Resources.GameState;

/// <summary>
/// The event ids that are <b>game-state FIELDS, not flags</b> — <c>gstate_event_write</c>
/// (canassa GAME/STATE/GSTATE.C:118).
/// </summary>
/// <remarks>
/// <b>The "flag" space has three regions and only two of them are bitmaps.</b> Below
/// <see cref="LowBitmapLimit"/> an id is a bit in the low event bitmap; at or above
/// <see cref="HighBitmapBase"/> it is a bit in the high one; <b>and everything between is a switch
/// onto named GameState fields</b>. A port that treats the whole space as flags stores those writes
/// somewhere nothing reads, and the effect simply does not happen.
///
/// <para><b>This is why grepping for <c>go_to_chapter</c> finds nothing.</b> Changing the chapter is
/// <see cref="Field.Chapter"/> — an ordinary "set flag 30007" from dialog data, not a routine. The
/// same is true of the world-loop exit that ends a chapter.</para>
///
/// <para><b>The shipped dialogs use six of these ids, 38 times</b> (counted over
/// <c>generated/DDX</c>): 30014 x17, 30017 x7, 30000 x5, 30016 x4, 30015 x3 and 30004 x2. So this is
/// live content, not a facility waiting for one. <b>30007 — the chapter — is used by NO shipped
/// dialog</b>, which is worth knowing before assuming it is how chapters advance.</para>
/// </remarks>
public static class GameStateEventFields {
    /// <summary>Ids below this are bits in the low event bitmap.</summary>
    public const int LowBitmapLimit = 0x2134;

    /// <summary>Ids at or above this are bits in the high event bitmap.</summary>
    public const int HighBitmapBase = 0xdac0;

    /// <summary>Where the field range starts: <c>0x7530</c>, 30000 decimal.</summary>
    public const int FieldBase = 0x7530;

    /// <summary>A named GameState field an event write can target.</summary>
    public enum Field {
        /// <summary>Not one of them — the id is a bitmap flag, or an unmapped id in the range.</summary>
        None = 0,

        /// <summary>+0 — <c>nEvtArgCount</c>, the dialog's arg count.</summary>
        EventArgCount,

        /// <summary>
        /// +6 — clears <c>dwLastActionTimeSnapshot</c>.
        /// </summary>
        /// <remarks>
        /// <b>It writes ZERO whatever value was supplied.</b> The arm is
        /// <c>g_gameState.dwLastActionTimeSnapshot = 0;</c> with the value ignored, so this id is a
        /// RESET and not an assignment — passing 5 does not make it 5.
        /// </remarks>
        ClearLastActionSnapshot,

        /// <summary>+7 — the chapter number.</summary>
        Chapter,

        /// <summary>+14 — <c>lEvtArgGoldCost</c>, the price a dialog quotes.</summary>
        EventArgGoldCost,

        /// <summary>+15 — <c>lEvtArgValue</c>.</summary>
        EventArgValue,

        /// <summary>+16 — <c>bCombatExitRequest</c>; our <c>PartyDeathState</c>.</summary>
        PartyDeathState,

        /// <summary>+17 — <c>nWorldLoopExitRequest</c>; our <c>ChapterTransitionPending</c>.</summary>
        WorldLoopExitRequest,

        /// <summary>+18 — <c>lEvtArgAuxValue</c>.</summary>
        EventArgAuxValue,
    }

    /// <summary>
    /// Which field an event id writes, or <see cref="Field.None"/>.
    /// </summary>
    /// <remarks>
    /// <b>The offsets are sparse and the gaps are real.</b> 1-5, 8-13 and anything past 18 fall to
    /// the original's <c>default:</c>, which hands off to a stub — <b>including 30004, which two
    /// shipped dialogs write</b>. Mapping the range densely would give those a field they do not
    /// have.
    /// </remarks>
    public static Field FieldFor(int eventId) {
        if (eventId < LowBitmapLimit || eventId >= HighBitmapBase) {
            return Field.None;
        }
        switch (eventId - FieldBase) {
            case 0: return Field.EventArgCount;
            case 6: return Field.ClearLastActionSnapshot;
            case 7: return Field.Chapter;
            case 14: return Field.EventArgGoldCost;
            case 15: return Field.EventArgValue;
            case 16: return Field.PartyDeathState;
            case 17: return Field.WorldLoopExitRequest;
            case 18: return Field.EventArgAuxValue;
            default: return Field.None;
        }
    }

    /// <summary>Whether an id is a bitmap flag rather than a field or an unmapped middle id.</summary>
    public static bool IsBitmapFlag(int eventId) =>
        eventId < LowBitmapLimit || eventId >= HighBitmapBase;

    /// <summary>
    /// The value actually stored for a field — which is not always the one supplied.
    /// </summary>
    /// <inheritdoc cref="Field.ClearLastActionSnapshot"/>
    public static int ValueWritten(Field field, int requested) =>
        field == Field.ClearLastActionSnapshot ? 0 : requested;
}
