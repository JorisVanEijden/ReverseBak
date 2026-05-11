namespace GameData.Resources.Dialog;

/// <summary>
/// One outgoing edge from a <see cref="DialogEntry"/>. Branch evaluation
/// follows one of two paths depending on the entry's flags:
/// <list type="bullet">
/// <item><b>Main branch loop</b> (default): <see cref="Unknown3"/> and
///   <see cref="Unknown4"/> are min/max bounds for an **inclusive range
///   check** against <c>GetGlobalValue(GlobalKey)</c>. Special key values:
///   <c>0</c> = always matches (default branch), <c>30000</c> = multi-source
///   outcome global, <c>30001</c> = party gold. The pattern
///   <c>Unknown4 = -1</c> (0xFFFF) means "no upper bound".
/// </item>
/// <item><b>Bitfield mode</b> (when <c>GlobalKey ≥ 56000 AND GlobalKey % 10
///   == 0</c>): the key encodes an index into <c>global_flags2</c>;
///   <see cref="Unknown3"/> and <see cref="Unknown4"/> are interpreted
///   byte-by-byte as XOR mask, match mask, and per-chapter bit. See
///   <c>docs/specs/dialog-system.md §"Bitfield-Mode Detail"</c>.
/// </item>
/// <item><b>Choice/keyword menu</b> (entry flag <c>0x0400</c>): the engine
///   instead routes through <c>EvaluateDialogCondition</c> (15 hardcoded
///   conditions + <c>+6700</c> suppression namespace). The
///   <see cref="Unknown3"/> / <see cref="Unknown4"/> values are not used
///   in this path.
/// </item>
/// </list>
/// </summary>
public class DialogEntryBranch {
    public int GlobalKey { get; set; }
    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }
    public int? TargetOffset { get; set; }
    public int? TargetId { get; set; }
}
