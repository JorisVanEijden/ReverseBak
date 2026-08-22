namespace GameData.Resources.Combat;

/// <summary>
/// One ranged shot, from the trigger to the wear on the gear —
/// <c>combataiturn_ranged_attack</c> (canassa CBTAITRN.C:102).
///
/// <para>The formulas already existed (<see cref="CombatFormulas.RangedHitChance"/>,
/// <see cref="CombatFormulas.RangedDamage"/>); this is the join, the ranged counterpart to
/// <see cref="MeleeExchange"/>.</para>
/// </summary>
public static class RangedExchange {
    /// <summary>
    /// <b>The crossbow wears out on EVERY shot; armour only wears when the shot lands.</b>
    /// </summary>
    /// <remarks>
    /// The attacker's <c>cbstat_damage_equipped_items(attacker, 2, 0x100)</c> sits <b>outside</b> the
    /// hit branch — the last statement of the routine — while the target's
    /// <c>(target, 4, 0x200)</c> is inside it. So missing still costs the shooter condition on their
    /// weapon, which is the kind of asymmetry a port silently drops by putting both inside
    /// <c>if (hit)</c>.
    /// </remarks>
    public static bool WeaponWearsEvenOnAMiss => true;

    /// <inheritdoc cref="WeaponWearsEvenOnAMiss"/>
    public static bool ArmourWearsOnlyOnAHit => true;

    /// <summary>Equipment category worn by shooting — the crossbow.</summary>
    public const int ShooterWearCategory = 2;

    /// <summary>Equipment category worn by being hit — armour.</summary>
    public const int TargetWearCategory = 4;

    /// <summary>Wear severity applied to the shooter's weapon per shot.</summary>
    public const int ShooterWearSeverity = 0x100;

    /// <summary>Wear severity applied to the target's armour per hit.</summary>
    public const int TargetWearSeverity = 0x200;

    /// <summary>Base damage flags for a landed shot.</summary>
    public const int BaseDamageFlags = 0x540;

    /// <summary>
    /// How far a landed shot shoves the target, by quarrel kind.
    /// </summary>
    /// <remarks>
    /// <b>Kinds 4, 5 and 6 hit harder than the rest</b> — knockback 2 instead of 1, and they also set
    /// the low damage-flag bit. Every other kind (0-3, 7-9) knocks back 1. Reading the switch as
    /// "some kinds are special" and defaulting the rest to 0 would remove knockback from most shots.
    /// </remarks>
    public static int KnockbackFor(int quarrelKind) =>
        quarrelKind >= 4 && quarrelKind <= 6 ? 2 : 1;

    /// <summary>The damage flags a landed shot carries.</summary>
    /// <remarks>
    /// Base <see cref="BaseDamageFlags"/>, plus bit 0 for the heavier kinds, plus bit 3 for
    /// <see cref="StatusEffectQuarrelKind"/>.
    /// </remarks>
    public static int DamageFlagsFor(int quarrelKind) {
        int flags = BaseDamageFlags;
        if (KnockbackFor(quarrelKind) == 2) {
            flags |= 1;
        }
        if (quarrelKind == StatusEffectQuarrelKind) {
            flags |= 8;
        }
        return flags;
    }

    /// <summary>
    /// The one quarrel kind that applies a status effect rather than just damage.
    /// </summary>
    /// <remarks>
    /// Kind 3 alone runs a status-effect add/remove pair around the damage, raises the knockback
    /// flag on the target and fires a particle burst. <b>What the effect IS is not established</b> —
    /// the call is <c>cspell_status_effect_add(target, 4, 0, 0, 0)</c>, and effect 4 has not been
    /// identified here.
    /// </remarks>
    public const int StatusEffectQuarrelKind = 3;

    /// <summary>Whether this kind applies the status effect.</summary>
    public static bool AppliesStatusEffect(int quarrelKind) => quarrelKind == StatusEffectQuarrelKind;

    /// <summary>
    /// <b>Crossbow skill is paid twice, exactly as melee is.</b>
    /// </summary>
    /// <remarks>
    /// <c>stat_combatant_modify(attacker, 5, 1, 3)</c> runs before the roll and <b>again on a hit</b>
    /// — so a shot that connects advances the stat twice. Awarding only on a hit would halve the
    /// shooter's progression; awarding only once would halve it differently. Mirrors
    /// <see cref="CombatAdvancement.OnMeleeDeclared"/> and <see cref="CombatAdvancement.OnMeleeHit"/>.
    /// </remarks>
    public static int SkillAwards(bool hit) => hit ? 2 : 1;

    /// <summary>
    /// The stat the hit check reads: Crossbow plus an armour-derived modifier.
    /// </summary>
    /// <remarks>
    /// <c>stat_actor_get(attacker, 5, 0) + combataiturn_armor_eff_stat(attacker)</c> — so what the
    /// shooter is WEARING changes their accuracy, not just their skill. The armour term is computed
    /// by a helper this model does not port; it is passed in.
    /// </remarks>
    public static int EffectiveSkill(int crossbowSkill, int armourModifier) =>
        crossbowSkill + armourModifier;
}
