namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData;
using GameData.Resources.Dialog.Actions;

using System.IO;

internal class GetPartyAttributeActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var result = new GetPartyAttributeAction {
            Target = resourceReader.ReadUInt16(),
            Attribute = (ActorAttribute)resourceReader.ReadUInt16()
        };
        _ = resourceReader.ReadUInt32(); // unused data
        return result;
    }
}
