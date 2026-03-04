namespace ResourceExtractor;

using GameData.Resources.Animation;
using GameData.Resources.Audio;
using GameData.Resources.Book;
using GameData.Resources.Data;
using GameData.Resources.Dialog;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Location;
using GameData.Resources.Menu;
using GameData.Resources.Object;
using GameData.Resources.Palette;
using GameData.Resources.Spells;
using ResourceExtraction;
using ResourceExtraction.Assemblers;
using ResourceExtraction.Extractors;
using ResourceExtraction.Extractors.Animation;
using ResourceExtraction.Providers;
using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Extractors.Container;
using ResourceExtractor.Extractors.Dialog;
using System.Drawing;
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

        if (args.Length >= 1 && args[0] == "--ddx") {
            ExportDdxDialogs(args);
            return;
        }

        if (args.Length >= 1 && args[0] == "--objinfo") {
            ExportObjectInfo(args);
            return;
        }

        if (args.Length == 2
            && args[0].EndsWith(".GAM", StringComparison.OrdinalIgnoreCase)
            && args[1].EndsWith(".GAM", StringComparison.OrdinalIgnoreCase)) {
            RunSaveGameFlagDiff(args[0], args[1]);
            return;
        }

        string filePath = args.Length == 1 ? args[0] : @"D:\BaK\OriginalGame"; //Directory.GetCurrentDirectory();

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

    // private static void ExtractAllSounds(string filePath, ArchiveExtractor archiveExtractor) {
    //     var soundExtractor = new SoundExtractor();
    //     string sfxFile = Path.Join(filePath, "FRP.SX");
    //     using var resourceStream = archiveExtractor.GetResourceStream(sfxFile);
    //     var audioAssetList = soundExtractor.ExtractAll("frp.sx", resourceStream);
    //     foreach (var audioResource in audioAssetList.AudioResources) {
    //         var path = nameof(ResourceType.SND);
    //         string resourceDirectory = Path.Combine(path, audioResource.Id + "_" + audioResource.Name);
    //         foreach (KeyValuePair<byte, AudioDataResource> soundVariant in audioResource.Variants) {
    //             var variantName = soundVariant.Key.ToString("X2");
    //             if (!Directory.Exists(resourceDirectory)) {
    //                 Directory.CreateDirectory(resourceDirectory);
    //             }
    //             if (soundVariant.Value.MidiData != null) {
    //                 string destPath = Path.Combine(resourceDirectory, $"{audioResource.Id}_{variantName}.mid");
    //                 File.WriteAllBytes(destPath, soundVariant.Value.MidiData);
    //             }
    //             if (soundVariant.Value.WavData != null) {
    //                 string destPath = Path.Combine(resourceDirectory, $"{audioResource.Id}_{variantName}.wav");
    //                 File.WriteAllBytes(destPath, soundVariant.Value.WavData);
    //             }
    //         }
    //     }
    // }

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
            colors = ApplyRemapping("Z12.RMP", colors, 2);
            // For multiple images we create a directory and extract each image
            var path = ResourceType.BMX.ToString();
            string resourceDirectory = Path.Combine(path, imageName);
            for (var i = 0; i < imageSet.Images.Count; i++) {
                BmImage bmImage = imageSet.Images[i];
                bmImage.Filename = $"{i}.png";
                File.WriteAllText(Path.Combine(resourceDirectory, $"{i}.json"), bmImage.ToJson());
                WriteToPngFile(i.ToString(), resourceDirectory, bmImage.ToBitmap(colors));
            }
        }
    }

    private static Color[] ApplyRemapping(string name, Color[] colors, int id = 0) {
        var remap = ReadFromJsonFile<RemapResource>(name);
        var remappedColors = new Color[colors.Length];

        if (!remap.Mappings.TryGetValue(id, out Dictionary<byte, byte>? mapping))
            return colors;

        for (var i = 0; i < colors.Length; i++) {
            if (mapping.TryGetValue((byte)i, out byte index)) {
                remappedColors[i] = colors[index];
            } else {
                remappedColors[i] = colors[i];
            }
        }

        return remappedColors;
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

            var bitmap = image.ToBitmap(colors);

            WriteToPngFile(imageName, backgroundImage.Type.ToString(), bitmap);
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

        string s = binary.Replace("0", "  ").Replace("1", "##");

        // Return the bitstream as a string
        return s;
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