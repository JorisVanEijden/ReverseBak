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

    // +0x08..+0x0C (5 × u8): the field's colour pens, copied at load time to the runtime
    // widget struct +0x18..+0x1C and consumed by the field-draw routine sub_ovr140_A73
    // (@0x47463). Palette pen indices (OPTIONS.PAL). IN_SAVE uses (14, 1, 1, 0, 10).

    /// <summary>+0x08 → runtime +0x18. Box background pen (fill behind the text; drawn @0x474a5).
    /// IN_SAVE = 14.</summary>
    public int BackgroundPen { get; set; }

    /// <summary>+0x09 → runtime +0x19. Border pen. IN_SAVE = 1.</summary>
    public int BorderPen { get; set; }

    /// <summary>+0x0A → runtime +0x1A. Caret pen — the blinking vertical line drawn on the active
    /// field when the selection is collapsed (@0x4758f). IN_SAVE = 1.</summary>
    public int CaretPen { get; set; }

    /// <summary>+0x0B → runtime +0x1B. Text pen. IN_SAVE = 0.</summary>
    public int TextPen { get; set; }

    /// <summary>+0x0C → runtime +0x1C. Selection-highlight background pen — the rect drawn over the
    /// selected substring on the active field (@0x475f6). IN_SAVE = 10.</summary>
    public int SelectionPen { get; set; }

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
