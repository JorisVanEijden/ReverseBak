namespace GameData.Resources.Dialog.Branches;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(DefaultBranch), nameof(DefaultBranch))]
[JsonDerivedType(typeof(ConditionalBranch), nameof(ConditionalBranch))]
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
/// <item><see cref="ConditionalBranch"/> — taken when its Condition holds.</item>
/// <item><see cref="KeywordChoiceBranch"/> — a choice-menu option.</item>
/// </list>
/// <para>
/// The base carries only the destination: exactly one of
/// <see cref="TargetOffset"/> / <see cref="TargetId"/> is non-null.
/// For <see cref="DefaultBranch"/> the key is always 0; for
/// <see cref="KeywordChoiceBranch"/> the key is the keyword index.
/// </para>
/// </summary>
public abstract class DialogBranchBase {
    public int? TargetOffset { get; set; }
    public int? TargetId { get; set; }
}
