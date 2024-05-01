namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class ChangePartyActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new ChangePartyAction {
            PartySize = resourceReader.ReadUInt16(),
            Member1 = resourceReader.ReadUInt16(),
            Member2 = resourceReader.ReadUInt16(),
            Member3 = resourceReader.ReadUInt16()
        };
    }
}