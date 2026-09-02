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
/// <para><b>Every named cast routine is now accounted for.</b> Eighteen have a cue and
/// <c>Cast_Evil_Seek</c> has none; the field-spell numbers come from <c>Cast_field_spell</c>'s
/// dispatch and the rest from <see cref="SpellIds"/>. What is still missing is spells with no
/// dedicated routine at all — they cast through <c>Cast_Spell</c>'s switch, whose cases push sounds
/// without naming a spell.</para>
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
        { SpellIds.CandleGlow, FieldSpells.CreationSound },
        { SpellIds.Stardusk, FieldSpells.CreationSound },
        { SpellIds.Steelfire, FieldSpells.CreationSound },
        { SpellIds.StrengthDrain, 63 },     // sound_heal
        { SpellIds.MadGodsRage, 78 },       // sound_quake
        { SpellIds.WindsOfEortis, 79 },     // sound_wind
        { SpellIds.Nightfingers, FieldSpells.GeneralSound },
        { SpellIds.Invitation, FieldSpells.GeneralSound },

        // The nine field spells, from Cast_field_spell's dispatch (ovr179 @0x6ca30 — a linear scan
        // of a nine-entry table, so this IS the complete field set). Their numbers live on
        // FieldSpells, not SpellIds: the two classes both name spells by number and neither is a
        // superset, which is worth knowing before adding a constant to either.
        { FieldSpells.DragonsBreath, FieldSpells.CreationSound },
        { FieldSpells.ScentOfSarig, FieldSpells.ScentSound },
        { FieldSpells.EyesOfIshap, FieldSpells.ScentSound },
        { FieldSpells.TheUnseen, 13 },              // sound_spell3
        { FieldSpells.NacreCicatrix, 13 },
        { FieldSpells.Union, FieldSpells.GeneralSound },
        { FieldSpells.AndTheLightShallLie, FieldSpells.GeneralSound },

        // Spells with NO dedicated routine, cast through Cast_Spell's switch (ovr173 @0x687a5).
        // That switch is driven by the spell number biased by three — IDA's switch info reports
        // lowcase 3 against the `mov bx,[bp+SpellNumber]` at 0x68798, so its case labels are true
        // spell numbers and not post-subtraction indices.
        //
        // *** TWO CAVEATS ON THESE THREE, ADDED 2026-09-02 WITH THE FLUX ENTRY. ***
        // 1. Grief's cue is CONDITIONAL in the original — case 13 plays it only when
        //    Grief_TargetIsSusceptible(target) answers true, which the port models as
        //    SpellPerSpellHandlers.GriefAffects. This table is unconditional, so a consumer that
        //    wants fidelity has to ask that question itself.
        // 2. These come from Cast_Spell, which is the COMBAT resolver, but ForCast is read only by
        //    FieldSpellCaster — CombatRuntime.ResolveCast uses ForCombatCast instead. Whether a
        //    combat cast of one of these should also sound is unresolved; see TASK-144.
        { SpellIds.DespairThyEyes, FieldSpells.CreationSound },        // case 3
        { SpellIds.GriefOfAThousandNights, 77 },                       // case 13, sound_sparkly
        { SpellIds.UnfortunateFlux, 77 },                              // case 20, sound_sparkly
        { SpellIds.SkinOfTheDragon, FieldSpells.CreationSound },       // case 23
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
    // ------------------------------------------------------------- the COMBAT path's cues
    //
    // *** THESE ARE NOT ALTERNATIVES TO ForCast, THEY ARE A DIFFERENT PATH. *** ForCast keys on the
    // SPELL and is what the field caster plays. Everything below keys on the spell KIND and is what
    // cspell_resolve_cast plays inside a fight. A spell cast in the field and the same spell cast in
    // combat do not make the same noise, and neither table is a fallback for the other.

    /// <summary>The cue a ranged-kind cast makes as the caster winds up.</summary>
    public const int RangedWindupCue = 0x12;

    /// <summary>The cue a melee-kind cast makes as the caster swings.</summary>
    public const int MeleeSwingCue = 0x13;

    /// <summary>The cue that plays when the target RESISTS the spell.</summary>
    /// <remarks>
    /// Fires on the resistance bitmap, which is a different table from the weakness one tested two
    /// lines above it, and fires whether or not the spell then does anything. So a resisted cast is
    /// audible even when it changes nothing — which is the point of it.
    /// </remarks>
    public const int ResistedCue = 0x3b;

    /// <summary>The cue the targeting-type-2 delivery makes — the pool move.</summary>
    public const int PoolDeliveryCue = 0x3f;

    /// <summary>Spell kinds that wind up like a bow rather than swinging.</summary>
    private static readonly int[] RangedKinds = { 0, 2, 3, 7, 8 };

    /// <summary>Spell kinds that make no cast noise at all.</summary>
    /// <remarks>
    /// Kind 5 lays a tile effect and kind 6 summons a creature. Both have their own case in the
    /// switch and neither plays anything — they are carved OUT of the default arm, which is what
    /// makes them silent rather than melee.
    /// </remarks>
    private static readonly int[] SilentKinds = { 5, 6 };

    /// <summary>
    /// The cue a cast makes in COMBAT, by spell kind — <c>cspell_resolve_cast</c> (CSPELL.C:1303).
    /// </summary>
    /// <param name="spellKind">The spell record's kind.</param>
    /// <param name="costWasNegated">Whether the cost arrived negative, i.e. this is a heal.</param>
    /// <returns>The sound id, or null for silence.</returns>
    /// <remarks>
    /// <b>A HEAL MAKES NO CAST NOISE AND PLAYS NO ANIMATION.</b> The whole switch sits inside
    /// <c>if (isNeg == 0)</c>, so the negated-cost branch skips the cue AND the wind-up or swing.
    /// A port that plays the cue first and animates second gets a healer who swings at their own
    /// side.
    ///
    /// <para><b>The default arm is MELEE, not silence.</b> Kinds 1 and 4 share it with
    /// <c>default</c>, so an unknown kind swings; only 5 and 6 are quiet, and they are quiet by
    /// having cases of their own. Reading the switch as "0/2/3/7/8 ranged, 1/4 melee, rest silent"
    /// inverts that for every kind above 8.</para>
    /// </remarks>
    public static int? ForCombatCast(int spellKind, bool costWasNegated) {
        if (costWasNegated || System.Array.IndexOf(SilentKinds, spellKind) >= 0) {
            return null;
        }

        return System.Array.IndexOf(RangedKinds, spellKind) >= 0 ? RangedWindupCue : MeleeSwingCue;
    }

}
