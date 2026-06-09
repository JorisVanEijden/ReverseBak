namespace ResourceExtractor;

using GameData.Resources.Animation;
using GameData.Resources.Audio;
using GameData.Resources.Book;
using GameData.Resources.Credits;
using GameData.Resources.Data;
using GameData.Resources.Dialog;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Menu;
using GameData.Resources.Object;
using GameData.Resources.Palette;
using GameData.Resources.Spells;
using GameData.Resources.Monster;
using GameData.Resources.World;
using ResourceExtraction;
using ResourceExtraction.Assemblers;
using ResourceExtraction.Extractors;
using ResourceExtraction.Extractors.Animation;
using ResourceExtraction.Extractors.Def;
using ResourceExtraction.Providers;
using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Extractors.Container;
using ResourceExtraction.Extractors.Dialog;
using ResourceExtractor.Imaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using Color = GameData.Resources.Palette.Color;
using PaletteExtractor = ResourceExtraction.Extractors.PaletteExtractor;
using ResourceType = GameData.Resources.ResourceType;

internal static class Program {
    private const int DosCodePage = 437;

    public static void Main(string[] args) {
        // CodePagesEncodingProvider.Instance.GetEncoding(DosCodePage);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string generatedDir = ResolveGeneratedDir();
        Directory.CreateDirectory(generatedDir);
        Directory.SetCurrentDirectory(generatedDir);

        if (args.Length >= 1 && args[0] == "--cutscene-data") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            string outputDir = args.Length >= 3 ? args[2] : "generated";
            ExportCutsceneData(gamePath, outputDir);
            return;
        }

        if (args.Length >= 1 && args[0] == "--ddx") {
            ExportDdxDialogs(args);
            return;
        }

        if (args.Length >= 1 && args[0] == "--objinfo") {
            ExportObjectInfo(args);
            return;
        }

        if (args.Length >= 1 && args[0] == "--world") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractZoneData(gamePath);
            ExtractWorldTiles(gamePath);
            ExtractTileEvents(gamePath);
            ExtractMonsterStats(gamePath);
            ExtractZoneTables(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--spells") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            GeneralResourceProvider provider = new(gamePath);
            ExtractSpells(provider);
            ExtractSpellInfo(provider);
            return;
        }

        if (args.Length >= 1 && args[0] == "--tile-events") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractTileEvents(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--scx") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            GeneralResourceProvider provider = new(gamePath);
            ExtractAllScx(gamePath, provider);
            return;
        }

