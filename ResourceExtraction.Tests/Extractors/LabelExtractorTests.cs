namespace ResourceExtraction.Tests.Extractors;

using System.IO;
using GameData.Resources.Label;
using ResourceExtraction.Extractors;
using Xunit;

/// <summary>
/// Verifies LBL_*.DAT parsing maps the raw colorIndex byte onto the semantic
/// <see cref="LabelRole"/> selector: colorIndex 10 is the title pen, everything else a caption.
/// </summary>
public class LabelExtractorTests {
    [Fact]
    public void Extract_MapsColorIndexTenToTitle_AndOtherValuesToCaption() {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true)) {
            writer.Write((ushort)2); // numberOfEntries

            // Entry 0: title
            writer.Write((short)0);  // Offset -> "Title"
            writer.Write((short)1);  // XPosition
            writer.Write((short)2);  // YPosition
            writer.Write((short)0);  // Attributes
            writer.Write((byte)10);  // ColorIndex -> Title
            writer.Write((byte)0);   // ShadowColorIndex (discarded)

            // Entry 1: caption
            writer.Write((short)6);  // Offset -> "Caption"
            writer.Write((short)3);  // XPosition
            writer.Write((short)4);  // YPosition
            writer.Write((short)0);  // Attributes
            writer.Write((byte)5);   // ColorIndex -> Caption
            writer.Write((byte)0);   // ShadowColorIndex (discarded)

            byte[] pool = System.Text.Encoding.ASCII.GetBytes("Title\0Caption\0");
            writer.Write((ushort)pool.Length); // stringBufferSize
            writer.Write(pool);
        }
        stream.Position = 0;

        var result = new LabelExtractor().Extract("LBL_TEST.DAT", stream);

        Assert.Equal(2, result.Labels.Count);
        Assert.Equal(LabelRole.Title, result.Labels[0].Role);
        Assert.Equal("Title", result.Labels[0].Text);
        Assert.Equal(LabelRole.Caption, result.Labels[1].Role);
        Assert.Equal("Caption", result.Labels[1].Text);
    }
}
