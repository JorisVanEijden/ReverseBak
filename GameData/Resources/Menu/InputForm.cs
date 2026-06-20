namespace GameData.Resources.Menu;

using System.Collections.Generic;

/// <summary>
/// An IN_*.DAT "input form" — a list of labelled input/list-box widgets used by the
/// original game's <b>editor</b> tools (and, for IN_SAVE.DAT, reused by the in-game
/// save/load dialog). Reversed from <c>Load_in_file</c> (ovr140 @ 0x469f0).
///
/// On-disk layout:
///   u16 stringBufLen; byte[stringBufLen] stringPool;   // NUL-terminated caption strings
///   u16 fieldCount;   field[fieldCount]                 // 21 bytes each (see InputField)
///
/// Of the ten shipped IN_*.DAT files only <b>IN_SAVE.DAT</b> is loaded by the shipped game
/// (its "Directories" / "Games" list columns, via <c>dialog_SaveGame</c>); the other nine
/// (IN_GE3, IN_GE_S, IN_TE_A/L/S/T/V/Z, IN_ZONE) have no loader in KRONDOR.EXE and are
/// build-time editor sources — extracted here as canonical original data. Their captions
/// give away the editor forms they describe (e.g. IN_ZONE: Zone / TileX / TileY / CellX /
/// CellY). See <c>docs/FileFormats/IN_DAT.md</c>.
///
/// Coordinates are 320×200 mode-13h pixels on disk; the extractor scales them into the
/// canonical 1600×1200 space (×5 / ×6 via <c>AspectCorrection</c>), like the other UI
/// coordinate formats (REQ / GDS / FMAP / ENCAMP).
/// </summary>
public class InputForm : IResource {
    public InputForm(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    public List<InputField> Fields { get; set; } = new();
}

/// <summary>One labelled input/list widget (21 bytes on disk).</summary>
public class InputField {
    /// <summary>+0x00 (u16). Number of rows/items the widget holds (e.g. IN_SAVE "Games" = 90).</summary>
    public int ItemCount { get; set; }

    /// <summary>+0x02 (u16). Input-box X (canonical space). Widget height is computed from the
    /// font at runtime, not stored.</summary>
    public int X { get; set; }

    /// <summary>+0x04 (u16). Input-box Y (canonical space).</summary>
    public int Y { get; set; }

    /// <summary>+0x06 (u16). Input-box width (canonical space).</summary>
    public int Width { get; set; }

    /// <summary>+0x08..+0x0C (5 × u8). Style/flag bytes (colour/type); shipped editor forms
    /// all use (14, 1, 1, 0, 10). Preserved verbatim.</summary>
    public List<int> StyleBytes { get; set; } = new();

    /// <summary>Caption text (resolved from the +0x0D u16 string-pool offset; empty if -1).</summary>
    public string Label { get; set; } = "";

    /// <summary>+0x0F (u16). Caption-text X (canonical space).</summary>
    public int LabelX { get; set; }

    /// <summary>+0x11 (u16). Caption-text Y (canonical space).</summary>
    public int LabelY { get; set; }

    /// <summary>+0x13 (u16). Allocate/extra flag (<c>boolAllocate</c> in the constructor);
    /// 0 in all shipped fields.</summary>
    public int AllocateFlag { get; set; }
}
