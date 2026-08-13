namespace GameData.Resources.Scene;

using GameData.Resources.GameState;
using System;

/// <summary>
/// Which parts of an interactive location are live, and which scene you actually get —
/// <c>townscene_load</c> (<c>SRC/SCREENS/TOWNSCN.C</c>), the front half of the GDS scene loop.
///
/// <para>Drawing, animation and the action-code dispatch are the runtime's; what is here is the
/// gating the runtime must not get wrong, because a mis-gated hotspot is either an interaction the
/// player cannot find or one they can reach a chapter too early.</para>
/// </summary>
public static class GdsSceneRules {
    /// <summary>
    /// Action ids the scene's hotspots occupy: hotspot <c>i</c> is <c>0x80 + i</c>. Worth knowing
    /// before adding synthetic entries to a scene panel — anything below this collides.
    /// </summary>
    public const int HotspotActionIdBase = 0x80;

    /// <summary>The action id for the hotspot at <paramref name="index"/> in the scene's list.</summary>
    public static int ActionIdFor(int index) => HotspotActionIdBase + index;

    /// <summary>
    /// Whether a hotspot is interactive right now.
    /// </summary>
    /// <param name="chapter">The current chapter, 1-based.</param>
    /// <param name="preserve">
    /// The original's third <c>townscene_load</c> argument, set when a scene is re-entered rather
    /// than freshly loaded. <b>It bypasses the chapter test entirely</b> — on a preserved load every
    /// hotspot is created regardless of the chapter mask, and only the flag gate still applies.
    /// </param>
    /// <param name="gatePasses">
    /// Evaluates the hotspot's <see cref="GdsHotspot.VisibilityGate"/>. Supplied by the caller
    /// because it needs live game state; the gate is a <b>range</b> test (<c>min ≤ value ≤ max</c>
    /// on a global), not a boolean, which <see cref="VarCondition"/> carries faithfully.
    /// </param>
    public static bool IsHotspotVisible(GdsHotspot hotspot, int chapter, bool preserve,
        Func<Condition, bool> gatePasses) {
        if (hotspot == null) {
            return false;
        }

        // The chapter list is a HIDE list, not a show list — the original tests the chapter's bit in
        // wChapterMask and creates the hotspot when it is CLEAR. Reading it the other way round
        // hides exactly the hotspots that should be showing.
        if (!preserve && hotspot.HiddenInChapters != null
            && hotspot.HiddenInChapters.Contains(chapter)) {
            return false;
        }

        if (hotspot.VisibilityGate == null) {
            return true; // gate id 0 = ungated
        }
        return gatePasses == null || gatePasses(hotspot.VisibilityGate);
    }

    /// <summary>
    /// Which sub-scene a location actually opens on.
    ///
    /// <para>Two locations swap themselves for a different sub-scene once a story flag is set, and
    /// the redirect happens <b>before</b> the file is even named — so the scene you load is not
    /// always the one you asked for. Both are hard-coded in <c>townscene_load</c> against specific
    /// chapter/sub pairs rather than being data, which is why they cannot be read out of the GDS
    /// files and have to live here.</para>
    /// </summary>
    /// <param name="readFlag">Reads a global flag's value.</param>
    /// <returns>The sub-scene letter index to load.</returns>
    public static int ResolveSubScene(int chapter, int sub, Func<int, int> readFlag) {
        if (readFlag == null) {
            return sub;
        }
        if (chapter == 0x40 && sub == 1 && readFlag(0x1c86) != 0) {
            return 7;
        }
        if (chapter == 1 && sub == 1 && readFlag(0x7539) != 0) {
            return 4;
        }
        return sub;
    }

    /// <summary>
    /// What an inn charges per night in the current chapter — <c>townscene_load</c>'s tail, which
    /// stamps the figure onto the location's actor record once per chapter.
    ///
    /// <para><b>Inns get dearer as the story advances</b>: the base unit is scaled by
    /// <c>(chapter + 19) / 20</c> in the 1.02 CD build we target, so chapter 1 pays the base rate
    /// and later chapters pay proportionally more. The floppy build used <c>(chapter + 9) / 10</c>,
    /// which climbs twice as fast — do not take the floppy branch.</para>
    /// </summary>
    /// <returns>The nightly rate, capped at 250 (<c>0xfa</c>).</returns>
    public static int InnNightlyRate(int baseUnit, int chapter) {
        long scaled = (long)baseUnit * (chapter + 19) / 20;
        return scaled > 0xfa ? 0xfa : (int)scaled;
    }
}
