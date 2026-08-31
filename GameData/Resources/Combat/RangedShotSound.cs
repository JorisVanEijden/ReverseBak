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
}
