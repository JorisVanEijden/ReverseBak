namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class SetReturnValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort value = resourceReader.ReadUInt16();
        ushort field2 = resourceReader.ReadUInt16();
        ushort field4 = resourceReader.ReadUInt16();
        ushort field6 = resourceReader.ReadUInt16();
        return new SetReturnValueAction {
            Value = value,
            Field2 = field2,
            Field4 = field4,
            Field6 = field6
        };
    }
}
