namespace ResourceExtractor.Extractors;

using System.Drawing;
using System.Text;

public class PaletteExtractor : ExtractorBase {
    public static Color[] Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        string mainTag = ReadTag(resourceReader);
        uint fileSize = resourceReader.ReadUInt16();
        ushort unknown = resourceReader.ReadUInt16();
        var palette = new Color[256];
        while (resourceReader.BaseStream.Position < fileSize) {
            string tag = ReadTag(resourceReader);
            uint chunkSize = resourceReader.ReadUInt32();
            if (tag != "VGA") {
                resourceReader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                continue;
            }
            for (int i = 0; i < 256; i++) {
                byte r = resourceReader.ReadByte();
                byte g = resourceReader.ReadByte();
                byte b = resourceReader.ReadByte();
                int red = r << 2 | r >> 4;
                int green = g << 2 | g >> 4;
                int blue = b << 2 | b >> 4;
                palette[i] = Color.FromArgb(red, green, blue);
            }
        }
        return palette;
    }

    /// <summary>
    /// Quick and dirty way of getting a palette file form a filename.
    /// Works only in some cases, but it's nice to see some colors.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="imageName"></param>
    /// <returns></returns>
    public static Color[]? FindPalette(string filePath, string imageName) {
        while (imageName.Length > 1) {
            string palettePath = Path.Combine(filePath, $"{imageName}.pal");
            if (File.Exists(palettePath)) {
                return Extract(palettePath);
            }
            imageName = imageName[..^1];
        }
        return Extract( Path.Combine(filePath, "options.pal")); // use options palette as default cause it goes good with bicons
    }
}