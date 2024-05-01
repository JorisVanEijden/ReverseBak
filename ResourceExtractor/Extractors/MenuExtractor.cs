namespace ResourceExtractor.Extractors;

using GameData.Resources.Menu;

using System.Text;

public class MenuExtractor : ExtractorBase {
    public static UserInterface Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        var menuData = new UserInterface();
        menuData.UserInterfaceType = (UserInterfaceType)resourceReader.ReadUInt16();
        menuData.IsModal = resourceReader.ReadUInt16() > 0;
        menuData.ColorSet = resourceReader.ReadUInt16();
        menuData.XPosition = resourceReader.ReadUInt16();
        menuData.YPosition = resourceReader.ReadUInt16();
        menuData.Width = resourceReader.ReadUInt16();
        menuData.Height = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // Placeholder for number of menu entries
        _ = resourceReader.ReadUInt16(); // Placeholder for pointer to menu entries
        short titleOffset = resourceReader.ReadInt16();
        menuData.XOffset = resourceReader.ReadInt16();
        menuData.YOffset = resourceReader.ReadInt16();
        _ = resourceReader.ReadUInt32(); // Placeholder for pointer to bitmap data
        ushort numberOfElements = resourceReader.ReadUInt16();
        var menuEntries = new UiElement[numberOfElements];
        for (int i = 0; i < numberOfElements; i++) {
            menuEntries[i] = new UiElement {
                ElementType = (ElementType)resourceReader.ReadUInt16(),
                ActionId = resourceReader.ReadInt16(),
                Visible = resourceReader.ReadBoolean(),
                ColorSet = resourceReader.ReadUInt16(),
                Unknown7 = resourceReader.ReadUInt16() > 0,
                Unknown9 = resourceReader.ReadUInt16() > 0,
                XPosition = resourceReader.ReadUInt16(),
                YPosition = resourceReader.ReadUInt16(),
                Width = resourceReader.ReadUInt16(),
                Height = resourceReader.ReadUInt16(),
                Unknown13 = resourceReader.ReadInt16(),
                LabelOffset = resourceReader.ReadInt16(),
                Teleport = resourceReader.ReadInt16(),
                Icon = resourceReader.ReadUInt16(),
                Cursor = resourceReader.ReadUInt16(),
                Unknown1D = resourceReader.ReadUInt16(),
                Sound = resourceReader.ReadUInt16()
            };
        }
        ushort labelBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(labelBufferSize);
        foreach (UiElement entry in menuEntries) {
            if (entry.LabelOffset >= 0) {
                entry.Label = GetZeroTerminatedString(stringBuffer, entry.LabelOffset);
            }
        }
        menuData.Title = titleOffset >= 0 ? GetZeroTerminatedString(stringBuffer, titleOffset) : null;
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