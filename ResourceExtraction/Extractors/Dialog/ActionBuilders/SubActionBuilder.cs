namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class SubActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new SubAction {
            SubActionType = (SubActionType)resourceReader.ReadUInt16(),
            Field2 = resourceReader.ReadUInt16(),
            Field4 = resourceReader.ReadUInt16(),
            Field6 = resourceReader.ReadUInt16()
        };
    }
}
