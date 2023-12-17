using ResourceExtractor.Extractors;

using System.Text;

namespace ResourceExtractor;

using ResourceExtractor.Extensions;
using ResourceExtractor.Extractors;
using ResourceExtractor.Resources;
using ResourceExtractor.Resources.Dialog;
using ResourceExtractor.Resources.Label;
using ResourceExtractor.Resources.Spells;

using System.Runtime.InteropServices.JavaScript;
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
        // MenuExtractor.Extract(Path.Combine(filePath, "REQ_SAVE.DAT"));
        // foreach (string adsFile in GetFiles(filePath, "*.ads")) {
        //     AnimationResource anim = AnimationExtractor.Extract(adsFile);
        //     WriteToJsonFile(adsFile, anim.Type, anim.ToJson());
        // }
        // foreach (string ttmFile in GetFiles(filePath, "*.ttm")) {
        //     // string ttmFile = Path.Combine(filePath, "C21.TTM");    
        //     var ttm = TtmExtractor.Extract(ttmFile);
        //     WriteToJsonFile(ttmFile, ttm.Type, ttm.ToJson());
        // }
        // DdxStatistics statistics = new();
        // var ddxExtractor = new DdxExtractor();
        // foreach (string ddxFile in GetFiles(filePath, "*.ddx")) {
        //     // string ddxFile = Path.Combine(filePath, "DIAL_Z19.DDX");    
        //     var ddx = ddxExtractor.Extract(ddxFile);
        //     WriteToJsonFile(ddxFile, ddx.Type, ddx.ToJson());
        //     statistics.Add(ddx);
        // }
        // Console.WriteLine(statistics.Dump());

        // var labelExtractor = new LabelExtractor();
        // foreach (string labelFile in GetFiles(filePath, "lbl_*.dat")) {
        //     LabelSet labelSet = labelExtractor.Extract(labelFile);
        //     WriteToJsonFile(labelFile, labelSet.Type, labelSet.ToJson());
        // }

        // var spellExtractor = new SpellExtractor();
        // List<Spell> spells = spellExtractor.ExtractSpells(filePath);
        // WriteToJsonFile("spells", ResourceType.DAT, spells.ToJson());

        var ObjectExtractor = new ObjectExtractor();

        var objectInfo = ObjectExtractor.Extract(Path.Combine(filePath, "objinfo.dat"));
        WriteToCsvFile("objinfo", ResourceType.DAT, objectInfo.ToCsv());
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

    private static void WriteToCsvFile(string fileName, ResourceType resourceType, string csv) {
        string resourceDirectory = resourceType.ToString();
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(fileName) + ".csv"), csv);
    }
}

internal class ObjectExtractor : ExtractorBase {
    public List<ObjectInfo> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var objectInfoList = new List<ObjectInfo>();
        for (int i = 0; i < 138; i++) {
            var objectInfo = new ObjectInfo {
                Number = i,
                Name = new string(resourceReader.ReadChars(30)).Replace('\0', ' ').Trim(),
                Field1E = resourceReader.ReadUInt16(),
                Field20 = resourceReader.ReadUInt16(),
                Field22 = resourceReader.ReadUInt16(),
                Field24 = resourceReader.ReadUInt16(),
                Price = resourceReader.ReadUInt16(),
                SwingBaseDamage = resourceReader.ReadInt16(),
                ThrustBaseDamage = resourceReader.ReadInt16(),
                SwingAccuracy_ArmorMod_BowAccuracy = resourceReader.ReadInt16(),
                ThrustAccuracy = resourceReader.ReadInt16(),
                Icon = resourceReader.ReadUInt16(),
                InventorySlots = resourceReader.ReadUInt16(),
                Field34 = resourceReader.ReadUInt16(),
                Field36 = resourceReader.ReadByte(),
                Field37 = resourceReader.ReadByte(),
                Race = (Race)resourceReader.ReadUInt16(),
                Field3A = resourceReader.ReadUInt16(),
                ObjectType = (ObjectType)resourceReader.ReadUInt16(),
                Field3E = resourceReader.ReadUInt16(),
                Field40 = resourceReader.ReadUInt16(),
                Field42 = resourceReader.ReadUInt16(),
                Field44 = resourceReader.ReadUInt16(),
                CanEffect = resourceReader.ReadInt16(),
                Field48 = resourceReader.ReadUInt16(),
                Field4A = resourceReader.ReadUInt16(),
                Field4C = resourceReader.ReadUInt16(),
                Field4E = resourceReader.ReadUInt16()
            };

            objectInfoList.Add(objectInfo);
        }

