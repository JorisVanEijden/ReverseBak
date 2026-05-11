namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class PushDialogEntryActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        int offset = resourceReader.ReadInt32();
        ushort field4 = resourceReader.ReadUInt16();
        ushort field6 = resourceReader.ReadUInt16();
        return new PushDialogEntryAction {
            Offset = offset,
            Field4 = field4,
            Field6 = field6
        };
    }
}
