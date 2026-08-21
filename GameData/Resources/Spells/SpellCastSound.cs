namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// The sound a spell makes when it is cast.
/// </summary>
/// <remarks>
/// Read by sweeping every <c>Cast_*</c> function in the executable for the immediate it pushes
/// before <c>audio_PlaySound</c>, rather than from a table — <b>there is no sound field on a spell.</b>
/// Each cast routine plays its own cue inline, which is why this mapping cannot be recovered from the
/// game data at all and has to come from the code.
///
/// <para><b>Three states, not two.</b> A spell can have a verified cue, be verified to have NO cue
/// (Evil Seek pushes nothing), or simply not be mapped yet. Collapsing the last two makes an
/// unmapped spell look deliberately silent, and a port would then never come back to it.</para>
/// </remarks>
public static class SpellCastSound {
    /// <summary>Spells whose cast cue is confirmed from their own routine.</summary>
    private static readonly Dictionary<int, int> Sounds = new Dictionary<int, int> {
        { SpellIds.Flamecast, 1 },          // sound_arrow
        { SpellIds.BlackNimbus, 1 },        // sound_arrow
        { SpellIds.CandleGlow, 58 },        // sound_mcreate
        { SpellIds.Stardusk, 58 },
        { SpellIds.Steelfire, 58 },
        { SpellIds.StrengthDrain, 63 },     // sound_heal
        { SpellIds.MadGodsRage, 78 },       // sound_quake
        { SpellIds.WindsOfEortis, 79 },     // sound_wind
        { SpellIds.Nightfingers, 81 },      // sound_mgeneral
        { SpellIds.Invitation, 81 },
    };

    /// <summary>Spells confirmed to cast in silence.</summary>
    /// <remarks>
    /// <b>Verified absence, not an omission.</b> <c>Cast_Evil_Seek</c> contains no sound push at all,
    /// so giving it a cue would be adding one the game does not have. Kept separate from the unmapped
    /// spells so that distinction survives.
    /// </remarks>
    private static readonly HashSet<int> Silent = new HashSet<int> { SpellIds.EvilSeek };

    /// <summary>The cue for casting a spell, or null when there is none or none is known.</summary>
    public static int? ForCast(int spellId) =>
        Sounds.TryGetValue(spellId, out int sound) ? sound : (int?)null;

    /// <summary>Whether this spell's cast audio has been established either way.</summary>
    /// <remarks>
    /// True for a spell with a cue AND for one confirmed silent. False means nobody has looked, which
    /// is a different thing from "it makes no sound" and is worth being able to ask.
    /// </remarks>
    public static bool IsEstablished(int spellId) =>
        Sounds.ContainsKey(spellId) || Silent.Contains(spellId);

    /// <summary>Whether this spell is known to cast without a sound.</summary>
    public static bool IsSilent(int spellId) => Silent.Contains(spellId);

    /// <summary>
    /// Mad God's Rage plays a SECOND cue, once per target it hits.
    /// </summary>
    /// <remarks>
    /// <b>Two sounds at two moments</b> — <c>sound_quake</c> when the spell goes off and this one at
    /// each target, from a later push in the same routine. A port that treats a spell as having one
    /// cue plays the quake and drops the rest, so a rage that hits five enemies sounds identical to
    /// one that hits nobody.
    /// </remarks>
    public const int MadGodsRagePerTargetSound = 29;

    /// <summary>Whether a spell has a per-target cue on top of its cast cue.</summary>
    public static int? PerTarget(int spellId) =>
        spellId == SpellIds.MadGodsRage ? MadGodsRagePerTargetSound : (int?)null;
}
