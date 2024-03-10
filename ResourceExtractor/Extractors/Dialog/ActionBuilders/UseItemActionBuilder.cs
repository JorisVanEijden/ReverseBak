namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class UseItemActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort objectId = resourceReader.ReadUInt16();
        ushort amount = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused data
        return new UseItemAction {
            ObjectId = objectId,
            Amount = amount
        };
    }
}