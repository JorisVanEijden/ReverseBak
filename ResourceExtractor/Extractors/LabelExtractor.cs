namespace ResourceExtractor.Extractors;

using ResourceExtractor.Extensions;
using ResourceExtractor.Resources.Label;

using System.Text;

internal class LabelExtractor : ExtractorBase {
    public LabelSet Extract(string filePath) {
        Log($"Extracting {filePath}");
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var labelSet = new LabelSet(Path.GetFileNameWithoutExtension(filePath));

        ushort numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            var label = new Label {
                Offset = resourceReader.ReadInt16(),
                XPosition = resourceReader.ReadInt16(),
                YPosition = resourceReader.ReadInt16(),
                Attributes = (LabelAttributes)resourceReader.ReadInt16(),
                ColorIndex = resourceReader.ReadByte(),
                ShadowColorIndex = resourceReader.ReadByte()
            };
            labelSet.Labels.Add(label);
        }
        ushort stringBufferSize = resourceReader.ReadUInt16();
        long position = resourceReader.BaseStream.Position;
        foreach (Label label in labelSet.Labels) {
            resourceReader.BaseStream.Seek(position + label.Offset, SeekOrigin.Begin);
            label.Text = resourceReader.ReadZeroTerminatedString();
        }
        return labelSet;
    }
}