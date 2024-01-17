namespace ResourceExtractor.Extractors.Dialog;

using GameData;

using ResourceExtractor.Extractors.Dialog.ActionBuilders;
using ResourceExtractor.Resources.Dialog.Actions;

internal static class DialogActionFactory {
    private static readonly Dictionary<DialogActionType, IDialogActionBuilder> Builders = new() {
        {
            DialogActionType.SetTextVariable, new SetTextVariableActionBuilder()
        }, {
            DialogActionType.GiveItem, new GiveItemActionBuilder()
        }, {
            DialogActionType.RemoveItem, new RemoveItemActionBuilder()
        }, {
            DialogActionType.SetGlobalValue, new SetGlobalValueActionBuilder()
        }, {
            DialogActionType.ResizeDialog, new ResizeDialogActionBuilder()
        }, {
            DialogActionType.ApplyCondition, new ApplyConditionActionBuilder()
        }, {
            DialogActionType.ChangeAttribute, new ChangeAttributeActionBuilder()
        }, {
            DialogActionType.GetPartyAttribute, new GetPartyAttributeActionBuilder()
        }, {
            DialogActionType.PlayAudio, new PlayAudioActionBuilder()
        }, {
            DialogActionType.AdvanceTime, new AdvanceTimeActionBuilder()
        }
        // ... and so on for all 23 action types
    };

    public static DialogActionBase Build(DialogActionType actionType, BinaryReader resourceReader) {
        DialogActionBase result;
        if (Builders.TryGetValue(actionType, out IDialogActionBuilder? builder)) {
            result = builder.Build(resourceReader);
        } else {
            // throw new ArgumentException($"Invalid action type: {actionType}");
            result = new UnknownDialogAction {
                Field0 = actionType,
                Field2 = resourceReader.ReadUInt16(),
                Field4 = resourceReader.ReadUInt16(),
                Field6 = resourceReader.ReadUInt16(),
                Field8 = resourceReader.ReadUInt16()
            };
        }
        return result;
    }
}