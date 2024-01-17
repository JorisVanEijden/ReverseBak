namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

internal class ResizeDialogActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var x = resourceReader.ReadUInt16();
        var y = resourceReader.ReadUInt16();
        var width = resourceReader.ReadUInt16();
        var height = resourceReader.ReadUInt16();

        return new ResizeDialogAction {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
}