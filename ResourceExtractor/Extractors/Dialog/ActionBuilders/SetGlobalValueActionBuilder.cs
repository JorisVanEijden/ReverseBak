namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class SetGlobalValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort key = resourceReader.ReadUInt16();
        byte mask = resourceReader.ReadByte();
        byte data = resourceReader.ReadByte();
        _ = resourceReader.ReadUInt16(); // unused data, always 0
        ushort value = resourceReader.ReadUInt16();

        return new SetGlobalValueAction {
            Key = key,
            Mask = mask,
            Data = data,
            Value = value
        };
    }
}