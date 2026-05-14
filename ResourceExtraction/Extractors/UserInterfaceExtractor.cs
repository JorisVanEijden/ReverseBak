namespace ResourceExtraction.Extractors;

using GameData.Resources.Menu;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class UserInterfaceExtractor : ExtractorBase<UserInterface> {
    public override UserInterface Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var userInterface = new UserInterface(id);
        userInterface.UserInterfaceType = (UserInterfaceType)resourceReader.ReadUInt16();
        userInterface.IsModal = resourceReader.ReadUInt16() > 0;
        userInterface.ColorBase = resourceReader.ReadUInt16();
        userInterface.XPosition = resourceReader.ReadUInt16();
        userInterface.YPosition = resourceReader.ReadUInt16();
        userInterface.Width = resourceReader.ReadUInt16();
        userInterface.Height = resourceReader.ReadUInt16();
        _ = resourceReader.ReadUInt16(); // Placeholder for number of menu entries
        _ = resourceReader.ReadUInt16(); // Placeholder for pointer to menu entries
        short titleOffset = resourceReader.ReadInt16();
        userInterface.XOffset = resourceReader.ReadInt16();
        userInterface.YOffset = resourceReader.ReadInt16();
        _ = resourceReader.ReadUInt32(); // Placeholder for pointer to bitmap data
        ushort numberOfElements = resourceReader.ReadUInt16();
        var uiElements = new UiElement[numberOfElements];
        for (var i = 0; i < numberOfElements; i++) {
            uiElements[i] = new UiElement {
                ElementType = (ElementType)resourceReader.ReadUInt16(),
                ActionId = resourceReader.ReadInt16(),
                Visible = resourceReader.ReadBoolean(),
                ColorBase = resourceReader.ReadUInt16(),
                Disabled = resourceReader.ReadUInt16(),
                State = resourceReader.ReadUInt16(),
                XPosition = resourceReader.ReadUInt16(),
                YPosition = resourceReader.ReadUInt16(),
                Width = resourceReader.ReadUInt16(),
                Height = resourceReader.ReadUInt16(),
                Field13Offset = resourceReader.ReadInt16(),
                LabelOffset = resourceReader.ReadInt16(),
                LabelAltOffset = resourceReader.ReadInt16(),
                IconBase = resourceReader.ReadInt16(),
                Cursor = resourceReader.ReadUInt16(),
                SoundFlags = resourceReader.ReadUInt16(),
                ClickSound = resourceReader.ReadUInt16()
            };
        }
        ushort labelBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(labelBufferSize);
        foreach (UiElement entry in uiElements) {
            if (entry.LabelOffset >= 0) {
                entry.Label = GetZeroTerminatedString(stringBuffer, entry.LabelOffset);
            }
            if (entry.LabelAltOffset >= 0) {
                entry.LabelAlt = GetZeroTerminatedString(stringBuffer, entry.LabelAltOffset);
            }
            if (entry.Field13Offset >= 0) {
                entry.Field13 = GetZeroTerminatedString(stringBuffer, entry.Field13Offset);
            }
        }
        userInterface.Title = titleOffset >= 0 ? GetZeroTerminatedString(stringBuffer, titleOffset) : null;
        userInterface.MenuEntries = uiElements;

        return userInterface;
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
