namespace GameData.Resources.Dialog.Actions;

using GameData;

#if JSON_SERIALIZE
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(UnknownDialogAction), "UnknownDialogAction")]
[JsonDerivedType(typeof(NullDialogAction), nameof(DialogActionType.NullAction))]
[JsonDerivedType(typeof(SetTextVariableAction), nameof(DialogActionType.SetTextVariable))]
[JsonDerivedType(typeof(GiveItemAction), nameof(DialogActionType.GiveItem))]
[JsonDerivedType(typeof(RemoveItemAction), nameof(DialogActionType.RemoveItem))]
[JsonDerivedType(typeof(SetGlobalValueAction), nameof(DialogActionType.SetGlobalValue))]
[JsonDerivedType(typeof(ChangeFaceAction), nameof(DialogActionType.ChangeFace))]
[JsonDerivedType(typeof(ResizeDialogAction), nameof(DialogActionType.ResizeDialog))]
[JsonDerivedType(typeof(SubAction), nameof(DialogActionType.SubAction))]
[JsonDerivedType(typeof(ApplyConditionAction), nameof(DialogActionType.ApplyCondition))]
[JsonDerivedType(typeof(ChangeAttributeAction), nameof(DialogActionType.ChangeAttribute))]
[JsonDerivedType(typeof(GetPartyAttributeAction), nameof(DialogActionType.GetPartyAttribute))]
[JsonDerivedType(typeof(PlaySoundAction), nameof(DialogActionType.PlaySound))]
[JsonDerivedType(typeof(PlayAudioAction), nameof(DialogActionType.PlayAudio))]
[JsonDerivedType(typeof(AdvanceTimeAction), nameof(DialogActionType.AdvanceTime))]
[JsonDerivedType(typeof(SetTemporaryFlagAction), nameof(DialogActionType.SetTemporaryFlag))]
[JsonDerivedType(typeof(ChangePartyAction), nameof(DialogActionType.ChangeParty))]
[JsonDerivedType(typeof(HealAction), nameof(DialogActionType.Heal))]
[JsonDerivedType(typeof(LearnSpellAction), nameof(DialogActionType.LearnSpell))]
[JsonDerivedType(typeof(TeleportAction), nameof(DialogActionType.Teleport))]
[JsonDerivedType(typeof(SetTimerAction), nameof(DialogActionType.SetTimer))]
[JsonDerivedType(typeof(UseItemAction), nameof(DialogActionType.UseItem))]
#endif

public class DialogActionBase;