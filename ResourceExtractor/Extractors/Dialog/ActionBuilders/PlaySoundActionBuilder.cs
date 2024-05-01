namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class PlaySoundActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort audioId = resourceReader.ReadUInt16();
        _ = resourceReader.ReadBytes(6); // unused data

        return new PlaySoundAction {
            AudioId = audioId,
        };
    }
}