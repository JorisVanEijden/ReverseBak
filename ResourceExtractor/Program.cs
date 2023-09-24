namespace ResourceExtractor;

using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Resources;

using System.Net.NetworkInformation;
using System.Text;

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
        // foreach (string adsFile in GetFiles(filePath, "*.ads")) {
        //     AnimationResource anim = AnimationExtractor.Extract(adsFile);
        //     WriteToJsonFile(adsFile, anim.Type, anim.ToJson());
        // }
        // foreach (string ttmFile in GetFiles(filePath, "*.ttm")) {
        //     // string ttmFile = Path.Combine(filePath, "C21.TTM");    
        //     var ttm = TtmExtractor.Extract(ttmFile);
        //     WriteToJsonFile(ttmFile, ttm.Type, ttm.ToJson());
        // }
        DdxStatistics statistics = new();
        var ddxExtractor = new DdxExtractor();
        foreach (string ddxFile in GetFiles(filePath, "*.ddx")) {
            // string ddxFile = Path.Combine(filePath, "DIAL_Z19.DDX");    
            var ddx = ddxExtractor.Extract(ddxFile);
            WriteToJsonFile(ddxFile, ddx.Type, ddx.ToJson());
            statistics.Add(ddx);
        }
        Console.WriteLine(statistics.Dump());
        
    }

    private static IEnumerable<string> GetFiles(string filePath, string searchPattern) {
        return Directory.GetFileSystemEntries(filePath, searchPattern, new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });
    }

    private static void WriteToJsonFile(string fileName, ResourceType resourceType, string json) {
        string resourceDirectory = resourceType.ToString();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".json"), json);
    }
}

internal class DdxStatistics {
    private List<DialogDataItem> _dataItems = new();

    public void Add(Dialog ddx) {
        foreach (var entry in ddx.Entries.Values) {
            if (entry.DialogEntry_Field3 != 16) continue;
            foreach (DialogDataItem dataItem in entry.DataItems) {
                dataItem.FileName = ddx.Name;
                _dataItems.Add(dataItem);
            }
        }
    }

    public string? Dump() {
        StringBuilder sb = new StringBuilder();
        foreach (var dataItem in _dataItems) {
            sb.AppendLine($"type{dataItem.DataItem_Field0:D2},{dataItem.DataItem_Field2:D5},{dataItem.DataItem_Field4:D5},{dataItem.DataItem_Field6:D5},{dataItem.DataItem_Field8:D5},{dataItem.FileName}");
        }
        var dump = sb.ToString();
        File.WriteAllText("dataitemdump.csv", dump);
        return dump;
    }
}