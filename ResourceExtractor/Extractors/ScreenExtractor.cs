namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources;

using System.Text;

public class ScreenExtractor : ExtractorBase {
    public static Screen Extract(string filePath) {
        var screen = new Screen(filePath);
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        if (signature != 0x27B6) {
            if (signature == 0x4D42) {
                resourceFile.Seek(0, SeekOrigin.Begin);
                return ExtractScreenFromTaggedBitmap(resourceReader);
            }
            throw new InvalidOperationException($"Invalid SCX signature '{signature:X4}' in {filePath}");
        }
        screen.BitMapData = DecompressToByteArray(resourceReader);

        return screen;
    }

    private static Screen ExtractScreenFromTaggedBitmap(BinaryReader resourceReader) {
        throw new NotImplementedException();
    }

    private static void ExtractAllScx(string scxFilePath) {
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
            var image = new BmImage {
                Data = screen.BitMapData,
                Width = 320,
                Height = 200
            };
            SaveAsBitmap(image, imagePath);
        }
    }
}