        if (args.Length >= 1 && args[0] == "--bmx") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            GeneralResourceProvider provider = new(gamePath);
            ExtractAllBmx(gamePath, provider);
            return;
        }

        if (args.Length >= 1 && args[0] == "--cursor") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            GeneralResourceProvider provider = new(gamePath);
            ExtractCursors(gamePath, provider);
            return;
        }

        if (args.Length >= 1 && args[0] == "--images") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            GeneralResourceProvider provider = new(gamePath);
            ExtractAllScx(gamePath, provider);
            ExtractAllBmx(gamePath, provider);
            return;
        }

        if (args.Length >= 1 && args[0] == "--tbl") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractZoneTables(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--sfx") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractAllSounds(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--def") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractDefDat(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--chapsong") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractChapterSongMap(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--req") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractUserInterfaces(gamePath);
            return;
        }

        if (args.Length >= 1 && args[0] == "--cred") {
            string gamePath = args.Length >= 2 ? args[1] : @"D:\BaK\OriginalGame";
            ExtractCredits(gamePath);
            return;
        }

        if (args.Length == 1 && args[0].EndsWith(".GAM", StringComparison.OrdinalIgnoreCase)) {
            ExtractGamFile(args[0]);
            return;
        }

        if (args.Length == 2
            && args[0].EndsWith(".GAM", StringComparison.OrdinalIgnoreCase)
            && args[1].EndsWith(".GAM", StringComparison.OrdinalIgnoreCase)) {
            RunSaveGameFlagDiff(args[0], args[1]);
            return;
        }

        string filePath = args.Length == 1 ? args[0] : @"D:\BaK\OriginalGame"; //Directory.GetCurrentDirectory();

        ExtractBooks(filePath);


        const string saveGamePath = @"D:\BaK\OriginalGame\GAMES\dir.G01\SAVE02.GAM";
        var saveGameExtractor = new SaveGameExtractor();
        using (FileStream saveGameStream = File.OpenRead(saveGamePath)) {
            SaveGame saveGame = saveGameExtractor.Extract(Path.GetFileName(saveGamePath), saveGameStream);
            Console.WriteLine($"Extracted: {saveGame.Id}");
            Console.WriteLine($"Name: {saveGame.SaveGameName}");
            Console.WriteLine($"Header Chapter: {saveGame.ChapterNumber}");
            Console.WriteLine($"Version: {saveGame.Version} (supported: {saveGame.IsSupportedVersion})");
            Console.WriteLine($"Temp.GAM bytes: {saveGame.TempGameData.Length}");
            string saveGameJsonPath = Path.GetFileNameWithoutExtension(saveGamePath) + ".savegame.json";
            File.WriteAllText(saveGameJsonPath, saveGame.ToJson());
            Console.WriteLine($"Dumped JSON: {Path.GetFullPath(saveGameJsonPath)}");
            if (saveGame.Data != null) {
                Console.WriteLine($"State Chapter: {saveGame.Data.StateData.ChapterNumber}");
                Console.WriteLine($"Party Gold: {saveGame.Data.StateData.PartyGold}");
                Console.WriteLine($"Zone: {saveGame.Data.StateData.CurrentZoneNumber} @ ({saveGame.Data.StateData.WorldXCoordinate},{saveGame.Data.StateData.WorldYCoordinate})");
            }
        }

        const string saveGameAfterActionPath = @"D:\BaK\OriginalGame\GAMES\dir.G01\SAVE03.GAM";
        RunSaveGameFlagDiff(saveGamePath, saveGameAfterActionPath);

        return;

        GeneralResourceProvider generalResourceProvider = new(filePath);

        // Extracts all resource from krondor.001 to separate files in the game directory
        // var archiveExtractor = new ResourceExtraction.Extractors.ArchiveExtractor(filePath);
        // archiveExtractor.ExtractAllResources();

        Directory.SetCurrentDirectory(@"C:\Users\JvE\AppData\LocalLow\StellarGameStudio\BaK-Again\overrides");

        // ExtractAllSounds(filePath, archiveExtractor);

        var resourceProvider = ResourceProviderFactory.CreateResourceProvider(filePath);
        var resource = resourceProvider.GetResource<AudioResource>("1023");
        Console.WriteLine(resource.Type);
        Console.WriteLine(resource.Name);

        // OvlExtractor.Extract(filePath, "VMCODE.OVL");
        OvlExtractor.Extract(filePath, "SX.OVL");

        return;
        ExtractAnimations(filePath, generalResourceProvider);
        ExtractAnimatorScripts(filePath, generalResourceProvider);

        // TestAssembly(filePath, "INTRO");

        // ResourceExtractor.Extractors.ArchiveExtractor.ExtractResourceArchive(filePath);
        FontExtractor.Extract(Path.Combine(filePath, "game.fnt"));
        // ExtractScreen(Path.Combine(filePath, "Z01L.SCX"));

        ExtractAllScx(filePath, generalResourceProvider);
        ExtractAllBmx(filePath, generalResourceProvider);

        ExtractAllPalettes(filePath, generalResourceProvider);
        ExtractAllRemappings(filePath, generalResourceProvider);

        // var screen = ExtractScreen(Path.Combine(filePath, "PUZZLE.SCX"));
        // var image = new BmImage{BitMapData = screen.BitMapData, Width = 320, Height = 200};
        // SaveAsBitmap(image, "PUZZLE.png", colors);

        ExtractUserInterfaces(filePath);

        var ddxExtractor = new DdxExtractor();
        foreach (string ddxFile in GetFiles(filePath, "*.ddx")) {
            using FileStream resourceFile = File.OpenRead(Path.Combine(filePath, ddxFile));
            Dialog ddx = ddxExtractor.Extract(ddxFile, resourceFile);
            WriteToJsonFile(ddxFile, ddx.Type, ddx.ToJson());
        }

        ExtractLabels(filePath, generalResourceProvider);
        ExtractSpells(generalResourceProvider);
        ExtractSpellInfo(generalResourceProvider);

        var objectExtractor = new ObjectExtractor();
        List<ObjectInfo> objectInfo = objectExtractor.Extract(Path.Combine(filePath, "objinfo.dat"));
        WriteToCsvFile("objinfo.dat", ResourceType.DAT, objectInfo.ToCsv());

        var keywordExtractor = new KeywordExtractor();
        using Stream resourceStream = generalResourceProvider.GetResourceStream("keyword.dat");
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

    // Dumps every sound from FRP.SX to generated/SND/{id}_{name}/{id}_{format}.{mid|wav}.
    // soundFormat byte → driver: 00=SndBlast/AdLib, 07=GenMIDI, 0C=MT-32, 12=STD (see docs/FileFormats/FRP.SX.md).
    private static void ExtractAllSounds(string gamePath) {
        var provider = new AudioResourceProvider(gamePath);
        IDictionary<string, (long, uint)> dictionary = provider.GetDictionary();
        string root = nameof(ResourceType.SND);

        foreach (KeyValuePair<string, (long, uint)> entry in dictionary) {
            AudioResource audioResource;
            try {
                audioResource = provider.GetResource<AudioResource>(entry.Key);
            } catch (Exception ex) {
                Console.Error.WriteLine($"[SFX] {entry.Key}: skip ({ex.Message})");
                continue;
            }

            string safeName = SanitizeFilename(audioResource.Name);
            string resourceDirectory = Path.Combine(root, $"{audioResource.Id}_{safeName}");
            Directory.CreateDirectory(resourceDirectory);

            foreach (KeyValuePair<byte, AudioDataResource> soundVariant in audioResource.Variants) {
                string variantName = soundVariant.Key.ToString("X2");
                if (soundVariant.Value.MidiData != null) {
                    File.WriteAllBytes(Path.Combine(resourceDirectory, $"{audioResource.Id}_{variantName}.mid"),
                        soundVariant.Value.MidiData);
                }
                if (soundVariant.Value.WavData != null) {
                    File.WriteAllBytes(Path.Combine(resourceDirectory, $"{audioResource.Id}_{variantName}.wav"),
                        soundVariant.Value.WavData);
                }
            }
            Console.WriteLine($"[SFX] {audioResource.Id} {audioResource.Name} ({audioResource.Variants.Count} variants)");
        }
    }

    private static string SanitizeFilename(string name) {
        if (string.IsNullOrEmpty(name)) return "_";
        var sb = new StringBuilder(name.Length);
        foreach (char c in name) {
            sb.Append(Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '_' : c);
        }
        return sb.ToString();
    }

    private static void ExtractAllPalettes(string filePath, GeneralResourceProvider generalResourceProvider) {
        var paletteExtractor = new PaletteExtractor();
        foreach (string paletteFile in GetFiles(filePath, "*.PAL")) {
            using var resourceStream = generalResourceProvider.GetResourceStream(paletteFile);
            var paletteResource = paletteExtractor.Extract(paletteFile, resourceStream);
            WriteToJsonFile(paletteFile, ResourceType.PAL, paletteResource.ToJson());
            // WriteToCsvFile(paletteFile, ResourceType.PAL, paletteResource.Colors.ToCsv());
        }
    }

    private static void ExtractAllRemappings(string filePath, GeneralResourceProvider generalResourceProvider) {
        var remapExtractor = new RemapExtractor();
        foreach (string paletteFile in GetFiles(filePath, "*.RMP")) {
            using var resourceStream = generalResourceProvider.GetResourceStream(paletteFile);
            var remapResource = remapExtractor.Extract(Path.GetFileName(paletteFile), resourceStream);
            WriteToJsonFile(paletteFile, ResourceType.RMP, remapResource.ToJson());
        }
    }

    private static void TestAssembly(string filePath, string name) {
        string destination = Path.Combine(filePath, $"{name}.TTM");
        var mod = JsonSerializer.Deserialize<AnimationResource>(File.ReadAllText($"TTM/{name}.json"));
        TtmAssembler.Assemble(mod ?? throw new InvalidOperationException(), destination);
    }

    private static void ExtractAnimatorScripts(string filePath, GeneralResourceProvider generalResourceProvider) {
        var animatorScriptExtractor = new TtmExtractor();
        foreach (string ttmFile in GetFiles(filePath, "*.ttm")
                 // .Where(f => !f.EndsWith("C51.TTM"))
                ) {
            using Stream resourceStream = generalResourceProvider.GetResourceStream(ttmFile);
            AnimationResource ttm = animatorScriptExtractor.Extract(Path.GetFileName(ttmFile), resourceStream);
            WriteToJsonFile(ttmFile, ttm.Type, ttm.ToJson());
        }
    }

    private static void ExtractAnimations(string filePath, GeneralResourceProvider generalResourceProvider) {
        var animationExtractor = new AdsExtractor();
        foreach (string adsFile in GetFiles(filePath, "*.ads")
                 // .Where(f => f.EndsWith("C12.ADS"))
                ) {
            using Stream resourceStream = generalResourceProvider.GetResourceStream(adsFile);
            AnimatorResource anim = animationExtractor.Extract(Path.GetFileName(adsFile), resourceStream);
            WriteToJsonFile(adsFile, anim.Type, anim.ToJson());
        }
        // foreach (ushort command in AdsScriptBuilder.SeenCommands) {
        //     Console.WriteLine($"{command:X4}");
        // }
    }

    private static void ExtractSpells(GeneralResourceProvider generalResourceProvider) {
        var spellExtractor = new SpellExtractor();
        const string filename = "spells.dat";
        using Stream resourceStream = generalResourceProvider.GetResourceStream(filename);
        SpellList spellList = spellExtractor.Extract(filename, resourceStream);
        WriteToJsonFile(filename, ResourceType.DAT, spellList.ToJson());
        WriteToCsvFile(filename, ResourceType.DAT, spellList.ToCsv());
    }

    private static void ExtractSpellInfo(GeneralResourceProvider generalResourceProvider) {
        var spellInfoExtractor = new SpellInfoExtractor();
        const string filename = "spelldoc.dat";
        using Stream resourceStream = generalResourceProvider.GetResourceStream(filename);
        SpellInfoList spellInfoList = spellInfoExtractor.Extract(filename, resourceStream);
        WriteToJsonFile(filename, ResourceType.DAT, spellInfoList.ToJson());
    }

    private static void ExtractLabels(string filePath, GeneralResourceProvider generalResourceProvider) {
        var labelExtractor = new LabelExtractor();
        foreach (string labelFile in GetFiles(filePath, "lbl_*.dat")) {
            using Stream resourceStream = generalResourceProvider.GetResourceStream(labelFile);
            LabelSet labelSet = labelExtractor.Extract(Path.GetFileName(labelFile), resourceStream);
            WriteToJsonFile(labelFile, labelSet.Type, labelSet.ToJson());
        }
    }

    private static void ExtractAllBmx(string filePath, GeneralResourceProvider generalResourceProvider) {
        string[] bmxFiles = Directory.GetFileSystemEntries(filePath, "*.bmx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        var bitmapExtractor = new BitmapExtractor();
        var paletteExtractor = new PaletteExtractor();
        foreach (string bmxFile in bmxFiles) {
            using Stream resourceStream = generalResourceProvider.GetResourceStream(bmxFile);
            ImageSet imageSet = bitmapExtractor.Extract(Path.GetFileName(bmxFile), resourceStream);
            var imageName = $"{Path.GetFileNameWithoutExtension(bmxFile)}";
            Color[] colors = GetColorsFromPalette(filePath, generalResourceProvider, imageName, paletteExtractor);
            // For multiple images we create a directory and extract each image
            var path = ResourceType.BMX.ToString();
            string resourceDirectory = Path.Combine(path, imageName);
            Directory.CreateDirectory(resourceDirectory);
            for (var i = 0; i < imageSet.Images.Count; i++) {
                BmImage bmImage = imageSet.Images[i];
                bmImage.Filename = $"{i}.png";
                File.WriteAllText(Path.Combine(resourceDirectory, $"{i}.json"), bmImage.ToJson());
                WriteToPngFile(i.ToString(), resourceDirectory, bmImage.ToRawImage(colors));
            }
            Console.WriteLine($"[BMX] {imageName} ({imageSet.Images.Count} images)");
        }
    }

    private static void ExtractCursors(string filePath, GeneralResourceProvider generalResourceProvider) {
        var extractor = new CursorExtractor();
        const string outDir = "POINTER";
        Directory.CreateDirectory(outDir);
        foreach (string name in new[] { "POINTER.BMX", "POINTERG.BMX" }) {
            string full = Path.Combine(filePath, name);
            if (!File.Exists(full)) {
                Console.WriteLine($"[CURSOR] missing {name}");
                continue;
            }
            using Stream resourceStream = generalResourceProvider.GetResourceStream(full);
            GameData.Resources.Cursor.CursorSet set = extractor.Extract(name, resourceStream);
            File.WriteAllText(Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(name)}.json"), set.ToJson());
            Console.WriteLine($"[CURSOR] {name} ({set.Images.Count} images)");
        }
    }

    private static Color[] GetColorsFromPalette(string filePath, GeneralResourceProvider generalResourceProvider, string imageName, PaletteExtractor paletteExtractor) {
        var paletteFile = PaletteExtractor.FindPalette(filePath, imageName);
        using Stream paletteStream = generalResourceProvider.GetResourceStream(paletteFile);
        var colors = paletteExtractor.Extract(paletteFile, paletteStream).Colors;

        return colors;
    }

    private static void ExtractAllScx(string filePath, GeneralResourceProvider generalResourceProvider) {
        string[] scxFiles = Directory.GetFileSystemEntries(filePath, "*.scx", new EnumerationOptions {
            MatchCasing = MatchCasing.CaseInsensitive
        });

        var screenExtractor = new ScreenExtractor();
        var paletteExtractor = new PaletteExtractor();
        foreach (string scxFile in scxFiles) {
            using Stream resourceStream = generalResourceProvider.GetResourceStream(scxFile);
            string imageName = Path.GetFileNameWithoutExtension(scxFile);
            BackgroundImage backgroundImage = screenExtractor.Extract(Path.GetFileName(scxFile), resourceStream);
            if (backgroundImage.BitMapData == null) {
                continue;
            }

            Color[] colors = GetColorsFromPalette(filePath, generalResourceProvider, imageName, paletteExtractor);

            var image = new BmImage(scxFile) {
                BitMapData = backgroundImage.BitMapData,
                Width = backgroundImage.Width,
                Height = backgroundImage.Height
            };

            var bitmap = image.ToRawImage(colors);

            WriteToPngFile(imageName, backgroundImage.Type.ToString(), bitmap);
            Console.WriteLine($"[SCX] {imageName} -> {backgroundImage.Width}x{backgroundImage.Height}");
        }
    }

    private static void WriteToPngFile(string filename, string resourceDirectory, RawImage image) {
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }

        string filePath = Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(filename) + ".png");
        PngWriter.Write(filePath, image);
    }

    private static void ExtractUserInterfaces(string filePath) {
        var userInterfaceExtractor = new UserInterfaceExtractor();
        var reqFiles = new List<string>();
        reqFiles.AddRange(GetFiles(filePath, "REQ_*.DAT").Select(Path.GetFileName)!);
        reqFiles.Add("AROREQ.DAT");
        reqFiles.Add("COMBAT.DAT");
        reqFiles.Add("CONTENTS.DAT");
        reqFiles.Add("EDITREQ.DAT");
        reqFiles.Add("INFOREQ.DAT");
        reqFiles.Add("POWEREQ.DAT");
        reqFiles.Add("SHOOT.DAT");
        reqFiles.Add("SPELL.DAT");
        reqFiles.Add("SPELLREQ.DAT");

        const string reqDir = "REQ";
        Directory.CreateDirectory(reqDir);
        foreach (string reqFile in reqFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)) {
            string fullPath = Path.Combine(filePath, reqFile);
            if (!File.Exists(fullPath)) {
                Console.WriteLine($"[REQ] skip {reqFile}: not found");
                continue;
            }
            using FileStream resourceFile = File.OpenRead(fullPath);
            UserInterface userInterface = userInterfaceExtractor.Extract(reqFile, resourceFile);
            string outPath = Path.Combine(reqDir, Path.GetFileNameWithoutExtension(reqFile) + ".json");
            File.WriteAllText(outPath, userInterface.ToJson());
            Console.WriteLine($"[REQ] {reqFile} -> {outPath} ({userInterface.MenuEntries.Length} entries)");
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

        string s = binary.Replace("0", "  ").Replace("1", "##");

        // Return the bitstream as a string
        return s;
    }

    private static string ResolveGeneratedDir() {
        string? env = Environment.GetEnvironmentVariable("BAK_GENERATED_DIR");
        if (!string.IsNullOrEmpty(env)) return env;

        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            if (Directory.Exists(Path.Combine(dir, "OriginalGame")) &&
                Directory.Exists(Path.Combine(dir, "DotNetProjects"))) {
                return Path.Combine(dir, "generated");
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            "Could not locate workspace root from " + AppContext.BaseDirectory +
            " (no ancestor contains both OriginalGame/ and DotNetProjects/). " +
            "Set BAK_GENERATED_DIR to override.");
    }

    private static string[] GetFiles(string filePath, string searchPattern) {
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

    private static T ReadFromJsonFile<T>(string fileName) {
        string resourceDirectory = Path.GetExtension(fileName)[1..].ToUpper();
        string json = File.ReadAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".json"));
        var obj = JsonSerializer.Deserialize<T>(json);

        if (obj == null) {
            throw new InvalidOperationException($"Failed to deserialize {fileName}");
        }

        return obj;
    }

    private static void WriteToCsvFile(string fileName, ResourceType resourceType, string csv) {
        var resourceDirectory = resourceType.ToString();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".csv"), csv);
    }

    private static void RunSaveGameFlagDiff(string beforeSavePath, string afterSavePath) {
        var saveGameExtractor = new SaveGameExtractor();
        SaveGame before = ExtractSaveGame(beforeSavePath, saveGameExtractor);
        SaveGame after = ExtractSaveGame(afterSavePath, saveGameExtractor);

        if (before.Data == null || after.Data == null) {
            Console.WriteLine("Unable to diff flags: one of the saves did not contain parsed state data.");
            return;
        }

        var globalFlagChanges = GetFlagBitChanges(before.Data.StateData.GlobalFlags, after.Data.StateData.GlobalFlags, 0);
        var globalFlag2Changes = GetFlagBitChanges(before.Data.StateData.GlobalFlags2, after.Data.StateData.GlobalFlags2, null);

        var diffReport = new SaveGameFlagDiffReport(
            Path.GetFileName(beforeSavePath),
            Path.GetFileName(afterSavePath),
            globalFlagChanges,
            globalFlag2Changes
        );

        string diffJson = JsonSerializer.Serialize(diffReport, new JsonSerializerOptions {
            WriteIndented = true
        });

        string diffReportPath = $"{Path.GetFileNameWithoutExtension(beforeSavePath)}_to_{Path.GetFileNameWithoutExtension(afterSavePath)}.flagdiff.json";
        File.WriteAllText(diffReportPath, diffJson);

        Console.WriteLine($"Flag diff written: {Path.GetFullPath(diffReportPath)}");
        Console.WriteLine($"GlobalFlags changed bits: {globalFlagChanges.Count}");
        Console.WriteLine($"GlobalFlags2 changed bits: {globalFlag2Changes.Count}");
    }

    private static void ExtractGamFile(string gamPath) {
        var extractor = new SaveGameExtractor();
        using FileStream stream = File.OpenRead(gamPath);
        SaveGame saveGame = extractor.Extract(Path.GetFileName(gamPath), stream);
        Console.WriteLine($"Extracted: {saveGame.Id}");
        Console.WriteLine($"Name: '{saveGame.SaveGameName}'");
        Console.WriteLine($"Header Chapter: {saveGame.ChapterNumber}");
        Console.WriteLine($"Version: {saveGame.Version} (supported: {saveGame.IsSupportedVersion})");
        Console.WriteLine($"Temp.GAM bytes: {saveGame.TempGameData.Length}");
        string jsonPath = Path.GetFileNameWithoutExtension(gamPath) + ".savegame.json";
        File.WriteAllText(jsonPath, saveGame.ToJson());
        Console.WriteLine($"Dumped JSON: {Path.GetFullPath(jsonPath)}");
        if (saveGame.Data != null) {
            Console.WriteLine($"State Chapter: {saveGame.Data.StateData.ChapterNumber}");
            Console.WriteLine($"Party Gold: {saveGame.Data.StateData.PartyGold}");
            Console.WriteLine($"Zone: {saveGame.Data.StateData.CurrentZoneNumber} @ ({saveGame.Data.StateData.WorldXCoordinate},{saveGame.Data.StateData.WorldYCoordinate})");
            Console.WriteLine($"Active Party Members: {saveGame.Data.StateData.PartyConfigurationData.NumberOfActivePartyCharacters}");
            Console.WriteLine($"Game Time (2s ticks): {saveGame.Data.StateData.GameTimeIn2Seconds}");
            Console.WriteLine($"Timers: {saveGame.Data.StateData.Timers.Count(t => t.Type != 0)}");
            Console.WriteLine($"Zones with containers: {saveGame.Data.ZoneContainerStateData.Zones.Length}");
            int totalContainers = saveGame.Data.ZoneContainerStateData.Zones.Sum(z => z.Containers.Length);
            Console.WriteLine($"Total containers: {totalContainers}");
            int nonEmptyActors = saveGame.Data.ActorStateData.Count(a => a.NamePointer != 0);
            Console.WriteLine($"Non-empty actor slots: {nonEmptyActors}");
            Console.WriteLine($"Actor names: {string.Join(", ", saveGame.Data.StateData.ActorNames.Where(n => !string.IsNullOrEmpty(n)))}");
        } else {
            Console.WriteLine("WARNING: Data section could not be parsed!");
        }
    }

    private static SaveGame ExtractSaveGame(string savePath, SaveGameExtractor extractor) {
        using FileStream saveStream = File.OpenRead(savePath);
        return extractor.Extract(Path.GetFileName(savePath), saveStream);
    }

    private static List<FlagBitChange> GetFlagBitChanges(byte[] before, byte[] after, int? keyBase) {
        int bytesToCompare = Math.Min(before.Length, after.Length);
        var changes = new List<FlagBitChange>();

        for (var byteIndex = 0; byteIndex < bytesToCompare; byteIndex++) {
            int xor = before[byteIndex] ^ after[byteIndex];
            if (xor == 0) {
                continue;
            }

            for (var bitIndex = 0; bitIndex < 8; bitIndex++) {
                if ((xor & (1 << bitIndex)) == 0) {
                    continue;
                }

                bool beforeSet = (before[byteIndex] & (1 << bitIndex)) != 0;
                bool afterSet = (after[byteIndex] & (1 << bitIndex)) != 0;
                int? key = keyBase.HasValue ? keyBase.Value + (byteIndex * 8) + bitIndex : null;

                changes.Add(new FlagBitChange(byteIndex, bitIndex, key, beforeSet, afterSet));
            }
        }

        return changes;
    }

    private sealed record FlagBitChange(int ByteIndex, int BitIndex, int? Key, bool BeforeSet, bool AfterSet);

    private sealed record SaveGameFlagDiffReport(
        string BeforeSave,
        string AfterSave,
        List<FlagBitChange> GlobalFlagsChanges,
        List<FlagBitChange> GlobalFlags2Changes
    );

    private static void ExtractZoneData(string filePath) {
        var defExtractor = new ZoneDefExtractor();
        var mapExtractor = new ZoneMapExtractor();
        var refExtractor = new ZoneRefExtractor();
        var shapeExtractor = new ZoneShapeExtractor();
        var boundsExtractor = new ZoneBoundsExtractor();

        string zoneDatPath = Path.Combine(filePath, "zone.dat");
        if (File.Exists(zoneDatPath)) {
            using var stream = File.OpenRead(zoneDatPath);
            var bounds = boundsExtractor.Extract("zone.dat", stream);
            WriteToJsonFile("zone.dat", ResourceType.DAT, bounds.ToJson());
        }

        foreach (string defFile in GetFiles(filePath, "Z??DEF.DAT")) {
            string fileName = Path.GetFileName(defFile);
            using var stream = File.OpenRead(defFile);
            var def = defExtractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, def.ToJson());
        }

        foreach (string mapFile in GetFiles(filePath, "Z??MAP.DAT")) {
            string fileName = Path.GetFileName(mapFile);
            using var stream = File.OpenRead(mapFile);
            var map = mapExtractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, map.ToJson());
        }

        foreach (string refFile in GetFiles(filePath, "Z??REF.DAT")) {
            string fileName = Path.GetFileName(refFile);
            using var stream = File.OpenRead(refFile);
            var zoneRef = refExtractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, zoneRef.ToJson());
        }

        foreach (string shpFile in GetFiles(filePath, "Z??SHP.DAT")) {
            string fileName = Path.GetFileName(shpFile);
            using var stream = File.OpenRead(shpFile);
            var shape = shapeExtractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, shape.ToJson());
        }
    }

    private static void ExtractWorldTiles(string filePath) {
        var extractor = new WorldItemExtractor();
        foreach (string wldFile in GetFiles(filePath, "T??????.WLD")) {
            string fileName = Path.GetFileName(wldFile);
            using var stream = File.OpenRead(wldFile);
            var tile = extractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.WLD, tile.ToJson());
        }
    }

    private static void ExtractTileEvents(string filePath) {
        var extractor = new TileEventExtractor();
        foreach (string datFile in GetFiles(filePath, "T??????.DAT")) {
            string fileName = Path.GetFileName(datFile);
            using var stream = File.OpenRead(datFile);
            var tile = extractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, tile.ToJson());
        }
    }

    private static void ExtractMonsterStats(string filePath) {
        var extractor = new MonsterStatsExtractor();
        foreach (string monstFile in GetFiles(filePath, "MONST*.DAT")) {
            string fileName = Path.GetFileName(monstFile);
            using var stream = File.OpenRead(monstFile);
            var stats = extractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.DAT, stats.ToJson());
        }
    }

    private static void ExtractZoneTables(string filePath) {
        var extractor = new ZoneTableExtractor();
        foreach (string tblFile in GetFiles(filePath, "*.TBL")) {
            string fileName = Path.GetFileName(tblFile);
            Console.Error.WriteLine($"[TBL] begin {fileName}"); Console.Error.Flush();
            using var stream = File.OpenRead(tblFile);
            var table = extractor.Extract(fileName, stream);
            WriteToJsonFile(fileName, ResourceType.TBL, table.ToJson());
            Console.Error.WriteLine($"[TBL] done  {fileName} ({table.Entries.Count} entries)"); Console.Error.Flush();
        }
    }

    // Extracts CHAPSONG.DAT — see docs/FileFormats/CHAPSONG.DAT.md. 36 bytes:
    // 9 chapters × 2 × i16 song-id, loaded by open_book? at seg020:0x20d21.
    private static void ExtractCredits(string gamePath) {
        string fullPath = Path.Combine(gamePath, "CRED.DAT");
        if (!File.Exists(fullPath)) {
            Console.Error.WriteLine($"[CRED] missing: {fullPath}");
            return;
        }
        using var stream = File.OpenRead(fullPath);
        CreditsData credits = new CredExtractor().Extract("CRED.DAT", stream);
        Directory.CreateDirectory("DAT");
        string json = JsonSerializer.Serialize(credits, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine("DAT", "CRED.json"), json);
        Console.WriteLine($"[CRED] title \"{credits.Title}\", {credits.Lines.Count} lines written to DAT/CRED.json");
    }

    private static void ExtractChapterSongMap(string gamePath) {
        string fullPath = Path.Combine(gamePath, "CHAPSONG.DAT");
        if (!File.Exists(fullPath)) {
            Console.Error.WriteLine($"[CHAPSONG] missing: {fullPath}");
            return;
        }
        using var stream = File.OpenRead(fullPath);
        ChapterSongMap map = new ChapterSongMapExtractor().Extract("CHAPSONG.DAT", stream);
        string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("CHAPSONG.json", json);
        Console.WriteLine($"[CHAPSONG] {map.Entries.Count} chapter entries written to CHAPSONG.json");
    }

    // Extracts the DEF_*.DAT family — see docs/FileFormats/DEF_DAT family.md.
    // Three enum entries (def_comm/def_heal/def_soun) have no shipping data and
    // are skipped. Per-format extractors are added one by one as they're written.
    private static void ExtractDefDat(string gamePath) {
        const string outputDir = "DEF";
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        void Run<TEntry>(string fileName, DefFamilyExtractorBase<TEntry> extractor) {
            string fullPath = Path.Combine(gamePath, fileName);
            if (!File.Exists(fullPath)) {
                Console.Error.WriteLine($"[DEF] missing: {fileName}");
                return;
            }
            using var stream = File.OpenRead(fullPath);
            DefFamilyFile<TEntry> file = extractor.Extract(fileName, stream);
            string json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputDir, Path.GetFileNameWithoutExtension(fileName) + ".json"), json);
            int active = file.Records.Count(r => r.Status == 1);
            Console.WriteLine($"[DEF] {fileName}: {file.Records.Count} records ({active} active)");
        }

        Run("DEF_BKGR.DAT", new DefBkgrExtractor());
        Run("DEF_ENAB.DAT", new DefEnabExtractor());
        Run("DEF_DISA.DAT", new DefDisaExtractor());
        Run("DEF_TOWN.DAT", new DefTownExtractor());
        Run("DEF_BLOC.DAT", new DefBlocExtractor());
        Run("DEF_ZONE.DAT", new DefZoneExtractor());
        Run("DEF_DIAL.DAT", new DefDialExtractor());
        Run("DEF_TRAP.DAT", new DefTrapExtractor());
        Run("DEF_COMB.DAT", new DefCombExtractor());
    }

    private static void ExportObjectInfo(string[] args) {
        string gamePath = args.Length >= 2 ? args[1] : Directory.GetCurrentDirectory();
        string outputDir = args.Length >= 3 ? args[2] : "ObjectInfo";
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string objInfoPath = Directory.GetFileSystemEntries(gamePath, "objinfo.dat",
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).First();
        var extractor = new ObjectExtractor();
        List<ObjectInfo> objects = extractor.Extract(objInfoPath);
        string json = objects.ToJson();
        File.WriteAllText(Path.Combine(outputDir, "objinfo.json"), json);
        Console.WriteLine($"Exported: {objects.Count} objects to objinfo.json");
    }

    private static void ExportCutsceneData(string gamePath, string outputDir) {
        var generalResourceProvider = new GeneralResourceProvider(gamePath);
        var archiveResources = generalResourceProvider.GetDictionary();

        // DDX dialogs
        string ddxDir = Path.Combine(outputDir, "DDX");
        if (!Directory.Exists(ddxDir)) Directory.CreateDirectory(ddxDir);
        var ddxExtractor = new DdxExtractor();
        foreach (string ddxFile in GetFiles(gamePath, "*.ddx")) {
            using FileStream resourceFile = File.OpenRead(ddxFile);
            string fileName = Path.GetFileName(ddxFile);
            Dialog ddx = ddxExtractor.Extract(fileName, resourceFile);
            File.WriteAllText(Path.Combine(ddxDir, Path.GetFileNameWithoutExtension(fileName) + ".json"), ddx.ToJson());
            Console.WriteLine($"Exported DDX: {fileName}");
        }

        // ADS animator scripts — from both loose files and KRONDOR.001 archive
        string adsDir = Path.Combine(outputDir, "ADS");
        if (!Directory.Exists(adsDir)) Directory.CreateDirectory(adsDir);
        var adsExtractor = new AdsExtractor();
        var adsNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string adsFile in GetFiles(gamePath, "*.ads"))
            adsNames.Add(Path.GetFileName(adsFile).ToUpper());
        foreach (string key in archiveResources.Keys)
            if (key.EndsWith(".ADS", StringComparison.OrdinalIgnoreCase))
                adsNames.Add(key);
        foreach (string adsName in adsNames.OrderBy(n => n)) {
            try {
                using Stream resourceStream = generalResourceProvider.GetResourceStream(adsName);
                AnimatorResource anim = adsExtractor.Extract(adsName, resourceStream);
                File.WriteAllText(Path.Combine(adsDir, Path.GetFileNameWithoutExtension(adsName) + ".json"), anim.ToJson());
                Console.WriteLine($"Exported ADS: {adsName}");
            } catch (Exception ex) {
                Console.WriteLine($"FAILED ADS: {adsName} - {ex.Message}");
            }
        }

        // TTM animation resources — from both loose files and KRONDOR.001 archive
        string ttmDir = Path.Combine(outputDir, "TTM");
        if (!Directory.Exists(ttmDir)) Directory.CreateDirectory(ttmDir);
        var ttmExtractor = new TtmExtractor();
        var ttmNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string ttmFile in GetFiles(gamePath, "*.ttm"))
            ttmNames.Add(Path.GetFileName(ttmFile).ToUpper());
        foreach (string key in archiveResources.Keys)
            if (key.EndsWith(".TTM", StringComparison.OrdinalIgnoreCase))
                ttmNames.Add(key);
        foreach (string ttmName in ttmNames.OrderBy(n => n)) {
            try {
                using Stream resourceStream = generalResourceProvider.GetResourceStream(ttmName);
                AnimationResource ttm = ttmExtractor.Extract(ttmName, resourceStream);
                File.WriteAllText(Path.Combine(ttmDir, Path.GetFileNameWithoutExtension(ttmName) + ".json"), ttm.ToJson());
                Console.WriteLine($"Exported TTM: {ttmName}");
            } catch (Exception ex) {
                Console.WriteLine($"FAILED TTM: {ttmName} - {ex.Message}");
            }
        }
    }

    private static void ExportDdxDialogs(string[] args) {
        string gamePath = args.Length >= 2 ? args[1] : Directory.GetCurrentDirectory();
        string outputDir = args.Length >= 3 ? args[2] : "DDX";
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        var extractor = new DdxExtractor();
        foreach (string ddxFile in Directory.GetFileSystemEntries(gamePath, "*.ddx",
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })) {
            using FileStream resourceFile = File.OpenRead(ddxFile);
            Dialog ddx = extractor.Extract(Path.GetFileName(ddxFile), resourceFile);
            string json = ddx.ToJson();
            File.WriteAllText(Path.Combine(outputDir, Path.GetFileNameWithoutExtension(ddxFile) + ".json"), json);
            Console.WriteLine($"Exported: {Path.GetFileName(ddxFile)}");
        }
    }
}