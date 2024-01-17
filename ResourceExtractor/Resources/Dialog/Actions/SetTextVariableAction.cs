namespace ResourceExtractor.Resources.Dialog.Actions;

public class SetTextVariableAction : DialogActionBase {
    public ushort Variable { get; set; }
    public ushort Value { get; set; }
}