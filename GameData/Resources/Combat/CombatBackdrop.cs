namespace GameData.Resources.Combat;

/// <summary>
/// The one encounter that fights against a painted backdrop instead of the world —
/// <c>combat_captureArenaBackdrop</c> (IDA <c>0x2210b</c>).
/// </summary>
/// <remarks>
/// <b>It overwrites the arena backdrop AFTER the world is drawn into it.</b> The routine renders
/// every visible world item into VGA buffer C — the still that is the backdrop for the whole fight —
/// and only then, at 0x2227d, tests the encounter number and blits <c>fcombat.scx</c> over the lot.
/// So this is not "render something else": it is the world render being covered up.
///
/// <para><b>The original suppresses it during a GEOMETRY PROBE, and that suppression is
/// load-bearing.</b> The routine takes a flag (<c>arg_0</c>) that sets <c>word_dseg_494</c> and
/// skips the blit. Five call sites: <c>enterCombatGrid</c> @0x61343 passes 0 — the real entry, so
/// the swap happens — while <c>arena_buildGridByRenderProbe</c> passes 1 at both of its calls. That
/// probe is the UNDERGROUND arena builder and it works by rendering the scene and READING PIXELS
/// BACK: it samples a reference pixel, floods the buffer with a sentinel, renders again, and asks of
/// each projected cell whether the floor colour still shows. Swapping a painted image in mid-probe
/// would corrupt every one of those reads.
///
/// <para>None of that probe machinery is ported — it is engine architecture, and our arena is the
/// live 3D view rather than a captured still. It is recorded because the flag reads like a spare
/// argument, and "always 0" is what this project's own backlog said until the xrefs were checked.
/// </para></para>
/// </remarks>
public static class CombatBackdrop {
    /// <summary>The only encounter that carries one.</summary>
    public const long PaintedBackdropEncounter = 545;

    /// <summary>The image it carries.</summary>
    /// <remarks>
    /// Its palette is <c>OPTIONS.PAL</c> — <see cref="PaletteMapping"/> already knew that before
    /// anything displayed it.
    /// </remarks>
    public const string PaintedBackdropImage = "FCOMBAT.SCX";

    /// <summary>The image this encounter fights against, or null for the world.</summary>
    public static string ImageFor(long encounterNumber) =>
        encounterNumber == PaintedBackdropEncounter ? PaintedBackdropImage : null;
}
