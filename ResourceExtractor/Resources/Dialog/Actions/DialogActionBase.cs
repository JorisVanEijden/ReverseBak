namespace ResourceExtractor.Resources.Dialog.Actions;

using GameData;

using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SetTextVariableAction), typeDiscriminator: nameof(DialogActionType.SetTextVariable))]
[JsonDerivedType(typeof(GiveItemAction), typeDiscriminator: nameof(DialogActionType.GiveItem))]
[JsonDerivedType(typeof(RemoveItemAction), typeDiscriminator: nameof(DialogActionType.RemoveItem))]
[JsonDerivedType(typeof(SetGlobalValueAction), typeDiscriminator: nameof(DialogActionType.SetGlobalValue))]
[JsonDerivedType(typeof(ResizeDialogAction), typeDiscriminator: nameof(DialogActionType.ResizeDialog))]
[JsonDerivedType(typeof(ApplyConditionAction), typeDiscriminator: nameof(DialogActionType.ApplyCondition))]
[JsonDerivedType(typeof(ChangeAttributeAction), typeDiscriminator: nameof(DialogActionType.ChangeAttribute))]
[JsonDerivedType(typeof(GetPartyAttributeAction), typeDiscriminator: nameof(DialogActionType.GetPartyAttribute))]
[JsonDerivedType(typeof(PlayAudioAction), typeDiscriminator: nameof(DialogActionType.PlayAudio))]
[JsonDerivedType(typeof(AdvanceTimeAction), typeDiscriminator: nameof(DialogActionType.AdvanceTime))]
[JsonDerivedType(typeof(UnknownDialogAction), typeDiscriminator: nameof(DialogActionType.Unknown))]
public class DialogActionBase;