namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class GiveItemActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var objectId = resourceReader.ReadByte();
        var actor = resourceReader.ReadByte();
        var amount = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt32(); // unused data

        return new GiveItemAction {
            ObjectId = objectId,
            Actor = actor,
            Amount = amount
        };
    }
}