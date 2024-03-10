namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class GiveItemActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        byte objectId = resourceReader.ReadByte();
        byte actor = resourceReader.ReadByte();
        ushort amount = resourceReader.ReadUInt16();
        ushort cost = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // unused data

        return new GiveItemAction {
            ObjectId = objectId,
            Actor = actor,
            Amount = amount,
            Cost = cost
        };
    }
}