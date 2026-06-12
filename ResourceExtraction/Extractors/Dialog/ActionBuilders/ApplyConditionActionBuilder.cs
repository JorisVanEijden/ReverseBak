namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData;
using GameData.Resources.Dialog.Actions;

using System.IO;

internal class ApplyConditionActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new ApplyConditionAction {
            Target = resourceReader.ReadUInt16(),
            Condition = (ActorCondition)resourceReader.ReadUInt16(),
            MinimumAmount = resourceReader.ReadInt16(),
            MaximumAmount = resourceReader.ReadUInt16()
        };
    }
}
