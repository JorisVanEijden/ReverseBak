namespace GameData.Resources.Scene;

using System;

/// <summary>
/// One clickable region in a <see cref="GdsScene"/> (the original 36-byte <c>gds_struct36</c> record).
/// Hotspots are addressed in array order: the engine builds a menu element with actionId = 128 + index.
/// </summary>
[Serializable]
public class GdsHotspot {
    // --- Click rectangle in canonical 1600x1200 space (square-pixel, aspect-true). The on-disk
    // values are raw mode-13h pixels (X,W over 320; Y,H over 200); the extractor applies the shared
    // VGA correction (x5 / x6) so these match every other screen-layout resource — REQ UserInterface,
    // labels, TTM/DDX coords — and a renderer can treat a GDS hotspot exactly like a REQ button. ---
    /// <summary>Left edge in canonical 1600x1200 space.</summary>
    public int XPosition { get; set; }
    /// <summary>Top edge in canonical 1600x1200 space.</summary>
    public int YPosition { get; set; }
    /// <summary>Width in canonical 1600x1200 space.</summary>
    public int Width { get; set; }
    /// <summary>Height in canonical 1600x1200 space.</summary>
    public int Height { get; set; }

    /// <summary>
    /// Per-chapter visibility bitmask (@0x08). During a full load the hotspot is HIDDEN in chapter
    /// <c>c</c> when bit <c>(1 &lt;&lt; (c - 1))</c> is set. Bit 0x8000 additionally marks the hotspot as
    /// eligible to be the scene's default/auto-target. 0 = visible in every chapter.
    /// </summary>
    public int ChapterHideMask { get; set; }

    /// <summary>
    /// Mouse cursor image to show over this hotspot (@0x0A), as authored: 1-based, 0 = none.
    /// The engine subtracts 1 before indexing the cursor set (POINTERG).
    /// </summary>
    public int Cursor { get; set; }

    /// <summary>
    /// Action performed on left-click (@0x0C, byte). When <see cref="ActionDialogId"/> is set, the
    /// dialog's exit code (-1..-5) can override this. Known values (the engine's <c>di</c> dispatch):
    /// 2 = show dialog only; 3 = go to sub-scene <see cref="GdsScene.NextSceneLetter"/>;
    /// 4 = go to sub-scene <see cref="NextSceneLetter"/>; 5/6/8 = give container contents to the party;
    /// 7 = open shop; 9 = barding (instrument repair); 10/16 = shop buy/sell; 11 = teleport menu;
    /// 13 = shop services (heal/repair, chosen by dialog result); 15 = end chapter.
    /// </summary>
    public int ActionCode { get; set; }

    /// <summary>
    /// This hotspot's target sub-scene letter for a "go to scene" action (<see cref="ActionCode"/> 4)
    /// (@0x0E; @0x4e617). &lt;= 0 leaves the GDS scene and returns to the world. 0 in most hotspots.
    /// </summary>
    public int NextSceneLetter { get; set; }

    /// <summary>Animation scene/tag to play on left-click (@0x10). 0 = play the scene's default tag.</summary>
    public int AnimationSceneId { get; set; }

    /// <summary>Dialog shown on left-click; its exit code can override <see cref="ActionCode"/> (@0x12).</summary>
    public int ActionDialogId { get; set; }

    /// <summary>Dialog shown on right-click ("examine"/look-at). 0 = none (@0x16).</summary>
    public int ExamineDialogId { get; set; }

    // --- Conditional-visibility gate on a global game-state value (@0x1E / 0x20 / 0x22). ---
    /// <summary>Global state key tested for visibility; 0 = no gate.</summary>
    public int GlobalKey { get; set; }
    /// <summary>Hotspot is shown only while global[<see cref="GlobalKey"/>] is within [Min, Max].</summary>
    public int GlobalMin { get; set; }
    public int GlobalMax { get; set; }
}
