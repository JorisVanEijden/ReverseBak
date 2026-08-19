namespace ResourceExtraction.Extractors;

using GameData.Resources.Font;

using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;

using ResourceExtractor.Compression;

using System.IO;
using System.Linq;
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

        // *** THE OFFSET TABLE IS THE ONLY THING THAT SAYS HOW WIDE A ROW IS. *** A glyph's span
        // divided by the font's height gives its bytes per row, and that is what tells the two
        // pixel formats apart: GAME.FNT spends one byte on eight pixels, SPELL.FNT spends one byte
        // on each. Deriving the stride from the WIDTH instead reads SPELL as a bitmask and produces
        // noise — its rows are ten bytes for ten pixels.
        var offsets = new int[glyphCount];
        for (var i = 0; i < glyphCount; i++) {
            offsets[i] = reader.ReadUInt16();
        }

        var widths = new int[glyphCount];
        for (var i = 0; i < glyphCount; i++) {
            widths[i] = reader.ReadByte();
        }

        long dataStart = reader.BaseStream.Position;
        long dataLength = reader.BaseStream.Length - dataStart;

        for (var i = 0; i < glyphCount; i++) {
            // The last glyph runs to the end of the buffer; every other one to the next offset.
            long span = (i + 1 < glyphCount ? offsets[i + 1] : dataLength) - offsets[i];
            var glyph = new FontGlyph {
                Width = widths[i],
                BytesPerRow = height > 0 ? (int)(span / height) : 0,
            };

            reader.BaseStream.Seek(dataStart + offsets[i], SeekOrigin.Begin);
            for (var row = 0; row < height; row++) {
                glyph.Rows.Add(reader.ReadBytes(glyph.BytesPerRow));
            }
            font.Glyphs.Add(glyph);
        }

        // *** THE FORMAT IS THE FONT'S, NOT A GLYPH'S. *** A narrow glyph's stride is the same
        // either way — one byte holds one pixel or eight of them — so only a glyph whose row is too
        // wide to be a bitmask carries any evidence, and one such glyph settles the whole font.
        // Deciding per glyph reads BOOK.FNT's two one-pixel glyphs, and 155 of PUZZLE.FNT's, as
        // palette indices in fonts that are bitmasks throughout.
        if (font.Glyphs.Any(g => g.StrideExceedsABitmask)) {
            font.PixelFormat = FontPixelFormat.Paletted;
            font.Glyphs.ForEach(g => g.PixelFormat = FontPixelFormat.Paletted);
        }

        return font;
    }
}
