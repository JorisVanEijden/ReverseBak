namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class SetTextVariableActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var variable = resourceReader.ReadUInt16();
        var value = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt32(); // unused data

        return new SetTextVariableAction {
            Variable = variable,
            Value = value
        };
    }
}