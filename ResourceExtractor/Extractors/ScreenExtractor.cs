namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Image;

using System.Drawing;
using System.Text;

public class ScreenExtractor : ExtractorBase {
    public static Screen Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        if (signature == 0x27B6) {
            return new Screen(filePath) {
                BitMapData = DecompressToByteArray(resourceReader),
                HiRes = false
            };
        }
        resourceFile.Seek(0, SeekOrigin.Begin);
        return new Screen(filePath) {
            BitMapData = DecompressToByteArray(resourceReader),
            HiRes = true
        };
    }

    public static void ExtractAllScx(string scxFilePath) {
        const string resourceDirectory = "SCX";
        string[] scxFiles = Directory.GetFileSystemEntries(scxFilePath, "*.scx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        foreach (string scxFile in scxFiles) {
            Screen screen = Extract(Path.Combine(scxFilePath, scxFile));
            if (screen.BitMapData == null) {
                continue;
            }
            string imageName = $"{Path.GetFileNameWithoutExtension(scxFile)}";
            string imagePath = Path.Combine(resourceDirectory, $"{imageName}.png");

            // Hi-res screens use 4 bits per pixel, we need to convert them to 8 bits per pixel.
            if (screen.HiRes) {
                byte[] tempArray = new byte[screen.BitMapData.Length * 2];
                int j = 0;
                foreach (byte colors in screen.BitMapData) {
                    tempArray[j++] = (byte)(colors >> 4);
                    tempArray[j++] = (byte)(colors & 0x0F);
                }
                screen.BitMapData = tempArray;
            }

            var image = new BmImage {
                Data = screen.BitMapData,
                Width = screen.HiRes ? 640 : 320,
                Height = screen.HiRes ? 350 : 200
            };

            Color[]? palette = PaletteExtractor.FindPalette(scxFilePath, imageName);

            SaveAsBitmap(image, imagePath, palette);
        }
    }
}