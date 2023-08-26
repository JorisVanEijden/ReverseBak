namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources;

using System.Text;
using System.Text.Json;

public class MenuExtractor : ExtractorBase {
    public static MenuData Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        var menuData = new MenuData();
        menuData.Type = (MenuType)resourceReader.ReadUInt16();
        menuData.Modal = resourceReader.ReadUInt16() != 0;
        menuData.Unknown0 = resourceReader.ReadUInt16();
        menuData.XPosition = resourceReader.ReadUInt16();
        menuData.YPosition = resourceReader.ReadUInt16();
        menuData.Width = resourceReader.ReadUInt16();
        menuData.Height = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // Placeholder for number of menu entries
        _ = resourceReader.ReadUInt16(); // Placeholder for pointer to menu entries
        menuData.Unknown1 = resourceReader.ReadUInt16();
        menuData.Unknown2 = resourceReader.ReadUInt16();
        menuData.Unknown3 = resourceReader.ReadUInt16();
        menuData.Unknown4 = resourceReader.ReadUInt32();
        ushort numberOfEntries = resourceReader.ReadUInt16();
        var menuEntries = new MenuEntry[numberOfEntries];
        for (int i = 0; i < numberOfEntries; i++) {
            menuEntries[i] = new MenuEntry {
                Unknown0 = resourceReader.ReadUInt16(),
                Unknown2 = resourceReader.ReadUInt16(),
                Unknown4 = resourceReader.ReadBoolean(),
                Unknown5 = resourceReader.ReadUInt16(),
                Unknown7 = resourceReader.ReadUInt16(),
                Unknown9 = resourceReader.ReadUInt16(),
                UnknownB = resourceReader.ReadUInt16(),
                UnknownD = resourceReader.ReadUInt16(),
                UnknownF = resourceReader.ReadUInt16(),
                Unknown11 = resourceReader.ReadUInt16(),
                Unknown13 = resourceReader.ReadUInt16(),
                LabelOffset = resourceReader.ReadInt16(),
                Unknown17 = resourceReader.ReadUInt16(),
                Unknown19 = resourceReader.ReadUInt16(),
                Unknown1B = resourceReader.ReadUInt16(),
                Unknown1D = resourceReader.ReadUInt16(),
                Unknown1F = resourceReader.ReadUInt16()
            };
        }
        ushort labelBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(labelBufferSize);
        foreach (MenuEntry entry in menuEntries) {
            if (entry.LabelOffset >= 0) {
                entry.Label = GetZeroTerminatedString(stringBuffer, entry.LabelOffset);
            }
        }
        menuData.MenuEntries = menuEntries;
        return menuData;
    }

    private static string GetZeroTerminatedString(IReadOnlyList<char> stringBuffer, short offset) {
        var label = new StringBuilder();
        for (int i = offset; i < stringBuffer.Count; i++) {
            if (stringBuffer[i] == '\0') {
                break;
            }
            label.Append(stringBuffer[i]);
        }
        return label.ToString();
    }

    public static void ExtractToFile(string filePath) {
        const string resourceDirectory = "REQ";
        if (!Directory.Exists(resourceDirectory)) {
            Directory.CreateDirectory(resourceDirectory);
        }
        MenuData menu = Extract(filePath);
        string json = JsonSerializer.Serialize(menu, new JsonSerializerOptions {
            WriteIndented = true
        });
        File.WriteAllText(Path.Combine(resourceDirectory, Path.GetFileNameWithoutExtension(filePath) + ".json"), json);
    }
}