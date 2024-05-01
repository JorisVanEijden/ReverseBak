namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class PlayAudioActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort audioId = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused data

        return new PlayAudioAction {
            AudioId = audioId,
            Unknown = unknown
        };
    }
}