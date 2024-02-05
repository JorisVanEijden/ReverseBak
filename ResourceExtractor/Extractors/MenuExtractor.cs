namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Menu;

using System.Text;

public class MenuExtractor : ExtractorBase {
    public static UserInterface Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        var menuData = new UserInterface();
        menuData.UserInterfaceType = (UserInterfaceType)resourceReader.ReadUInt16();
        menuData.Modal = resourceReader.ReadUInt16() > 0;
        menuData.Unknown0 = resourceReader.ReadUInt16();
        menuData.XPosition = resourceReader.ReadUInt16();
        menuData.YPosition = resourceReader.ReadUInt16();
        menuData.Width = resourceReader.ReadUInt16();
        menuData.Height = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // Placeholder for number of menu entries
        _ = resourceReader.ReadUInt16(); // Placeholder for pointer to menu entries
        menuData.XOffset = resourceReader.ReadInt16();
        menuData.YOffset = resourceReader.ReadInt16();
        menuData.Unknown3 = resourceReader.ReadUInt16();
        menuData.Unknown4 = resourceReader.ReadUInt32();
        ushort numberOfEntries = resourceReader.ReadUInt16();
        var menuEntries = new UiElement[numberOfEntries];
        for (int i = 0; i < numberOfEntries; i++) {
            menuEntries[i] = new UiElement {
                ElementType = (ElementType)resourceReader.ReadUInt16(),
                ActionId = resourceReader.ReadInt16(),
                Visible = resourceReader.ReadBoolean(),
                Unknown5 = resourceReader.ReadUInt16(),
                Unknown7 = resourceReader.ReadUInt16() > 0,
                Unknown9 = resourceReader.ReadUInt16() > 0,
                XPosition = resourceReader.ReadUInt16(),
                YPosition = resourceReader.ReadUInt16(),
                Width = resourceReader.ReadUInt16(),
                Height = resourceReader.ReadUInt16(),
                Unknown13 = resourceReader.ReadInt16(),
                LabelOffset = resourceReader.ReadInt16(),
                Unknown17 = resourceReader.ReadInt16(),
                Icon = resourceReader.ReadUInt16(),
                Unknown1B = resourceReader.ReadUInt16(),
                Unknown1D = resourceReader.ReadUInt16(),
                Unknown1F = resourceReader.ReadUInt16()
            };
        }
        ushort labelBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(labelBufferSize);
        foreach (UiElement entry in menuEntries) {
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
}