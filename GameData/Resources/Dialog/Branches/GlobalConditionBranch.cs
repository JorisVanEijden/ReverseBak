namespace GameData.Resources.Dialog.Branches;

/// <summary>
/// <c>globalKey</c> in <c>1..55999</c>. Taken when
/// <c>GetGlobalValue(GlobalKey)</c> falls within the inclusive range
/// <c>[Min, Max]</c>. <see cref="Max"/> is <c>null</c> when there is no upper
/// bound (the engine's <c>0xFFFF</c> sentinel). A few well-known keys carry
/// extra meaning the consumer may special-case (e.g. <c>30001</c> = party gold,
/// <c>30000</c> = last gameplay outcome) but they evaluate by the same range
/// rule.
/// </summary>
public class GlobalConditionBranch : DialogBranchBase {
    public int GlobalKey { get; set; }
    public int Min { get; set; }
    public int? Max { get; set; }
}
