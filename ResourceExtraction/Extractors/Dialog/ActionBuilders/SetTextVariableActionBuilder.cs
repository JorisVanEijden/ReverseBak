namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class SetTextVariableActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort slot = resourceReader.ReadUInt16();
        ushort kind = resourceReader.ReadUInt16();
        ushort aux = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // unused data

        return new SetTextVariableAction {
            Slot = slot,
            Source = kind,
            Aux = aux
        };
    }
}
