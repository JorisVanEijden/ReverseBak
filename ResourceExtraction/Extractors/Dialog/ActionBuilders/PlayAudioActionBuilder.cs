namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class PlayAudioActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort audioId = resourceReader.ReadUInt16();
        ushort timing = resourceReader.ReadUInt16();

        _ = resourceReader.ReadUInt32(); // unused (dialogAction_PlayAudio field_4 + field_6)

        return new PlayAudioAction {
            AudioId = audioId,
            Timing = (PlayAudioTiming)timing
        };
    }
}
