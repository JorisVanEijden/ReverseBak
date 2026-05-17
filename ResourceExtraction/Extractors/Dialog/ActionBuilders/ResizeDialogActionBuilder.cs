namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class ResizeDialogActionBuilder : IDialogActionBuilder {
    // VGA reference resolution baked into the original game's dialog coords.
    // Conversion to viewport percentages (0..100) happens here so downstream
    // consumers see only resolution-independent values.
    private const float VgaWidth = 320f;
    private const float VgaHeight = 200f;

    public DialogActionBase Build(BinaryReader resourceReader) {
        ushort x = resourceReader.ReadUInt16();
        ushort y = resourceReader.ReadUInt16();
        ushort width = resourceReader.ReadUInt16();
        ushort height = resourceReader.ReadUInt16();

        return new ResizeDialogAction {
            LeftPct = x / VgaWidth * 100f,
            TopPct = y / VgaHeight * 100f,
            WidthPct = width / VgaWidth * 100f,
            HeightPct = height / VgaHeight * 100f
        };
    }
}
