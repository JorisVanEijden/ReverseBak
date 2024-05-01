namespace ResourceExtractor;

using GameData.Resources;
using GameData.Resources.Animation;
using GameData.Resources.Book;
using GameData.Resources.Dialog;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Spells;

using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Extractors.Animation;
using ResourceExtractor.Extractors.Container;
using ResourceExtractor.Extractors.Dialog;

using System.Text;

internal static class Program {
    private const int DosCodePage = 437;

    public static void Main(string[] args) {
        CodePagesEncodingProvider.Instance.GetEncoding(DosCodePage);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string filePath = args.Length == 1
            ? args[0]
            : @"C:\Games\Betrayal at Krondor"; //Directory.GetCurrentDirectory();

        Directory.SetCurrentDirectory(@"C:\Users\JvE\AppData\LocalLow\StellarGames\ModBuilder\overrides");
        
        // ResourceExtractor.Extractors.ArchiveExtractor.ExtractResourceArchive(filePath);
        FontExtractor.Extract(Path.Combine(filePath, "game.fnt"));
        // ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));
        // OvlExtractor.Extract(Path.Combine(filePath, "VMCODE.OVL"));
        ScreenExtractor.ExtractAllScx(filePath);
        BitmapExtractor.ExtractAllBmx(filePath);

        foreach (var paletteFile in GetFiles(filePath, "*.PAL")) {
            var colors2 = PaletteExtractor.Extract(paletteFile);
            // WriteToJsonFile(paletteFile, ResourceType.PAL, colors2.ToJson());
            WriteToCsvFile(paletteFile, ResourceType.PAL, colors2.ToCsv());
        }
        
        
        // var screen = ExtractScreen(Path.Combine(filePath, "PUZZLE.SCX"));
        // var image = new BmImage{Data = screen.BitMapData, Width = 320, Height = 200};
        // SaveAsBitmap(image, "PUZZLE.png", colors);

        var reqFiles = new List<string>();
        reqFiles.AddRange(GetFiles(filePath, "REQ_*.DAT"));
        reqFiles.Add("contents.dat");
        reqFiles.Add("combat.dat");
        reqFiles.Add("shoot.dat");
        reqFiles.Add("spell.dat");
        foreach (string reqFile in reqFiles) {
            var menuData = MenuExtractor.Extract(Path.Combine(filePath, reqFile));
            WriteToJsonFile(reqFile, ResourceType.REQ, menuData.ToJson());
        }

        DdxStatistics statistics = new();
        var ddxExtractor = new DdxExtractor();
        foreach (string ddxFile in GetFiles(filePath, "*.ddx")) {
            Dialog ddx = ddxExtractor.Extract(ddxFile);
            WriteToJsonFile(ddxFile, ddx.Type, ddx.ToJson());
            statistics.Add(ddx);
        }
        Console.WriteLine(statistics.Dump());

        var labelExtractor = new LabelExtractor();
        foreach (string labelFile in GetFiles(filePath, "lbl_*.dat")) {
            LabelSet labelSet = labelExtractor.Extract(labelFile);
            WriteToJsonFile(labelFile, labelSet.Type, labelSet.ToJson());
        }

        var spellExtractor = new SpellExtractor();
        List<Spell> spells = SpellExtractor.ExtractSpells(filePath);
        WriteToJsonFile("spells.dat", ResourceType.DAT, spells.ToJson());
        WriteToCsvFile("spells.dat", ResourceType.DAT, spells.ToCsv());
        
        var objectExtractor = new ObjectExtractor();
        var objectInfo = objectExtractor.Extract(Path.Combine(filePath, "objinfo.dat"));
        WriteToCsvFile("objinfo.dat", ResourceType.DAT, objectInfo.ToCsv());
        
        var keywordExtractor = new KeywordExtractor();
        var keywords = KeywordExtractor.Extract(Path.Combine(filePath, "keyword.dat"));
        WriteToCsvFile("keywords.dat", ResourceType.DAT, string.Join("\r\n", keywords));

        var mNames = MNamesExtractor.Extract(Path.Combine(filePath, "mnames.dat"));
        WriteToCsvFile("mnames.dat", ResourceType.DAT, string.Join("\r\n", mNames));

        var bokExtractor = new BokExtractor();
        foreach (string bokFile in GetFiles(filePath, "*.BOK")) {
            BookResource book = bokExtractor.Extract(bokFile);
            WriteToJsonFile(bokFile, book.Type, book.ToJson());
        }

        foreach (string mapFile in GetFiles(filePath, "Z??MAP.DAT")) {
            var s = FileToBitStream(Path.Combine(filePath, mapFile));
            File.AppendAllText("tempdebug.txt", s);
        }

        var objFixedExtractor = new ObjFixedExtractor();
        string path = "OBJFIXED.DAT";
        List<Container> fixedObjects = objFixedExtractor.Extract(Path.Combine(filePath, path));
        WriteToJsonFile(path, ResourceType.DAT, fixedObjects.ToJson());

        const string teleportDat = "teleport.dat";
        List<TeleportDestination> teleportDestinations = TeleportExtractor.Extract(Path.Combine(filePath, teleportDat));
        WriteToJsonFile(teleportDat, ResourceType.DAT, teleportDestinations.ToJson());
        
        foreach (string adsFile in GetFiles(filePath, "*.ads")) {
            AnimationResource anim = AnimationExtractor.Extract(adsFile);
            WriteToJsonFile(adsFile, anim.Type, anim.ToJson());
        }
        foreach (string ttmFile in GetFiles(filePath, "*.ttm")) {
            // string ttmFile = Path.Combine(filePath, "C21.TTM");    
            var ttm = TtmExtractor.Extract(ttmFile);
            WriteToJsonFile(ttmFile, ttm.Type, ttm.ToJson());
        }
        
    }

    public static string FileToBitStream(string filePath) {
        // Read all bytes from the file
        byte[] fileBytes = File.ReadAllBytes(filePath);
        Array.Reverse(fileBytes);
        // Use a StringBuilder to build the bitstream
        var stringBuilder = new StringBuilder();

        int pos = 0;

        // Iterate over each byte
        foreach (byte b in fileBytes) {
            // Convert the byte to binary and pad it with zeros to ensure it's always 8 bits
            stringBuilder.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            if (++pos % 8 == 0) {
                stringBuilder.AppendLine();
            }
        }

        stringBuilder.AppendLine();

        var binary = stringBuilder.ToString();

        var s = binary.Replace("0", "  ")
            .Replace("1", "##");

        // Return the bitstream as a string
        return s;
    }

    private static IEnumerable<string> GetFiles(string filePath, string searchPattern) {
        return Directory.GetFileSystemEntries(filePath, searchPattern, new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });
    }

    private static void WriteToJsonFile(string fileName, ResourceType resourceType, string json) {
        // string resourceDirectory = resourceType.ToString();
        string resourceDirectory = Path.GetExtension(fileName)[1..].ToUpper();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".json"), json);
    }

    private static void WriteToCsvFile(string fileName, ResourceType resourceType, string csv) {
        // string resourceDirectory = resourceType.ToString();
        string resourceDirectory = Path.GetExtension(fileName)[1..].ToUpper();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".csv"), csv);
    }
}