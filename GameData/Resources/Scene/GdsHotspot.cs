namespace GameData.Resources.Scene;

using System;
using System.Collections.Generic;

using GameData.Resources.GameState;

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
    /// Chapters in which this hotspot is hidden during a full load (decoded from
    /// the on-disk per-chapter bitmask; 8 also covers chapters 9+). Empty = visible
    /// in every chapter.
    /// </summary>
    public List<int> HiddenInChapters { get; set; } = [];

    /// <summary>Whether this hotspot is eligible to be the scene's default/auto-target (on-disk bit 0x8000).</summary>
    public bool CanBeDefaultTarget { get; set; }

    /// <summary>
    /// Visibility gate on global game state, decoded to a shared <see cref="Condition"/>
    /// (the same vocabulary as dialog branches). <c>null</c> = no gate (on-disk key 0).
    /// </summary>
    public Condition? VisibilityGate { get; set; }

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
}
