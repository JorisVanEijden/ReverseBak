namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal interface IDialogActionBuilder {
    DialogActionBase Build(BinaryReader resourceReader);
}