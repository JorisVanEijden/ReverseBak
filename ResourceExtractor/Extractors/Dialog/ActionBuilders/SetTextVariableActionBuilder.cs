namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class SetTextVariableActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort variable = resourceReader.ReadUInt16();
        ushort value = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16(); // unknown data 
        _ = resourceReader.ReadUInt16(); // unused data

        return new SetTextVariableAction {
            Slot = variable,
            Source = value,
            Unknown = unknown
        };
    }
}