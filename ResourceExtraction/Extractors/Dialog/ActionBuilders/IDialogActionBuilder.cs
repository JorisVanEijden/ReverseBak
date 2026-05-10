namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal interface IDialogActionBuilder {
    DialogActionBase Build(BinaryReader resourceReader);
}
