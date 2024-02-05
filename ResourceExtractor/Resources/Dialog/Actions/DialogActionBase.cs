namespace ResourceExtractor.Resources.Dialog.Actions;

using GameData;

using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SetTextVariableAction), nameof(DialogActionType.SetTextVariable))]
[JsonDerivedType(typeof(GiveItemAction), nameof(DialogActionType.GiveItem))]
[JsonDerivedType(typeof(RemoveItemAction), nameof(DialogActionType.RemoveItem))]
[JsonDerivedType(typeof(SetGlobalValueAction), nameof(DialogActionType.SetGlobalValue))]
[JsonDerivedType(typeof(ResizeDialogAction), nameof(DialogActionType.ResizeDialog))]
[JsonDerivedType(typeof(ApplyConditionAction), nameof(DialogActionType.ApplyCondition))]
[JsonDerivedType(typeof(ChangeAttributeAction), nameof(DialogActionType.ChangeAttribute))]
[JsonDerivedType(typeof(GetPartyAttributeAction), nameof(DialogActionType.GetPartyAttribute))]
[JsonDerivedType(typeof(PlayAudioAction), nameof(DialogActionType.PlayAudio))]
[JsonDerivedType(typeof(AdvanceTimeAction), nameof(DialogActionType.AdvanceTime))]
[JsonDerivedType(typeof(UnknownDialogAction), nameof(DialogActionType.Unknown))]
public class DialogActionBase;