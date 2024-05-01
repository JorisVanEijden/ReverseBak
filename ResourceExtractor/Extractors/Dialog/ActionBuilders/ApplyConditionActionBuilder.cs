namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData;
using GameData.Resources.Dialog.Actions;

internal class ApplyConditionActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new ApplyConditionAction {
            Target = resourceReader.ReadUInt16(),
            Condition = (Condition)resourceReader.ReadUInt16(),
            MinimumAmount = resourceReader.ReadInt16(),
            MaximumAmount = resourceReader.ReadUInt16()
        };
    }
}