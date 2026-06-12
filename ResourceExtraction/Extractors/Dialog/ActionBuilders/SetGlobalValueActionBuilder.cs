namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using ResourceExtraction.Extractors.GameState;

using System.IO;

internal class SetGlobalValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort key = resourceReader.ReadUInt16();
        byte mask = resourceReader.ReadByte();
        byte data = resourceReader.ReadByte();
        _ = resourceReader.ReadUInt16(); // unused, always 0
        ushort value = resourceReader.ReadUInt16();

        return new GlobalEffectAction {
            Effect = GlobalRef.DecodeEffect(key, mask, data, value),
        };
    }
}
