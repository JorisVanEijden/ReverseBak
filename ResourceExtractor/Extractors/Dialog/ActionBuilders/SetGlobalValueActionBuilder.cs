namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class SetGlobalValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var key = resourceReader.ReadUInt16();
        var mask = resourceReader.ReadByte();
        var data = resourceReader.ReadByte();
        _ = resourceReader.ReadUInt16(); // unused data, always 0
        var value = resourceReader.ReadUInt16();

        return new SetGlobalValueAction {
            Key = key,
            Mask = mask,
            Data = data,
            Value = value
        };
    }
}