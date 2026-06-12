namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using ResourceExtraction.Extractors.GameState;

using System.IO;

internal class SetTemporaryFlagActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        uint globalKey = resourceReader.ReadUInt32();
        uint duration = resourceReader.ReadUInt32();
        return new GlobalEffectAction {
            Effect = GlobalRef.DecodeTemporaryEffect((int)globalKey, duration),
        };
    }
}
