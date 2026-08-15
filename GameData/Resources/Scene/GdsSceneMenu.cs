namespace GameData.Resources.Scene;

using GameData.Resources.GameState;
using GameData.Resources.Menu;
using System;
using System.Collections.Generic;

/// <summary>
/// Turns a location's hotspots into the menu the engine actually clicks on — the element-building
/// loop of <c>gds_loadSceneFile</c> (ovr149 @0x4d878).
///
/// <para><b>A GDS scene's hotspots are not a separate interaction system.</b> The original walks the
/// scene's 36-byte hotspot records and writes them into <c>REQ_GDS.DAT</c>'s element array as plain
/// click areas, then runs the ordinary menu loop over it. So a port already owning a REQ renderer
/// does not need a second one for locations — it needs this translation and nothing else.</para>
/// </summary>
public static class GdsSceneMenu {
    /// <summary>
    /// Builds the click areas for a scene, in the original's order and with its action ids.
    /// </summary>
    /// <param name="scene">The loaded scene.</param>
    /// <param name="frame">
    /// <c>REQ_GDS.DAT</c> — the layout the elements are written into. Only its origin is used; see
    /// <see cref="RebaseX"/> for why it is required rather than optional.
    /// </param>
    /// <param name="chapter">Current chapter, 1-based.</param>
    /// <param name="preserve">Re-entering a scene rather than loading it fresh.</param>
    /// <param name="gatePasses">Evaluates a hotspot's visibility gate against live globals.</param>
    /// <remarks>
    /// Hidden hotspots are dropped from the result but <b>still consume their action id</b> — see
    /// <see cref="GdsSceneRules.ActionIdFor"/>.
    /// </remarks>
    public static UiElement[] BuildElements(GdsScene scene, UserInterface frame, int chapter,
        bool preserve, Func<Condition, bool> gatePasses) {
        if (scene?.Hotspots == null || frame == null) {
            return [];
        }

        var elements = new List<UiElement>(scene.Hotspots.Length);
        for (var i = 0; i < scene.Hotspots.Length; i++) {
            GdsHotspot hotspot = scene.Hotspots[i];
            if (!GdsSceneRules.IsHotspotVisible(hotspot, chapter, preserve, gatePasses)) {
                continue;
            }

            elements.Add(new UiElement {
                ElementType = ElementType.ClickArea,
                ActionId = GdsSceneRules.ActionIdFor(i),
                XPosition = RebaseX(hotspot, frame),
                YPosition = RebaseY(hotspot, frame),
                Width = hotspot.Width,
                Height = hotspot.Height,
                Cursor = CursorIndexFor(hotspot),
                // A click area carries no artwork: the picture underneath is the scene's animation.
                Visible = false,
            });
        }

        return elements.ToArray();
    }

    /// <summary>
    /// A hotspot's left edge <b>relative to the layout it is written into</b>.
    /// </summary>
    /// <remarks>
    /// <b>Hotspot coordinates are absolute and the element array is not.</b> The original subtracts
    /// the menu's own origin from every hotspot (<c>xPos - menuData.xPosition</c>) before storing it,
    /// so the two are in different spaces and copying the raw value across misplaces every click
    /// region by the layout's offset — silently, because the regions are invisible and the picture
    /// beneath them is unaffected. This is why <c>REQ_GDS.DAT</c> is a required argument and not a
    /// convenience.
    /// </remarks>
    public static int RebaseX(GdsHotspot hotspot, UserInterface frame) =>
        hotspot == null || frame == null ? 0 : hotspot.XPosition - frame.XPosition;

    /// <inheritdoc cref="RebaseX"/>
    public static int RebaseY(GdsHotspot hotspot, UserInterface frame) =>
        hotspot == null || frame == null ? 0 : hotspot.YPosition - frame.YPosition;

    /// <summary>
    /// The cursor index the renderer should show, converted out of the file's 1-based numbering.
    /// </summary>
    /// <remarks>
    /// The original stores <c>cursor - 1</c> into the element (<c>dec ax</c>), which turns the
    /// file's "no cursor" 0 into <b>-1</b> — the same value the renderer already uses for "restore
    /// the default arrow". So the off-by-one and the no-cursor case resolve together, and passing
    /// the authored value straight through would shift every hotspot onto the neighbouring cursor.
    /// </remarks>
    public static int CursorIndexFor(GdsHotspot hotspot) => (hotspot?.Cursor ?? 0) - 1;

    /// <summary>
    /// The cursor set a location uses.
    /// </summary>
    /// <remarks>
    /// Locations index <c>POINTERG</c>, not the <c>POINTER</c> set the REQ menus use. Same element
    /// type, same renderer, different set — so the set has to be selected per screen rather than
    /// assumed from the widget kind.
    /// </remarks>
    public const string CursorSet = "POINTERG";
}
