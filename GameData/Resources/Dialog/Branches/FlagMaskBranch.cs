namespace GameData.Resources.Dialog.Branches;

using System.Collections.Generic;

/// <summary>
/// <c>globalKey >= 56000</c> and divisible by 10 — a masked quest-flag test on
/// <c>global_flags2[FlagGroup]</c>. The branch is taken when the bit
/// <see cref="Conditions"/> hold (<see cref="Match"/> = <see cref="BranchMatchMode.All"/>
/// → every condition must hold; <see cref="BranchMatchMode.Any"/> → at least one)
/// <em>and</em> the current chapter is allowed. <see cref="Chapters"/> is
/// <c>null</c> for "any chapter"; otherwise it lists the allowed chapter numbers
/// (where <c>8</c> also covers chapters 9+, since the engine caps the chapter
/// bit at <c>0x80</c>). See <c>docs/specs/dialog-system.md</c>
/// §"Bitfield-Mode Detail".
/// </summary>
public class FlagMaskBranch : DialogBranchBase {
    public int FlagGroup { get; set; }
    public BranchMatchMode Match { get; set; }
    public List<FlagBitCondition> Conditions { get; set; } = [];
    public List<int>? Chapters { get; set; }
}
