namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal interface IDialogActionBuilder {
    DialogActionBase Build(BinaryReader resourceReader);
}