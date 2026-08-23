namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using ResourceExtraction.Extractors.GameState;

using System.IO;

internal class SetGlobalValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort key = resourceReader.ReadUInt16();
        // The three bytes the setter applies IN THIS ORDER: byte &= andMask, byte |= orMask,
        // byte ^= xorMask (DIALOG.C's op-4 handler). They are three separate masks, not a
        // "which bits" mask plus its data — see GlobalRef.DecodeEffect.
        byte andMask = resourceReader.ReadByte();
        byte orMask = resourceReader.ReadByte();
        byte xorMask = resourceReader.ReadByte();
        _ = resourceReader.ReadByte(); // high half of the same word; the setter never reads it
        ushort value = resourceReader.ReadUInt16();

        return new GlobalEffectAction {
            Effect = GlobalRef.DecodeEffect(key, andMask, orMask, xorMask, value),
        };
    }
}
