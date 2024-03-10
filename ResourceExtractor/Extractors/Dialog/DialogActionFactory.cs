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
            DialogActionType.PlaySound, new PlaySoundActionBuilder()
        }, {
            DialogActionType.PlayAudio, new PlayAudioActionBuilder()
        }, {
            DialogActionType.AdvanceTime, new AdvanceTimeActionBuilder()
        }, {
            DialogActionType.UseItem, new UseItemActionBuilder()
        }, {
            DialogActionType.ChangeFace, new ChangeFaceActionBuilder()
        }, {
            DialogActionType.ChangeParty, new ChangePartyActionBuilder()
        }, {
            DialogActionType.Heal, new HealActionBuilder()
        }, {
            DialogActionType.SetTemporaryFlag, new SetTemporaryFlagActionBuilder()
        }, {
            DialogActionType.SetTimer, new SetTimerActionBuilder()
        }, {
            DialogActionType.LearnSpell, new LearnSpellActionBuilder()
        }, {
            DialogActionType.Teleport, new TeleportActionBuilder()
        }
        // ... and so on for all 23 action types
    };

    public static DialogActionBase Build(int actionType, BinaryReader resourceReader) {
        DialogActionBase result;
        if (Builders.TryGetValue((DialogActionType)actionType, out IDialogActionBuilder? builder)) {
            result = builder.Build(resourceReader);
        } else {
            if (actionType == 0) {
                result = new NullDialogAction();
                resourceReader.ReadBytes(8);
            } else {
                result = new UnknownDialogAction {
                    Field0 = actionType,
                    Field2 = resourceReader.ReadUInt16(),
                    Field4 = resourceReader.ReadUInt16(),
                    Field6 = resourceReader.ReadUInt16(),
                    Field8 = resourceReader.ReadUInt16()
                };
            }
        }
        return result;
    }
}

[Flags]
internal enum TimerFlag {
    Add = 0x40,
    Reset = 0x80
}

internal enum TimerType {
    Light = 1,
    Spell = 2,
    SetFlag = 3,
    ClearFlag = 4
}