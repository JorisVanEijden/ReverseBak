namespace ResourceExtractor;

using GameData.Resources;
using GameData.Resources.Animation;
using GameData.Resources.Book;
using GameData.Resources.Data;
using GameData.Resources.Dialog;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Menu;
using GameData.Resources.Object;
using GameData.Resources.Spells;
using ResourceExtraction.Assemblers;
using ResourceExtraction.Extractors;
using ResourceExtraction.Extractors.Animation;
using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Extractors.Container;
using ResourceExtractor.Extractors.Dialog;
using System.Drawing;
using System.Text;
using System.Text.Json;
using ArchiveExtractor = ResourceExtraction.Extractors.ArchiveExtractor;

internal static class Program {
    private const int DosCodePage = 437;

    public static void Main(string[] args) {
        CodePagesEncodingProvider.Instance.GetEncoding(DosCodePage);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string filePath = args.Length == 1
            ? args[0]
            : @"C:\Games\Betrayal at Krondor"; //Directory.GetCurrentDirectory();

        ArchiveExtractor archiveExtractor = new(filePath);

        Directory.SetCurrentDirectory(@"C:\Users\JvE\AppData\LocalLow\StellarGameStudio\BaK-Again\overrides");

        ExtractAnimations(filePath, archiveExtractor);

        ExtractAnimatorScripts(filePath, archiveExtractor);

        TestAssembly(filePath, "C51");

        return;

        // ResourceExtractor.Extractors.ArchiveExtractor.ExtractResourceArchive(filePath);
        FontExtractor.Extract(Path.Combine(filePath, "game.fnt"));
        // ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));
        // OvlExtractor.Extract(Path.Combine(filePath, "VMCODE.OVL"));

        ExtractAllScx(filePath, archiveExtractor);
        ExtractAllBmx(filePath, archiveExtractor);

        foreach (string paletteFile in GetFiles(filePath, "*.PAL")) {
            Color[] colors2 = PaletteExtractor.Extract(paletteFile);
            // WriteToJsonFile(paletteFile, ResourceType.PAL, colors2.ToJson());
            WriteToCsvFile(paletteFile, ResourceType.PAL, colors2.ToCsv());
        }

        // var screen = ExtractScreen(Path.Combine(filePath, "PUZZLE.SCX"));
        // var image = new BmImage{Data = screen.BitMapData, Width = 320, Height = 200};
        // SaveAsBitmap(image, "PUZZLE.png", colors);

        ExtractUserInterfaces(filePath);

        var ddxExtractor = new DdxExtractor();
        foreach (string ddxFile in GetFiles(filePath, "*.ddx")) {
            using FileStream resourceFile = File.OpenRead(Path.Combine(filePath, ddxFile));
            Dialog ddx = ddxExtractor.Extract(ddxFile, resourceFile);
            WriteToJsonFile(ddxFile, ddx.Type, ddx.ToJson());
        }

        ExtractLabels(filePath, archiveExtractor);
        ExtractSpells(archiveExtractor);
        ExtractSpellInfo(archiveExtractor);

        var objectExtractor = new ObjectExtractor();
        List<ObjectInfo> objectInfo = objectExtractor.Extract(Path.Combine(filePath, "objinfo.dat"));
        WriteToCsvFile("objinfo.dat", ResourceType.DAT, objectInfo.ToCsv());

        var keywordExtractor = new KeywordExtractor();
        using Stream resourceStream = archiveExtractor.GetResourceStream("keyword.dat");
        KeywordList keywordList = keywordExtractor.Extract("globalKeywords", resourceStream);
        WriteToJsonFile("keywords.dat", keywordList.Type, keywordList.ToJson());

        IEnumerable<string> mNames = MNamesExtractor.Extract(Path.Combine(filePath, "mnames.dat"));
        WriteToCsvFile("mnames.dat", ResourceType.DAT, string.Join("\r\n", mNames));

        ExtractBooks(filePath);

        foreach (string mapFile in GetFiles(filePath, "Z??MAP.DAT")) {
            string s = FileToBitStream(Path.Combine(filePath, mapFile));
            File.AppendAllText("tempdebug.txt", s);
        }

        var objFixedExtractor = new ObjFixedExtractor();
        string path = "OBJFIXED.DAT";
        List<Container> fixedObjects = objFixedExtractor.Extract(Path.Combine(filePath, path));
        WriteToJsonFile(path, ResourceType.DAT, fixedObjects.ToJson());

        const string teleportDat = "teleport.dat";
        List<TeleportDestination> teleportDestinations = TeleportExtractor.Extract(Path.Combine(filePath, teleportDat));
        WriteToJsonFile(teleportDat, ResourceType.DAT, teleportDestinations.ToJson());
    }

