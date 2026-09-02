namespace GameData.Resources.Combat;

/// <summary>
/// The two cues a spell's projectile makes — <c>Spell_ApplyHitWithProjectile</c> @0x66ec9.
/// </summary>
/// <remarks>
/// <b>A flying spell sounds twice, and the two are the same shape as a crossbow's.</b> The routine
/// plays <see cref="LaunchCue"/> immediately before
/// <c>Combat_AnimateProjectileToTarget</c> and <see cref="ImpactCue"/> after it, inside the branch
/// the flight's own hit result guards. So the departure is heard whether or not the spell connects
/// and the arrival only when it does — see <see cref="RangedShotSound"/>, which is the same
/// division for a bolt.
///
/// <para><b>This is separate from the CAST cue.</b> <c>SpellCastSound.ForCombatCast</c> is the
/// caster's own noise, chosen by targeting type, and it plays for spells that fly nothing at all.
/// Folding the two together would give a ground-aimed spell a projectile's whistle.</para>
///
/// <para><b>Only a spell aimed at an ACTOR flies.</b> The routine takes a destination actor, and
/// <c>Cast_Spell</c> reaches it on the arm that has one; a ground- or crystal-aimed cast lands
/// without a projectile. That is the same condition the arena uses to arm the effect sprite, so the
/// sound and the picture agree by construction rather than by coincidence.</para>
/// </remarks>
public static class SpellProjectileSound {
    /// <summary><c>sound_arrow</c> (1) — the projectile leaving, heard on a miss too.</summary>
    public const int LaunchCue = 1;

    /// <summary><c>sound_arrowexp</c> (29) — the burst where it lands, only on a hit.</summary>
    /// <remarks>
    /// The same id a magic-bolt quarrel plays on impact
    /// (<see cref="RangedShotSound.MagicBoltCue"/>), which is consistent rather than coincidental:
    /// both are a magical projectile bursting on its target.
    /// </remarks>
    public const int ImpactCue = 29;

    /// <summary>Whether a cast flies a projectile at all.</summary>
    /// <param name="hasTargetActor">Whether the cast has a destination actor.</param>
    /// <param name="animationEffectType">The spell record's <c>AnimationEffectType</c>.</param>
    /// <remarks>
    /// <b>Both conditions, and the second is the one that is easy to miss.</b> These cues live
    /// inside <c>Spell_ApplyHitWithProjectile</c>, which <c>Spell_RunAnimationEffect</c> reaches on
    /// one arm of a 20-case switch — see <see cref="CombatEffectSprite.ProjectileAnimationType"/>.
    /// A spell with no destination actor flies nothing; so does a spell whose animation is a
    /// palette fade rather than a missile, and there are far more of the latter.
    /// </remarks>
    public static bool Flies(bool hasTargetActor, int animationEffectType) =>
        hasTargetActor && CombatEffectSprite.FliesProjectile(animationEffectType);
}
