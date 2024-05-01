namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData;
using GameData.Resources.Dialog.Actions;

internal class ChangeAttributeActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new ChangeAttributeAction {
            Target = resourceReader.ReadByte(),
            Type = resourceReader.ReadByte(),
            Attribute = (ActorAttribute)resourceReader.ReadUInt16(),
            MinimumAmount = resourceReader.ReadInt16(),
            MaximumAmount = resourceReader.ReadUInt16()
        };
    }
}