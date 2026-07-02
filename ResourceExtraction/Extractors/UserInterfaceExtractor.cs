namespace ResourceExtraction.Extractors;

using GameData.Resources.Menu;

using ResourceExtraction.Imaging;

using System;
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
        userInterface.MenuEntries = AppendCompassWindowIfMain(id, uiElements);

        CanonicalSpace.Apply(userInterface);
        return userInterface;
    }

    // REQ_MAIN ships no compass element, but the travel HUD's scrolling compass needs a window
    // rect. Synthesize a data-only marker at the fixed FRAME.SCR compass window (drawCompass @
    // KRONDOR.EXE 0x4691f: renderView VGA 144,121..175,131 => x144,y121,w31,h10). ElementType.Unknown
    // so no renderer draws it; added here (VGA coords) so CanonicalSpace.Apply scales it like the rest.
    private static UiElement[] AppendCompassWindowIfMain(string id, UiElement[] uiElements) {
        if (id == null || id.IndexOf("REQ_MAIN", StringComparison.OrdinalIgnoreCase) < 0) {
            return uiElements;
        }
        var withCompass = new UiElement[uiElements.Length + 1];
        uiElements.CopyTo(withCompass, 0);
        withCompass[^1] = new UiElement {
            ElementType = ElementType.Unknown,
            ActionId = UserInterface.CompassWindowActionId,
            Visible = false,
            XPosition = 144,
            YPosition = 121,
            Width = 31,
            Height = 10,
        };
        return withCompass;
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
