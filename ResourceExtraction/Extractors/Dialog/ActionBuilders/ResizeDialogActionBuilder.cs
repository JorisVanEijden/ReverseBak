namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;
using GameData.Resources.Layout;

using System.IO;

internal class ResizeDialogActionBuilder : IDialogActionBuilder {
    // Only px is ever built here: the binary holds raw VGA ushorts and there is no percentage
    // anywhere in the original data (see the doc comment on ResizeDialogAction). CanonicalSpace
    // .Apply(Dialog) later rescales these px values from VGA into canonical space.
    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort x = resourceReader.ReadUInt16();
        ushort y = resourceReader.ReadUInt16();
        ushort width = resourceReader.ReadUInt16();
        ushort height = resourceReader.ReadUInt16();

        return new ResizeDialogAction {
            Left = LayoutLength.Px(x),
            Top = LayoutLength.Px(y),
            Width = LayoutLength.Px(width),
            Height = LayoutLength.Px(height)
        };
    }
}