        return objectInfoList;
    }
}

public class ObjectInfo : IResource {
    public ResourceType Type { get => ResourceType.DAT; }
    public string Name { get; set; }
    public int Field1E { get; set; }
    public int Field20 { get; set; }
    public int Field22 { get; set; }
    public int Field24 { get; set; }
    public int Price { get; set; }
    public int SwingBaseDamage { get; set; }
    public int ThrustBaseDamage { get; set; }
    public int SwingAccuracy_ArmorMod_BowAccuracy { get; set; }
    public int ThrustAccuracy { get; set; }
    public int Icon { get; set; }
    public int InventorySlots { get; set; }
    public int Field34 { get; set; }
    public int Field36 { get; set; }
    public Race Race { get; set; }
    public int Field3A { get; set; }
    public ObjectType ObjectType { get; set; }
    public int Field3E { get; set; }
    public int Field40 { get; set; }
    public int Field42 { get; set; }
    public int Field44 { get; set; }
    public int CanEffect { get; set; }
    public int Field48 { get; set; }
    public int Field4A { get; set; }
    public int Field4C { get; set; }
    public int Field4E { get; set; }
    public int Number { get; set; }
    public int Field37 { get; set; }

    public string ToCsv() {
        return $"{Number},{Name},{Field1E},{Field20:X4},{Field22},{Field24},{Price},{SwingBaseDamage},{ThrustBaseDamage},{SwingAccuracy_ArmorMod_BowAccuracy},{ThrustAccuracy},{Icon},{InventorySlots},{Field34},{Field36},{Field37},{Race},{Field3A:X4},{ObjectType},{Field3E:X4},{Field40},{Field42},{Field44},{CanEffect:X4},{Field48:X4},{Field4A},{Field4C},{Field4E}";
    }
}

public enum ObjectType {
    Misc = 0,
    Sword = 1,
    Crossbow = 2,
    Staff = 3,
    Armor = 4,
    Key = 7,
    Repair = 8,
    Poison = 9,
    Enhancer = 10,
    ClericalEnhancer = 11,
    BowString = 12,
    MagicalScroll = 13,
    Note = 16,
    Book = 17,
    Potion = 18,
    Restorative = 19,
    MassRestorative = 20,
    LightSource = 21,
    CombatItem = 22,
    Food = 23,
    Drink = 24,
    Usable = 25
}

public enum Race {
    None,
    Tsurani,
    Elf,
    Human,
    Dwarf
}

internal class DdxStatistics {
    private readonly List<DialogDataItem> _dataItems = new();

    public void Add(Dialog ddx) {
        foreach (DialogEntry entry in ddx.Entries.Values) {
            if (entry.DialogEntry_Field3 != 16) continue;
            foreach (DialogDataItem dataItem in entry.DataItems) {
                dataItem.FileName = ddx.Name;
                _dataItems.Add(dataItem);
            }
        }
    }

    public string? Dump() {
        var sb = new StringBuilder();
        foreach (DialogDataItem dataItem in _dataItems) {
            sb.AppendLine($"type{dataItem.DataItem_Field0:D2},{dataItem.DataItem_Field2:D5},{dataItem.DataItem_Field4:D5},{dataItem.DataItem_Field6:D5},{dataItem.DataItem_Field8:D5},{dataItem.FileName}");
        }
        string dump = sb.ToString();
        File.WriteAllText("dataitemdump.csv", dump);
        return dump;
    }
}