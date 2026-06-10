namespace GameData.Resources.Scene;

using System;

/// <summary>
/// A GDS interactive location scene (the on-disk files are named GDS&lt;number&gt;&lt;letter&gt;.DAT,
/// e.g. GDS1A, GDS64A; the expansion of the "GDS" prefix is unknown and deliberately not assumed).
///
/// At runtime the engine (ovr149 <c>GDS_RunScene</c> @0x4de9d / <c>gds_loadSceneFile</c> @0x4d878)
/// loads one of these when entering a town gate, shop, temple or scripted encounter. It draws an SCX
/// background, plays the scene's animation phases, and turns each <see cref="GdsHotspot"/> into a
/// clickable region. Scenes form a small state machine: clicking a hotspot can chain to a sibling
/// sub-scene (same <c>number</c>, different <c>letter</c>) — see <see cref="NextSceneLetter"/> and
/// <see cref="GdsHotspot.NextSceneLetter"/> — or run an action (dialog, give items, shop, teleport).
///
/// The .DAT file is a verbatim dump of the original C struct, so it also contains stale DOS far
/// pointers (the header's resolved dialog/container pointers and each hotspot's resolved dialog
/// pointer). Those are overwritten before they are ever read, carry no information, and are dropped
/// here. Every file in the set satisfies: fileSize == 2 + 41 + 36 * <see cref="Hotspots"/>.Length.
/// </summary>
[Serializable]
public class GdsScene : IResource {
    public GdsScene(string id) {
        Id = id;
    }

    /// <summary>Base name of the scene animation resource (label, max 12 chars), e.g. "g_town" -&gt; G_TOWN.ADS. Empty when the scene has no animation.</summary>
    public string AnimationResource { get; set; } = string.Empty;

    /// <summary>Audio song id played while the scene is on screen (header @0x11). 0 = none.</summary>
    public int Song { get; set; }

    /// <summary>Dialog id shown when the scene first opens (header @0x1D).</summary>
    public int SceneDialogId { get; set; }

    /// <summary>
    /// Animation tag played once on scene entry, before fade-in (header @0x15;
    /// <c>playAnimationScene</c> @0x4df6c).
    /// </summary>
    public int EntryAnimationTag { get; set; }

    /// <summary>
    /// Animation tag played for the steady-state scene display and re-shown after each action
    /// (header @0x19; <c>playAnimationScene</c> @0x4e01a / @0x4e66b).
    /// </summary>
    public int IdleAnimationTag { get; set; }

    /// <summary>
    /// Animation tag played on a sub-scene transition or exit (header @0x17;
    /// <c>playAnimationScene</c> @0x4e5f6 / @0x4e71b). Constant 0 in every shipped file.
    /// </summary>
    public int TransitionAnimationTag { get; set; }

    /// <summary>
    /// Default sub-scene letter to chain to on a "go to scene" action (<see cref="GdsHotspot.ActionCode"/> 3)
    /// or the scene's back/exit path (header @0x0F). 0 = leave the GDS scene and return to the world.
    /// </summary>
    public int NextSceneLetter { get; set; }

    /// <summary>
    /// On scene entry, if bit 0x80 is set the engine sets global bit-flag <c>6480 + (value &amp; 0x7F)</c>
    /// to 1 (header @0x0D; @0x4dfa0). In shipped data this is used exclusively by the 15 temple scenes:
    /// each temple carries a unique index 1-15, so bits 6481-6495 form the "temple visited" region of the
    /// global-flags bitfield. <c>UI_teleportation</c> reads these bits to decide which temples are
    /// available teleport destinations (the temple teleport network). 0 = no flag set (non-temple scenes).
    /// Note: global keys &lt; 8500 are individual bits (key = bit index), so 6480 is a base bit-index.
    /// </summary>
    public int EntryFlagWord { get; set; }

    /// <summary>
    /// Header @0x13. Meaning UNDETERMINED — do not treat this as a music volume. The on-disk value
    /// carries real per-scene information (it tracks scene structure: entry "A" scenes = 25, interior
    /// tavern/shop sub-scenes = 18-20, a few shops = 60), but the engine discards all of it. Its sole
    /// consumer is a threshold in the loader, <c>level = (value &lt; 25) ? 75 : 60</c>
    /// (gds_loadSceneFile @0x4db31); that level is then handed to <c>audio_setSongVolume</c> as the scene
    /// music volume. But after the volume curve the two levels render as 125 vs 121 on a ~0-127 scale —
    /// an inaudible ~0.3 dB difference — and the relation is inverted for a volume (smaller value → louder
    /// output). So the field has no meaningful runtime effect; it is preserved verbatim for analysis only.
    /// Observed file values: 18/19/20/25/60.
    /// </summary>
    public int Unknown13 { get; set; }

    public GdsHotspot[] Hotspots { get; set; } = [];

    public ResourceType Type => ResourceType.GDS;

    public string Id { get; }
}
