using System.Text;

namespace ResourceExtractor;

using ResourceExtractor.Extractors;

internal static class Program {
    private const int DosCodePage = 437;

    public static void Main(string[] args) {
        CodePagesEncodingProvider.Instance.GetEncoding(DosCodePage);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string filePath = args.Length == 1
            ? args[0]
            : @"C:\Games\Betrayal at Krondor"; //Directory.GetCurrentDirectory();

        // ExtractResourceArchive(filePath);
        // ExtractFont(Path.Combine(filePath, "book.fnt"));
        // ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));
        // ExtractOvl(Path.Combine(filePath, "SX.OVL"));
        // ExtractAllScx(filePath);
        // ExtractAllBmx(filePath);
        // var colors = ExtractPalette(Path.Combine(filePath, "PUZZLE.PAL"));
        // var screen = ExtractScreen(Path.Combine(filePath, "PUZZLE.SCX"));
        // var image = new BmImage{Data = screen.BitMapData, Width = 320, Height = 200};
        // SaveAsBitmap(image, "PUZZLE.png", colors);
        // MenuExtractor.ExtractToFile(Path.Combine(filePath, "REQ_SAVE.DAT"));
        AnimationExtractor.Extract(Path.Combine(filePath, "CHAPTER1.ADS"));
    }
}