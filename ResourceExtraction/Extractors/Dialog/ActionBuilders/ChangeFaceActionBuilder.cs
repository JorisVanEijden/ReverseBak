namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData;
using GameData.Resources.Dialog.Actions;

using System.IO;

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
