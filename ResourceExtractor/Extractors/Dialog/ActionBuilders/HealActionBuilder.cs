namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class HealActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort target = resourceReader.ReadUInt16();
        short amount = resourceReader.ReadInt16();
        _ = resourceReader.ReadUInt32(); // unused data
        
        return new HealAction {
            Target = target,
            Amount = amount,
        };
    }
}