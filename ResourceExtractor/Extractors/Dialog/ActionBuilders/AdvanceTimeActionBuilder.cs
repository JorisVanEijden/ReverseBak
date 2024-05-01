namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class AdvanceTimeActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        uint seconds = resourceReader.ReadUInt32() * 2;

        _ = resourceReader.ReadUInt32(); // unused data

        return new AdvanceTimeAction {
            Seconds = seconds
        };
    }
}