namespace ResourceExtraction.Extractors;

using GameData;
using GameData.Resources.Palette;
using System.IO;
using System.Text;

public class PaletteExtractor : ExtractorBase<PaletteResource> {
    public override PaletteResource Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        string mainTag = ReadTag(resourceReader);
        uint fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        var palette = new PaletteResource(id);
        while (resourceReader.BaseStream.Position < fileSize) {
            string tag = ReadTag(resourceReader);
            uint chunkSize = resourceReader.ReadUInt32();
            if (tag != "VGA") {
                resourceReader.BaseStream.Seek(chunkSize, SeekOrigin.Current);

                continue;
            }
            for (var i = 0; i < 256; i++) {
                byte r = resourceReader.ReadByte();
                byte g = resourceReader.ReadByte();
                byte b = resourceReader.ReadByte();
                int red = r << 2 | r >> 4;
                int green = g << 2 | g >> 4;
                int blue = b << 2 | b >> 4;
                palette.Colors[i] = Color.FromRgb(red, green, blue);
            }
        }

        return palette;
    }

    /// <summary>
    /// Quick and dirty way of getting a palette file from a filename.
    /// Works only in some cases, but it's nice to see some colors.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="imageName"></param>
    /// <returns></returns>
    public static string FindPalette(string filePath, string imageName) {
        if (PaletteMapping.GetPaletteFor(imageName) is { } paletteName) {
            return Path.Combine(filePath, paletteName);
        }

        while (imageName.Length > 1) {
            string palettePath = Path.Combine(filePath, $"{imageName}.pal");
            if (File.Exists(palettePath)) {
                return palettePath;
            }
            imageName = imageName[..^1];
        }

        return Path.Combine(filePath, "options.pal"); // use options palette as default cause it goes good with bicons
    }
}