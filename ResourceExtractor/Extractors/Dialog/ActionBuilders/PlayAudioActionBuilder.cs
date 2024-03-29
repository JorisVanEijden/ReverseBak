namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class PlayAudioActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort audioId = resourceReader.ReadUInt16();
        ushort shouldPlay = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused data

        return new PlayAudioAction {
            AudioId = audioId,
            ShouldPlay = shouldPlay > 0
        };
    }
}