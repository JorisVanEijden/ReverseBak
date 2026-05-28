namespace GameData.Resources.Dialog.Branches;

/// <summary>
/// How a <see cref="FlagMaskBranch"/> combines its bit conditions. Maps to the
/// engine's bitfield-mode sub-path selector (<c>LOW(unknown4)</c>): <c>1</c> →
/// <see cref="All"/>, <c>0</c> → <see cref="Any"/>.
/// </summary>
public enum BranchMatchMode {
    /// <summary>Every condition must hold (selector = 1).</summary>
    All,

    /// <summary>At least one condition must hold (selector = 0).</summary>
    Any,
}
