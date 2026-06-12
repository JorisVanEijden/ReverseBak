namespace GameData.Resources.Dialog.Branches;

using GameData.Resources.GameState;

/// <summary>
/// An outgoing edge taken when <see cref="Condition"/> holds. The uniform carrier
/// for every global-state-gated branch — the variety lives in the embedded
/// <see cref="Condition"/> (a flag test, item check, composite, …), shared with
/// GDS hotspot gates and DEF triggers. Replaces the former
/// <c>GlobalConditionBranch</c>/<c>FlagBitBranch</c>/<c>FlagMaskBranch</c>.
/// </summary>
public class ConditionalBranch : DialogBranchBase {
    public Condition Condition { get; set; } = null!;
}
