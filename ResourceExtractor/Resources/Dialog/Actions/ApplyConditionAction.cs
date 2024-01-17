namespace ResourceExtractor.Resources.Dialog.Actions;

using GameData;

internal class ApplyConditionAction : DialogActionBase {
    /// <summary>
    /// 1 = whole party, 2 = 1st party member, 3 = 2nd party member, etc.
    /// </summary>
    public int Target { get; set; }
    public Condition Condition { get; set; }
    public int MinimumAmount { get; set; }
    public int MaximumAmount { get; set; }
}