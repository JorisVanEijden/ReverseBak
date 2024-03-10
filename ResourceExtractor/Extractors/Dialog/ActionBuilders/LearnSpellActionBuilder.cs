namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class LearnSpellActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort actor = resourceReader.ReadUInt16();
        ushort spellId = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt32(); // unused data
        return new LearnSpellAction {
            Actor = actor,
            SpellId = spellId
        };
    }
}