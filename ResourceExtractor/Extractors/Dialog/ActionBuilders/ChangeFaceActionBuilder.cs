namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData;

using ResourceExtractor.Resources.Dialog.Actions;

internal class ChangeFaceActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new ChangeFaceAction {
            Actor1 = resourceReader.ReadUInt16(),
            Actor2 = resourceReader.ReadUInt16(),
            Actor3 = resourceReader.ReadUInt16(),
            Actor4 = resourceReader.ReadUInt16(),
        };
    }
}