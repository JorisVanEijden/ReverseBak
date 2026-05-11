namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class DisposeActorFacesActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        _ = resourceReader.ReadBytes(8); // payload unused by the engine
        return new DisposeActorFacesAction();
    }
}
