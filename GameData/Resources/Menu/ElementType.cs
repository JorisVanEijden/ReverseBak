namespace GameData.Resources.Menu;

/// <summary>
/// A REQ menu element's widget kind — the on-disk <c>uiElement.elementType</c>, dispatched by
/// <c>menu_drawElement</c> (@0x2b73a) to a per-type renderer. (The engine also defines a type 8,
/// a <see cref="TextButton"/> variant sharing <c>menu_type_6_8</c>. It appears in no shipped REQ
/// FILE, which is why it is not in this enum — but the engine does use it: the keyword menu builds
/// type 8 at runtime for an already-asked topic. The only difference is in the UNPRESSED branch of
/// <c>widget_draw_text_button</c> — text pen 0x0a becomes 1 and the drop shadow is dropped — so it
/// is the same button, differently lettered, and it still presses.)
/// </summary>
public enum ElementType {
    /// <summary>Invisible click hotspot, no chrome (<c>menu_type_0_1_5</c>).</summary>
    ClickArea = 0x0000,

    /// <summary>Text-entry box, drawn from the companion IN_*.DAT (<c>menu_type_0_1_5</c>).</summary>
    InputField = 0x0001,

    /// <summary>Scrolling list / file picker (<c>menu_type_2</c>).</summary>
    FilePicker = 0x0002,

    /// <summary>Icon button, two frames — normal / pressed (<c>menu_type_3_4</c>).</summary>
    ImageButton = 0x0003,

    /// <summary>On/off toggle icon; state from the field_9 flag (<c>menu_type_3_4</c>).</summary>
    Toggle = 0x0004,

    /// <summary>Clickable text link (<c>menu_type_0_1_5</c>).</summary>
    TextLink = 0x0005,

    /// <summary>Bevelled text button (<c>menu_type_6_8</c>).</summary>
    TextButton = 0x0006,

    /// <summary>A stateful icon with three frames — <c>icon</c> normal, <c>icon+1</c> active/pressed,
    /// <c>icon+2</c> highlighted (when the field_7 flag is set). Handler <c>menu_type_7</c> (@0x2bf44).
    /// Defined by the engine but used by <b>no</b> shipped REQ file (behaviour RE'd, purpose unattested).</summary>
    StatefulIcon = 0x0007,

    // ── Synthetic — not an on-disk elementType ──────────────────────────────────────────────────
    /// <summary>Not a real game widget. A data-only marker the extractor synthesizes for REQ_MAIN's
    /// travel-HUD compass window, which has no native REQ element (a fixed FRAME.SCR design rect,
    /// <c>drawCompass</c> @0x4691f). Non-rendered; the Unity <c>CompassView</c> reads its canonical rect
    /// via <see cref="UserInterface.CompassWindowActionId"/>. The 0x1000 value is deliberately outside
    /// the on-disk range so it can never collide with a parsed <c>elementType</c>.</summary>
    CompassWindow = 0x1000
}
