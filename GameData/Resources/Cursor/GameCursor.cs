namespace GameData.Resources.Cursor;

/// <summary>Engine-independent semantic cursor identities, mapped to (set,index) by cursor-map.json.
/// Names are traceable to the DOS SetPointerImage / SetDefaultMousePointer call sites.</summary>
public enum GameCursor {
    Arrow,    // POINTER[0] / POINTERG[0] - default (SetPointerImage(-1) falls back here)
    Wait,     // POINTERG[2] - hourglass (GDS scenes)
    Examine,  // POINTERG[3] - magnifying glass (GDS scenes)
    // POINTERG[4..26] are baked text-label cursors for GDS scene hotspots (Exit/Shop/Tavern/...).
    // They are data-bound to scene hotspots, not fixed semantic identities, so they are not enum members.
}
