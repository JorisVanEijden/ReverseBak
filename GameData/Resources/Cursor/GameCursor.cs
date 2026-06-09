namespace GameData.Resources.Cursor;

/// <summary>Engine-independent semantic cursor identities, mapped to (set,index) by cursor-map.json.
/// Names are traceable to the DOS SetPointerImage / SetDefaultMousePointer call sites.</summary>
public enum GameCursor {
    Arrow,          // POINTER set, index 0 (defaultPointerImageNumber, always 0)
    // POINTERG context cursors are added in Phase 2 from call-site tracing, e.g.:
    // MoveForward, MoveBack, TurnLeft, TurnRight, CombatTarget, Examine, Wait ...
}
