namespace GameData.Resources.Dialog.Branches;

/// <summary>
/// <c>globalKey >= 56000</c> and <b>not</b> divisible by 10 — a single
/// quest-flag bit read. The key decodes to <c>global_flags2[FlagGroup]</c> bit
/// <see cref="Bit"/>; the branch is taken when that bit equals
/// <see cref="RequiredState"/>. All shipped data uses
/// <see cref="RequiredState"/> = <c>true</c> (the canonical "flag is set" form).
/// See <c>docs/specs/dialog-system.md</c> §"global_flags2 Key Encoding".
/// </summary>
public class FlagBitBranch : DialogBranchBase {
    public int FlagGroup { get; set; }
    public int Bit { get; set; }
    public bool RequiredState { get; set; }
}
