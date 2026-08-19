namespace ResourceExtraction.Extractors;

using GameData.Resources.Font;

using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;

using ResourceExtractor.Compression;

using System.IO;
using System.Text;

/// <summary>
/// Parses a <c>.FNT</c> file into its metrics and glyph bitmaps.
/// </summary>
/// <remarks>
/// The parsing itself is not new — the CLI carried it as a console dumper that printed each glyph
/// as hashes and dropped it. What is new is keeping the result, which is what a consumer of the
/// SPELL.FNT symbols needs: they are pictures, and nothing else in the archive holds them.
/// </remarks>
public class FontExtractor : ExtractorBase<FontResource> {
    private const string Tag = "FNT";

    public override FontResource Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        string tag = resourceReader.ReadTag();
        if (!tag.Equals(Tag)) {
            throw new InvalidDataException($"Invalid tag '{tag}' — expected '{Tag}'.");
        }

        _ = resourceReader.ReadUInt32();               // file size
        _ = resourceReader.ReadByte();                 // version
        _ = resourceReader.ReadByte();                 // nominal width; the per-glyph widths rule
        byte height = resourceReader.ReadByte();
        byte baseline = resourceReader.ReadByte();
        byte firstCharacter = resourceReader.ReadByte();
        byte glyphCount = resourceReader.ReadByte();
        _ = resourceReader.ReadUInt16();               // payload length
        var compressionType = (CompressionType)resourceReader.ReadByte();
        _ = resourceReader.ReadUInt32();               // decompressed size

        ICompression compression = CompressionFactory.Create(compressionType);
        // What the decompressor wants is how many COMPRESSED bytes to read, which is the rest of
        // the file — not the decompressed size the header states. Handing it the latter overruns
        // the stream, and an uncompressed font handed a zero would come back empty, so the
        // remaining length is the answer that suits both.
        long remaining = resourceReader.BaseStream.Length - resourceReader.BaseStream.Position;
        using var reader = new BinaryReader(
            compression.Decompress(resourceReader.BaseStream, remaining));

        var font = new FontResource(id) {
            Height = height,
            Baseline = baseline,
            FirstCharacter = firstCharacter,
        };

        // The offset table is read and discarded: the glyph data follows in the same order, so the
        // offsets only matter to a reader that seeks. Reading them keeps the stream in step.
        for (var i = 0; i < glyphCount; i++) {
            _ = reader.ReadUInt16();
        }

        var widths = new int[glyphCount];
        for (var i = 0; i < glyphCount; i++) {
            widths[i] = reader.ReadByte();
        }

        for (var i = 0; i < glyphCount; i++) {
            var glyph = new FontGlyph { Width = widths[i] };
            for (var row = 0; row < height; row++) {
                // CEIL(width / 8) bytes, most significant bit leftmost. Every shipped font fits in
                // one or two, so this is only a generalisation of what they need — but it is the
                // rule the format states, and a mod's font is not bound by what shipped.
                glyph.Rows.Add(reader.ReadBytes(glyph.BytesPerRow));
            }
            font.Glyphs.Add(glyph);
        }

        return font;
    }
}
