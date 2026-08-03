namespace ResourceExtraction.Extractors;

using GameData.Resources.Inventory;
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
        userInterface.Colorset = ToColorset(resourceReader.ReadUInt16());
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
            var elementType = (ElementType)resourceReader.ReadUInt16();
            var actionId = resourceReader.ReadInt16();
            var visible = resourceReader.ReadBoolean();
            _ = resourceReader.ReadUInt16(); // Per-element colour base pen — Unity theme concern now, discarded.
            uiElements[i] = new UiElement {
                ElementType = elementType,
                ActionId = actionId,
                Visible = visible,
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
        AdjustNavArrowClickBoxesIfMain(id, userInterface);
        ApplyInventoryLayoutIfReqInv(id, userInterface);
        return userInterface;
    }

    // REQ_INV's grid/paperdoll geometry is not read from the REQ_INV.DAT bytes — it is
    // transcribed from immediate operands in sub_ovr157_0 @0x54210 (see InventoryLayout for the
    // full provenance), exactly like the credits screen's geometry. Exact-filename match, not a
    // substring test: REQ_INV2 is a distinct screen (no fixed-cell grid) and must not match.
    private static void ApplyInventoryLayoutIfReqInv(string id, UserInterface ui) {
        if (!string.Equals(id, "REQ_INV.DAT", StringComparison.OrdinalIgnoreCase)) {
            return;
        }
        ui.Inventory = new InventoryLayout();
    }

    // REQ_MAIN's four travel nav-arrow click boxes are authored at TWICE the arrow's extent along
    // their long axis — the horizontal forward/back boxes are double width, the vertical turn-left/
    // right boxes are double height — so clicks register far beyond the arrow icon (the original's
    // hit-test uses the whole box; the extra area is dead space right of / below each arrow). Halve
    // that offending dimension, then nudge it back up (width +10% / height +25%, calibrated in-Editor)
    // so the box sits snugly on the arrow. Only the halved dimension is touched; the other stays.
    // Done in canonical space (post-scale). ActionIds are the DOS arrow scancodes:
    //   72 = forward (0x48), 80 = back (0x50)          -> horizontal, Width  = round(Width/2 * 1.10)
    //   75 = turn-left (0x4B), 77 = turn-right (0x4D)   -> vertical,   Height = round(Height/2 * 1.25)
    private static void AdjustNavArrowClickBoxesIfMain(string id, UserInterface ui) {
        if (id == null || id.IndexOf("REQ_MAIN", StringComparison.OrdinalIgnoreCase) < 0) {
            return;
        }
        foreach (UiElement e in ui.MenuEntries) {
            if (e.ActionId is 72 or 80) {
                e.Width = (int)Math.Round(e.Width / 2.0 * 1.10);
            } else if (e.ActionId is 75 or 77) {
                e.Height = (int)Math.Round(e.Height / 2.0 * 1.25);
            }
        }
    }

    // REQ_MAIN ships no compass element, but the travel HUD's scrolling compass needs a window
    // rect. Synthesize a data-only marker at the fixed FRAME.SCR compass window (drawCompass @
    // KRONDOR.EXE 0x4691f: renderView VGA 144,121..175,131 => x144,y121,w31,h10). ElementType.CompassWindow
    // (synthetic, non-rendered) so no renderer draws it; added here (VGA coords) so CanonicalSpace.Apply
    // scales it like the rest.
    private static UiElement[] AppendCompassWindowIfMain(string id, UiElement[] uiElements) {
        if (id == null || id.IndexOf("REQ_MAIN", StringComparison.OrdinalIgnoreCase) < 0) {
            return uiElements;
        }
        var withCompass = new UiElement[uiElements.Length + 1];
        uiElements.CopyTo(withCompass, 0);
        withCompass[^1] = new UiElement {
            ElementType = ElementType.CompassWindow,
            ActionId = UserInterface.CompassWindowActionId,
            Visible = false,
            XPosition = 144,
            YPosition = 121,
            Width = 31,
            Height = 10,
        };
        return withCompass;
    }

    // The on-disk colorBase byte is the screen-level base index into the renderer's 7-colour
    // palette range (base..base+6); observed shipped values are 0, 4, 7, 32, 144, 169. Map it
    // straight onto the semantic Colorset selector; anything unrecognized falls back to Menu
    // (the default fullscreen-menu set) rather than an undefined enum value.
    private static Colorset ToColorset(int rawColorBase) {
        return Enum.IsDefined(typeof(Colorset), rawColorBase) ? (Colorset)rawColorBase : Colorset.Menu;
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