    private static void TestAssembly(string filePath, string name) {
        string destination = Path.Combine(filePath, $"{name}.TTM");
        var mod = JsonSerializer.Deserialize<AnimatorScene>(File.ReadAllText($"TTM/{name}.json"));
        TtmAssembler.Assemble(mod ?? throw new InvalidOperationException(), destination);
    }

    private static void ExtractAnimatorScripts(string filePath, ArchiveExtractor archiveExtractor) {
        var animatorScriptExtractor = new TtmExtractor();
        foreach (string ttmFile in GetFiles(filePath, "*.ttm")
                     // .Where(f => !f.EndsWith("C51.TTM"))
                ) {
            using Stream resourceStream = archiveExtractor.GetResourceStream(ttmFile);
            AnimatorScene ttm = animatorScriptExtractor.Extract(Path.GetFileName(ttmFile), resourceStream);
            WriteToJsonFile(ttmFile, ttm.Type, ttm.ToJson());
        }
    }

    private static void ExtractAnimations(string filePath, ArchiveExtractor archiveExtractor) {
        var animationExtractor = new AnimationExtractor();
        foreach (string adsFile in GetFiles(filePath, "*.ads")) {
            using Stream resourceStream = archiveExtractor.GetResourceStream(adsFile);
            AnimationResource anim = animationExtractor.Extract(Path.GetFileName(adsFile), resourceStream);
            WriteToJsonFile(adsFile, anim.Type, anim.ToJson());
        }
        foreach (ushort command in AdsScriptBuilder.SeenCommands) {
            Console.WriteLine($"{command:X4}");
        }
    }

    private static void ExtractSpells(ArchiveExtractor archiveExtractor) {
        var spellExtractor = new SpellExtractor();
        const string filename = "spells.dat";
        using Stream resourceStream = archiveExtractor.GetResourceStream(filename);
        SpellList spellList = spellExtractor.Extract(filename, resourceStream);
        WriteToJsonFile(filename, ResourceType.DAT, spellList.ToJson());
        WriteToCsvFile(filename, ResourceType.DAT, spellList.ToCsv());
    }

    private static void ExtractSpellInfo(ArchiveExtractor archiveExtractor) {
        var spellInfoExtractor = new SpellInfoExtractor();
        const string filename = "spelldoc.dat";
        using Stream resourceStream = archiveExtractor.GetResourceStream(filename);
        SpellInfoList spellInfoList = spellInfoExtractor.Extract(filename, resourceStream);
        WriteToJsonFile(filename, ResourceType.DAT, spellInfoList.ToJson());
    }

    private static void ExtractLabels(string filePath, ArchiveExtractor archiveExtractor) {
        var labelExtractor = new LabelExtractor();
        foreach (string labelFile in GetFiles(filePath, "lbl_*.dat")) {
            using Stream resourceStream = archiveExtractor.GetResourceStream(labelFile);
            LabelSet labelSet = labelExtractor.Extract(Path.GetFileName(labelFile), resourceStream);
            WriteToJsonFile(labelFile, labelSet.Type, labelSet.ToJson());
        }
    }

