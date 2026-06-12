namespace GameData.Resources.GameState;

/// <summary>
/// One resolved flag in a list of flag operations: the global key
/// <see cref="Flag"/> must be / is set to <see cref="Set"/>. Used by
/// <c>SetFlagsEffect</c> and the composite conditions. <see cref="Flag"/> is the
/// raw global key (0–8499 for <c>global_flags</c>; <c>56000 + group*10 + bit + 1</c>
/// for a <c>global_flags2</c> bit).
/// </summary>
public class FlagState {
    public int Flag { get; set; }
    public bool Set { get; set; }
}
