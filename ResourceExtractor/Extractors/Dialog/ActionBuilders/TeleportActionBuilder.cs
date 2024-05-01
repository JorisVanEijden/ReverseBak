namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class TeleportActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        int destinationId = resourceReader.ReadInt16();
        _ = resourceReader.ReadBytes(6);
        return new TeleportAction {
            DestinationId = destinationId
        };
    }
}