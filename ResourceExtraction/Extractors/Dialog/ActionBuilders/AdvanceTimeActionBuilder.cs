namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class AdvanceTimeActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        uint seconds = resourceReader.ReadUInt32() * 2;

        _ = resourceReader.ReadUInt32(); // unused data

        return new AdvanceTimeAction {
            Seconds = seconds
        };
    }
}
