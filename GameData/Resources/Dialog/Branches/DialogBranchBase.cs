namespace GameData.Resources.Dialog.Branches;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(DefaultBranch), nameof(DefaultBranch))]
[JsonDerivedType(typeof(GlobalConditionBranch), nameof(GlobalConditionBranch))]
[JsonDerivedType(typeof(FlagBitBranch), nameof(FlagBitBranch))]
[JsonDerivedType(typeof(FlagMaskBranch), nameof(FlagMaskBranch))]
[JsonDerivedType(typeof(KeywordChoiceBranch), nameof(KeywordChoiceBranch))]
#endif

/// <summary>
/// One outgoing edge from a <see cref="DialogEntry"/>. The concrete subtype
/// captures <em>how</em> the engine decides to take the branch — decoded from
/// the raw <c>globalKey</c>/<c>unknown3</c>/<c>unknown4</c> triple so consumers
/// never touch the original bit-encoding. See
/// <c>docs/specs/dialog-system.md</c> §"Branch Evaluation".
/// <list type="bullet">
/// <item><see cref="DefaultBranch"/> — always taken (globalKey 0).</item>
/// <item><see cref="GlobalConditionBranch"/> — range check on a global.</item>
/// <item><see cref="FlagBitBranch"/> — single quest-flag bit read.</item>
/// <item><see cref="FlagMaskBranch"/> — masked quest-flag test.</item>
/// <item><see cref="KeywordChoiceBranch"/> — a choice-menu option.</item>
/// </list>
/// <para>
/// The base carries only the destination: exactly one of
/// <see cref="TargetOffset"/> / <see cref="TargetId"/> is non-null. The raw
/// engine key lives only on the subtypes that need it
/// (<see cref="GlobalConditionBranch.GlobalKey"/>,
/// <see cref="KeywordChoiceBranch.Keyword"/>); for the flag subtypes it is fully
/// determined by <c>FlagGroup</c>/<c>Bit</c>, and for <see cref="DefaultBranch"/>
/// it is always 0.
/// </para>
/// </summary>
public abstract class DialogBranchBase {
    public int? TargetOffset { get; set; }
    public int? TargetId { get; set; }
}
