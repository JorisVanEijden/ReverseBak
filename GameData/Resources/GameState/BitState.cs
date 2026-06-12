namespace GameData.Resources.GameState;

/// <summary>
/// One bit position within an unresolved flag group (raw fallback only): bit
/// <see cref="Bit"/> (0–7) of the group must be / is set to <see cref="Set"/>.
/// Used when a group's absolute-key base is not yet confirmed, so the operation
/// is still expressed as a list — never a packed mask.
/// </summary>
public class BitState {
    public int Bit { get; set; }
    public bool Set { get; set; }
}
