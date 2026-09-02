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
    /// <summary>
    /// Cues for spells cast <b>on the field</b> — reached only by <c>Cast_field_spell</c> @0x6ca30.
    /// </summary>
    /// <remarks>
    /// <b>The split is from a caller sweep, not from the spells' natures.</b> Every routine behind
    /// these entries has exactly one caller and it is the field dispatcher; their <c>j_</c> thunks
    /// have none at all. See <see cref="CombatSounds"/> for the other half and why the two were
    /// separated.
    /// </remarks>
    private static readonly Dictionary<int, int> FieldSounds = new Dictionary<int, int> {
        { FieldSpells.DragonsBreath, FieldSpells.CreationSound },
        { FieldSpells.ScentOfSarig, FieldSpells.ScentSound },
        { FieldSpells.EyesOfIshap, FieldSpells.ScentSound },
        { FieldSpells.TheUnseen, 13 },              // sound_spell3
        { FieldSpells.NacreCicatrix, 13 },
        { FieldSpells.Union, FieldSpells.GeneralSound },
        { FieldSpells.AndTheLightShallLie, FieldSpells.GeneralSound },
        { SpellIds.CandleGlow, FieldSpells.CreationSound },
        { SpellIds.Stardusk, FieldSpells.CreationSound },
    };

    /// <summary>
    /// Cues for spells cast <b>in combat</b> — reached only by <c>Cast_Spell</c> @0x6850c.
    /// </summary>
    /// <remarks>
    /// <b>These were all on the field path until 2026-09-02, so they sounded where the original is
    /// silent and were silent where it is not.</b> The sweep that separated them: every caller of
    /// <c>Cast_Spell</c> is a combat routine — <c>combat_arena_resume_dispatch</c>,
    /// <c>combat_arena_resolve_menu_action</c>, <c>castCombatSpell</c> and five <c>monster_*</c>
    /// casters — and no field routine reaches it, while <c>Cast_field_spell</c> reaches none of
    /// these. Neither set is a judgement call about what a spell "is".
    ///
    /// <para>The first seven are dedicated routines <c>Cast_Spell</c> calls; the last four are
    /// pushed inline by its own switch, whose case labels are true spell numbers (IDA reports
    /// lowcase 3 against the <c>mov bx,[bp+SpellNumber]</c> at 0x68798).</para>
    ///
    /// <para><b>Flamecast is deliberately absent.</b> <c>Cast_Flamecast</c> @0x2fb80 has one caller
    /// and it is <c>cannon_rayStopsAtCell</c> — the cannon ray, not a cast. Its cue (1,
    /// <c>sound_arrow</c>) was in the old table and looked right only by coincidence: a player's
    /// Flamecast does play cue 1, but through <c>Spell_ApplyHitWithProjectile</c>, which is already
    /// ported as <see cref="Combat.SpellProjectileSound.LaunchCue"/>. Keeping the entry would double
    /// the sound and misattribute it.</para>
    /// </remarks>
    private static readonly Dictionary<int, int> CombatSounds = new Dictionary<int, int> {
        { SpellIds.BlackNimbus, 1 },        // sound_arrow
        { SpellIds.Steelfire, FieldSpells.CreationSound },
        { SpellIds.StrengthDrain, 63 },     // sound_heal
        { SpellIds.MadGodsRage, 78 },       // sound_quake
        { SpellIds.WindsOfEortis, 79 },     // sound_wind
        { SpellIds.Nightfingers, FieldSpells.GeneralSound },
        { SpellIds.Invitation, FieldSpells.GeneralSound },

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

    /// <summary>The cue for casting a spell <b>on the field</b>, or null when there is none.</summary>
    public static int? ForCast(int spellId) =>
        FieldSounds.TryGetValue(spellId, out int sound) ? sound : (int?)null;

    /// <summary>The cue for casting a spell <b>in combat</b>, or null when there is none.</summary>
    /// <param name="spellId">The spell being cast.</param>
    /// <param name="targetIsSusceptible">
    /// Whether the target is one Grief of 1000 Nights works on —
    /// <c>SpellPerSpellHandlers.GriefAffects</c>. <b>Only Grief consults it</b>, and it is a
    /// parameter rather than a table column because it is the one cue in either set whose case is
    /// guarded: <c>Cast_Spell</c>'s case 13 plays it behind
    /// <c>Grief_TargetIsSusceptible(target)</c>. Pass true for every other spell.
    /// </param>
    public static int? ForCombatSpell(int spellId, bool targetIsSusceptible = true) {
        if (spellId == SpellIds.GriefOfAThousandNights && !targetIsSusceptible) {
            return null;
        }
        return CombatSounds.TryGetValue(spellId, out int sound) ? sound : (int?)null;
    }

    /// <summary>Whether this spell's cast audio has been established either way.</summary>
    /// <remarks>
    /// True for a spell with a cue AND for one confirmed silent. False means nobody has looked, which
    /// is a different thing from "it makes no sound" and is worth being able to ask.
    /// </remarks>
    public static bool IsEstablished(int spellId) =>
        FieldSounds.ContainsKey(spellId) || CombatSounds.ContainsKey(spellId)
        || Silent.Contains(spellId);

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
