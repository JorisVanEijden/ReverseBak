namespace GameData.Resources.Data;

/// <summary>
/// The StateData scalar fields the writer authors into the save body (the subset we currently model).
/// As more of the body is modeled, more fields/blocks move here and passthrough shrinks.
/// </summary>
public readonly record struct SaveGameFields(
    short Chapter,
    int PartyGold,
    int GameTime,
    int TimeSnapshot,
    short PaletteEventMask,
    /// <summary>Non-zero once the whole active party is down; ends the world loop.</summary>
    byte PartyDeathState,

    /// <summary>1 when the world loop should exit into the next chapter.</summary>
    byte ChapterTransitionPending,

    /// <summary>The zone the party came FROM, used to detect a zone CHANGE.</summary>
    byte PreviousZone,

    byte CurrentZone,
    byte WorldX,
    byte WorldY,
    int PositionX,
    int PositionY,
    int PositionZ,
    short Rotation);
