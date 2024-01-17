namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class PlayAudioActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var audioId = resourceReader.ReadUInt16();
        var unknown = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused data

        return new PlayAudioAction {
            AudioId = audioId,
            Unknown = unknown
        };
    }
}