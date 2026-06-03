namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class ResizeDialogActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort x = resourceReader.ReadUInt16();
        ushort y = resourceReader.ReadUInt16();
        ushort width = resourceReader.ReadUInt16();
        ushort height = resourceReader.ReadUInt16();

        return new ResizeDialogAction {
            Left = x,
            Top = y,
            Width = width,
            Height = height
        };
    }
}
