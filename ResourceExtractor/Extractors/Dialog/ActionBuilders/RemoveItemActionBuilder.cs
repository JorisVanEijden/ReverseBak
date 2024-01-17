namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class RemoveItemActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var objectId = resourceReader.ReadUInt16();
        var amount = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused data
        return new RemoveItemAction {
            ObjectId = objectId,
            Amount = amount
        };
    }
}