namespace ResourceExtractor.Resources.Dialog.Actions;

using GameData;

internal class ChangeAttributeAction : DialogActionBase {
    /// <summary>
    ///     1 = whole party, 2 = 1st party member, 3 = 2nd party member, etc.
    /// </summary>
    public int Target { get; set; }

    public ActorAttribute Attribute { get; set; }
    public int MinimumAmount { get; set; }
    public int MaximumAmount { get; set; }
    public int Type { get; set; }
}