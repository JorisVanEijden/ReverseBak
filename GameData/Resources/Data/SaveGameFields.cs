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
    byte CurrentZone,
    byte WorldX,
    byte WorldY,
    int PositionX,
    int PositionY,
    int PositionZ,
    short Rotation);
