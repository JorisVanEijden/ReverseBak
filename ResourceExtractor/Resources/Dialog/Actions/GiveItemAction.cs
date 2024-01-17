namespace ResourceExtractor.Resources.Dialog.Actions;

internal class GiveItemAction : DialogActionBase {
    public int ObjectId { get; set; }
    public int Actor { get; set; }
    public int Amount { get; set; }
}