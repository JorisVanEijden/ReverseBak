namespace ResourceExtractor.Resources.Dialog.Actions;

internal class SetGlobalValueAction : DialogActionBase {
    public int Key { get; set; }
    public int Mask { get; set; }
    public int Data { get; set; }
    public int Value { get; set; }
}