    private static void ExtractAllBmx(string filePath, ArchiveExtractor archiveExtractor) {
        string[] bmxFiles = Directory.GetFileSystemEntries(filePath, "*.bmx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        var bitmapExtractor = new BitmapExtractor();
        foreach (string bmxFile in bmxFiles) {
            using Stream resourceStream = archiveExtractor.GetResourceStream(bmxFile);
            ImageSet imageSet = bitmapExtractor.Extract(Path.GetFileName(bmxFile), resourceStream);
            var imageName = $"{Path.GetFileNameWithoutExtension(bmxFile)}";
            Color[]? colors = PaletteExtractor.FindPalette(filePath, imageName);
            // If it's just a single image we extract it directly
            if (imageSet.Images.Count == 1) {
                WriteToPngFile(bmxFile, ResourceType.BMX.ToString(), imageSet.Images[0].ToBitmap(colors));

                continue;
            }
            // For multiple images we create a directory and extract each image
            var path = ResourceType.BMX.ToString();
            string resourceDirectory = Path.Combine(path, imageName);
            for (var i = 0; i < imageSet.Images.Count; i++) {
                BmImage bmImage = imageSet.Images[i];
                WriteToPngFile(i.ToString(), resourceDirectory, bmImage.ToBitmap(colors));
            }
        }
    }

    private static void ExtractAllScx(string filePath, ArchiveExtractor archiveExtractor) {
        string[] scxFiles = Directory.GetFileSystemEntries(filePath, "*.scx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        var screenExtractor = new ScreenExtractor();

        foreach (string scxFile in scxFiles) {
            using Stream resourceStream = archiveExtractor.GetResourceStream(scxFile);
            string imageName = Path.GetFileNameWithoutExtension(scxFile);
            Screen screen = screenExtractor.Extract(Path.GetFileName(scxFile), resourceStream);
            if (screen.BitMapData == null) {
                continue;
            }

            Color[]? palette = PaletteExtractor.FindPalette(filePath, imageName);

            var image = new BmImage(scxFile) {
                Data = screen.BitMapData,
                Width = screen.Width,
                Height = screen.Height
            };

            var bitmap = image.ToBitmap(palette);

            WriteToPngFile(imageName, screen.Type.ToString(), bitmap);
        }
    }

    private static void WriteToPngFile(string filename, string resourceDirectory, Image bitmap) {
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }

        string filePath = Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(filename) + ".png");
        bitmap.Save(filePath);
    }

    private static void ExtractUserInterfaces(string filePath) {
        var userInterfaceExtractor = new UserInterfaceExtractor();
        var reqFiles = new List<string>();
        reqFiles.AddRange(GetFiles(filePath, "REQ_*.DAT"));
        reqFiles.Add("contents.dat");
        reqFiles.Add("combat.dat");
        reqFiles.Add("shoot.dat");
        reqFiles.Add("spell.dat");
        reqFiles.Add("spellreq.dat");
        foreach (string reqFile in reqFiles) {
            using FileStream resourceFile = File.OpenRead(Path.Combine(filePath, reqFile));
            UserInterface userInterface = userInterfaceExtractor.Extract(Path.GetFileName(reqFile), resourceFile);
            WriteToJsonFile(reqFile, ResourceType.REQ, userInterface.ToJson());
        }
    }

    private static void ExtractBooks(string filePath) {
        var bokExtractor = new BokExtractor();
        foreach (string bokFile in GetFiles(filePath, "*.BOK")) {
            using FileStream resourceFile = File.OpenRead(bokFile);
            BookResource book = bokExtractor.Extract(Path.GetFileName(bokFile), resourceFile);
            WriteToJsonFile(bokFile, book.Type, book.ToJson());
        }
    }

    public static string DictionaryToCsv(Dictionary<int, string> dictionary) {
        var writer = new StringBuilder();
        writer.AppendLine("id,value");
        foreach (KeyValuePair<int, string> pair in dictionary) {
            writer.AppendLine($"{pair.Key},{pair.Value}");
        }

        return writer.ToString();
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

        string s = binary.Replace("0", "  ")
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
        var resourceDirectory = resourceType.ToString();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".csv"), csv);
    }
}