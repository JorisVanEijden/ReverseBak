namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class SetTemporaryFlagActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        uint globalKey = resourceReader.ReadUInt32();
        uint duration = resourceReader.ReadUInt32();
        return new SetTemporaryFlagAction {
            GlobalKey = globalKey,
            Duration = duration
        };
    }
}