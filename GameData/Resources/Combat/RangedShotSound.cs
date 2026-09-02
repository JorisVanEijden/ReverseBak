namespace GameData.Resources.Combat;

/// <summary>
/// The cue a ranged attack plays — <c>resolveRangedAttack</c> @0x66114.
/// </summary>
/// <remarks>
/// <b>Two cues, and they are not the same event.</b> A thrown rock plays its cue AFTER the
/// projectile's flight — an impact — while a crossbow plays one BEFORE it, a firing. In the original
/// they sit on opposite sides of <c>Combat_AnimateProjectileToTarget</c>. Reading them as "the sound
/// a shot makes" collapses an impact and a discharge into one thing.
///
/// <para><b>Both play whether or not the shot HITS.</b> The to-hit roll happens first and its two
/// branches converge before the cue: a miss still animates and still sounds, only with a different
/// projectile step. So a cue behind a hit test is wrong in the common case — most shots miss.</para>
///
/// <para><b>A thrown weapon with no crossbow equipped is SILENT.</b> The crossbow cue is gated on
/// <c>getObjectInfoFromActor(attacker, ObjectType.Crossbow)</c> returning a record — the attacker
/// must actually be holding one. Playing it unconditionally gives a bowstring twang to a hurled
/// rock.</para>
/// </remarks>
public static class RangedShotSound {
    /// <summary>The quarrel kind that is a thrown rock rather than a shot bolt.</summary>
    /// <remarks>
    /// The routine branches on this value alone (<c>cmp [bp+arg_4], 8</c>), before it looks at the
    /// weapon at all.
    /// </remarks>
    public const int ThrownRockKind = 8;

    /// <summary><c>sound_rockhit</c> (73) — the rock landing, played after its flight.</summary>
    public const int RockImpactCue = 73;

    /// <summary><c>sound_crossbow</c> (68) — the discharge, played before the bolt flies.</summary>
    public const int CrossbowFiringCue = 68;

    /// <summary>
    /// The cue for one ranged attack, or <c>null</c> for silence.
    /// </summary>
    /// <param name="quarrelKind">The ammunition kind; <see cref="ThrownRockKind"/> is the rock.</param>
    /// <param name="attackerHasCrossbow">
    /// Whether the attacker has an <c>ObjectType.Crossbow</c> equipped — the original's
    /// <c>getObjectInfoFromActor(attacker, 2) != null</c>.
    /// </param>
    /// <remarks>
    /// <b>The rock ignores the weapon entirely.</b> Its branch is taken on the kind before the
    /// crossbow is consulted, so a rock thrown by someone holding a crossbow still plays the impact
    /// and never the discharge.
    /// </remarks>
    public static int? Cue(int quarrelKind, bool attackerHasCrossbow) {
        if (quarrelKind == ThrownRockKind) {
            return RockImpactCue;
        }
        return attackerHasCrossbow ? CrossbowFiringCue : (int?)null;
    }

    /// <summary>
    /// <b>The cue plays after the to-hit roll and before damage</b>, on both branches.
    /// </summary>
    /// <remarks>
    /// Stated as a constant because the placement is the part a port gets wrong: it is tempting to
    /// play a "hit" sound on a hit, and <c>rockhit</c>'s name invites exactly that. It is the rock
    /// striking the ground, not the target.
    /// </remarks>
    public static bool PlaysOnAMissToo => true;

    /// <summary><c>sound_staff2</c> (66) — the impact, on every shot that CONNECTS.</summary>
    /// <remarks>
    /// A third cue, and it is the one a port is most likely to miss: <see cref="Cue"/>'s two are
    /// both about the shot LEAVING, and this is about it arriving. In the original it sits after
    /// <c>calculateRangedDamage</c>, inside the branch the to-hit roll guards
    /// (<c>var_6 != 0</c>), so it plays on a hit and only on a hit — the mirror of
    /// <see cref="PlaysOnAMissToo"/>.
    ///
    /// <para><b>The name is misleading and the id is not.</b> <c>staff2</c> reads as a melee staff
    /// sound; the routine pushing it is the ranged one, and id 66 is what a landing bolt plays.
    /// Named here after the event rather than after the resource.</para>
    /// </remarks>
    public const int ImpactCue = 66;

    /// <summary>The quarrel kind that is a magic bolt.</summary>
    /// <remarks>
    /// Kind 3 does more than damage: the original applies <b>Flamecast</b> to the target
    /// (<c>ApplySpellToActor(target, SpellNumber 4, cost 0)</c>) and tints the particles before
    /// playing <see cref="MagicBoltCue"/>. <b>The port plays the cue and does NOT apply the
    /// spell</b> — that half wants the spell-effect dispatcher, so the sound is faithful and the
    /// effect is a known gap rather than a silent one.
    /// </remarks>
    public const int MagicBoltKind = 3;

    /// <summary><c>sound_arrowexp</c> (29) — the magic bolt's burst, after its effect lands.</summary>
    public const int MagicBoltCue = 29;

    /// <summary>
    /// The cues a shot that CONNECTS plays, in order — <see cref="ImpactCue"/> always, then
    /// <see cref="MagicBoltCue"/> for a magic bolt.
    /// </summary>
    /// <remarks>
    /// Two rather than one because the magic bolt plays BOTH: the impact cue is pushed before the
    /// kind is examined, and the burst after the spell is applied. Returning a single "best" cue
    /// would drop one of the two sounds the original makes.
    /// </remarks>
    public static System.Collections.Generic.IEnumerable<int> HitCues(int quarrelKind) {
        yield return ImpactCue;
        if (quarrelKind == MagicBoltKind) {
            yield return MagicBoltCue;
        }
    }
}
