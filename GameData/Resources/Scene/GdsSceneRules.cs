namespace GameData.Resources.Scene;

using GameData.Resources.Animation;
using System.Collections.Generic;

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
    // ---------------------------------------------------------------- the click outcome
    // GDS_RunScene @0x4de9d, the dispatch after a hotspot's dialog returns.

    /// <summary>
    /// The scene outcome a hotspot dialog's return value maps to.
    /// </summary>
    /// <param name="dialogResult">What the dialog returned.</param>
    /// <param name="currentOutcome">The outcome so far, kept when the result is not in the table.</param>
    /// <remarks>
    /// <b>Five results are translated and everything else is ignored.</b> The mapping is neither the
    /// identity, nor a negation, nor ordered — <c>-1</c> becomes 0, <c>-2</c> becomes 5, <c>-3</c>
    /// becomes 7, <c>-4</c> becomes 3 and <c>-5</c> becomes 10 — so there is nothing to derive it
    /// from and a port has to carry the table.
    ///
    /// <para>Any other return leaves the outcome exactly as it was, which is how a dialog that merely
    /// said something falls through without disturbing the scene.</para>
    /// </remarks>
    public static int OutcomeFor(int dialogResult, int currentOutcome) {
        switch (dialogResult) {
            case -1: return 0;
            case -2: return 5;
            case -3: return 7;
            case -4: return 3;
            case -5: return 10;
            default: return currentOutcome;
        }
    }

    /// <summary>
    /// <b>One outcome invalidates the palette.</b>
    /// </summary>
    /// <remarks>
    /// The <c>-2</c> arm additionally clears the current-palette pointer, so whatever runs next has
    /// to reload it. None of the other four touch it. A port that treats the outcomes as
    /// interchangeable numbers loses the reload and keeps the scene's colours into whatever follows.
    /// </remarks>
    public static bool InvalidatesPalette(int dialogResult) => dialogResult == -2;

    /// <summary>The most barding earnings a shop will hold.</summary>
    public const int MaxBardingReward = 250;

    /// <summary>
    /// <b>Barding earnings travel through a global and are banked per shop, capped.</b>
    /// </summary>
    /// <param name="pendingReward">The global's value when the dialog returns.</param>
    /// <remarks>
    /// Before the dialog the shop's stored reward is loaded into a global; after it, the global is
    /// written back into the shop clamped to <see cref="MaxBardingReward"/> and then zeroed. So the
    /// credit is per-location rather than per-party, it survives leaving and re-entering, and it
    /// stops accumulating at 250.
    ///
    /// <para>The zeroing matters as much as the cap: the global is scratch space for one visit, so
    /// carrying it between locations would pay a second innkeeper for the same performance.</para>
    /// </remarks>
    public static int BankedBardingReward(int pendingReward) =>
        pendingReward > MaxBardingReward ? MaxBardingReward : pendingReward;

    /// <summary>The global is cleared after banking, so nothing carries to the next location.</summary>
    public static bool BardingRewardGlobalIsScratch => true;
    // ---------------------------------------------------------------- what a scene actually draws
    // GDS_RunScene @0x4de9d, the entry sequence.

    /// <summary>
    /// <b>A location's picture is its animation, not a background image.</b>
    /// </summary>
    /// <remarks>
    /// Verified from the entry sequence rather than inferred: the scene file is loaded, and the very
    /// next thing drawn is <c>playAnimationScene</c> on the scene's own animation id. There is no
    /// background bitmap load anywhere near it.
    ///
    /// <para>The one SCX at entry is <c>Dialog.scr</c>, loaded <b>once</b> per run and into a
    /// <i>different</i> video buffer — it is the dialogue panel's frame, not the location. Reading it
    /// as the backdrop is the obvious mistake, and it would put the dialogue border behind every
    /// town gate.</para>
    ///
    /// <para>So rendering a location means driving the cutscene engine and holding on its last
    /// frame. A port looking for a static image per scene will not find one.</para>
    /// </remarks>
    public static bool PictureComesFromTheAnimation => true;

    /// <summary>The single SCX the scene loop loads at entry, and what it is for.</summary>
    public const string DialogueFrameResource = "Dialog.scr";

    /// <summary>
    /// <b>The dialogue frame is loaded once, not per sub-scene.</b>
    /// </summary>
    /// <remarks>
    /// Guarded by a flag that is set the first time through, so re-entering a sub-scene does not
    /// reload it. Cheap to get wrong in a port built around per-scene setup, and the symptom would be
    /// a flicker on every hotspot that changes scene.
    /// </remarks>
    public static bool DialogueFrameLoadsOncePerRun => true;

    /// <summary>
    /// The cursor set interactive locations use.
    /// </summary>
    /// <remarks>
    /// Loaded at entry: an arrow, a torch, an hourglass, a magnifier and then a run of baked gothic
    /// text labels — the hotspot names are <i>pictures</i>, not strings rendered at runtime. Which
    /// hotspot shows which is data-driven by the scene, the same pattern REQ elements use, so a port
    /// cannot generate those labels from the hotspot's name text.
    /// </remarks>
    public const string CursorSetResource = "POINTERG.BMX";
    // ---------------------------------------------------------------- animation tags

    /// <summary>
    /// The ADS tag name for one of a scene's animation fields.
    /// </summary>
    /// <param name="animations">The animation resource's scripts.</param>
    /// <param name="sceneAnimationTag">
    /// <see cref="GdsScene.EntryAnimationTag"/>, <see cref="GdsScene.IdleAnimationTag"/> or
    /// <see cref="GdsScene.TransitionAnimationTag"/>.
    /// </param>
    /// <returns>The tag name, or null when the animation has no such script.</returns>
    /// <remarks>
    /// <b>A scene's animation fields are ADS script <i>ids</i>, not tag names.</b> The player selects
    /// scripts by their tag <i>string</i>, so handing it the number — or the number formatted as text
    /// — matches nothing, and the failure is a log line and a blank screen rather than an exception.
    /// That is the whole reason this exists.
    ///
    /// <para>Confirmed against the shipped data: <c>GDS10A</c> asks <c>g_town</c> for entry 10, which
    /// is that resource's "ARMANGAR" script, and <c>GDS11A</c> asks for 11, which is
    /// "SILDEN(CHEAM)". Both towns get their own arrival animation and both share idle 13, the
    /// resource's "DISPLAY" loop.</para>
    /// </remarks>
    public static string AnimationTagFor(IEnumerable<AnimatorScript> animations,
        int sceneAnimationTag) {
        if (animations == null) {
            return null;
        }

        foreach (AnimatorScript script in animations) {
            if (script != null && script.Id == sceneAnimationTag) {
                return script.Tag;
            }
        }

        return null;
    }

    /// <summary>
    /// A scene animation field of zero means <b>there is no such animation</b>.
    /// </summary>
    /// <remarks>
    /// ADS script ids start at one, so zero cannot name a script. Scenes with no transition carry
    /// zero in that field, and asking the player for it would produce the same silent miss as passing
    /// an id where a name belongs.
    /// </remarks>
    public static bool HasAnimation(int sceneAnimationTag) => sceneAnimationTag > 0;
}
