namespace ResourceExtraction.Extractors;

using GameData.Resources.Label;

using ResourceExtraction.Extensions;
using ResourceExtraction.Imaging;

using System.IO;
using System.Text;

public class LabelExtractor : ExtractorBase<LabelSet> {
    public override LabelSet Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var labelSet = new LabelSet(id);
        ushort numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            var label = new Label {
                Offset = resourceReader.ReadInt16(),
                XPosition = resourceReader.ReadInt16(),
                YPosition = resourceReader.ReadInt16(),
                Attributes = (LabelAttributes)resourceReader.ReadInt16(),
            };
            int colorIndex = resourceReader.ReadByte();
            _ = resourceReader.ReadByte(); // shadow colour index — Unity theme concern now, discarded
            label.Role = colorIndex == 10 ? LabelRole.Title : LabelRole.Caption;
            labelSet.Labels.Add(label);
        }
        ushort stringBufferSize = resourceReader.ReadUInt16();
        long position = resourceReader.BaseStream.Position;
        foreach (Label label in labelSet.Labels) {
            resourceReader.BaseStream.Seek(position + label.Offset, SeekOrigin.Begin);
            label.Text = resourceReader.ReadZeroTerminatedString();
        }

        CanonicalSpace.Apply(labelSet);
        return labelSet;
    }
}