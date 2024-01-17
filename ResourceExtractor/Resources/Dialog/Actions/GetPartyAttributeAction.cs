namespace ResourceExtractor.Resources.Dialog.Actions;

using GameData;

using ResourceExtractor.Extractors.Dialog;

internal class GetPartyAttributeAction : DialogActionBase {
    /// <summary>
    /// 1 = whole party, 2 = 1st party member, 3 = 2nd party member, etc.
    /// </summary>
    public int Target { get; set; }
    public ActorAttribute Attribute { get; set; }
}