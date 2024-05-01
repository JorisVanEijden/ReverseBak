namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

internal class ResizeDialogActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort x = resourceReader.ReadUInt16();
        ushort y = resourceReader.ReadUInt16();
        ushort width = resourceReader.ReadUInt16();
        ushort height = resourceReader.ReadUInt16();

        return new ResizeDialogAction {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
}