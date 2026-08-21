namespace GameData.Resources.Combat;

using System;

/// <summary>
/// One melee swing resolved end to end: roll to hit, roll damage, apply it, and report whether the
/// target went down.
///
/// <para>The pieces already existed — <see cref="CombatFormulas.MeleeHitChance"/>,
/// <see cref="CombatFormulas.MeleeHits"/>, <see cref="CombatFormulas.MeleeDamage"/> and
/// <see cref="CombatFormulas.ApplyDamage"/> — but nothing joined them, so a fight could be entered
/// and stepped through and never actually hurt anyone. This is the join, and it is the smallest
/// thing that lets an encounter reach an end.</para>
///
/// <para><b>Deliberately not a turn.</b> It does not spend the actor's turn, move anyone, animate
/// anything or choose a target; the turn loop and the AI own those. It answers one question: what
/// happens when this attacker swings at that defender.</para>
/// </summary>
public static class MeleeExchange {
    /// <summary>What one swing did.</summary>
    public readonly struct Result {
        public Result(bool hit, int damage, bool defenderDown) {
            Hit = hit;
            Damage = damage;
            DefenderDown = defenderDown;
        }

        /// <summary>Whether the swing landed at all.</summary>
        public bool Hit { get; }

        /// <summary>Damage actually taken off, after armour and absorption — not the roll.</summary>
        public int Damage { get; }

        /// <summary>Whether this swing put the defender down.</summary>
        public bool DefenderDown { get; }

        public static Result Miss => new Result(false, 0, false);
    }

    /// <summary>What the attacker brings to a swing, beyond the combatant itself.</summary>
    public readonly struct Attacker {
        public Attacker(int accuracyMelee, int strength, bool hasWeapon = false, int weaponAccuracy = 0,
            int weaponBase = 0, int classGroupModifier = 0, int weaponConditionPercent = 100,
            ItemFlags weaponFlags = default) {
            AccuracyMelee = accuracyMelee;
            Strength = strength;
            HasWeapon = hasWeapon;
            WeaponAccuracy = weaponAccuracy;
            WeaponBase = weaponBase;
            ClassGroupModifier = classGroupModifier;
            WeaponConditionPercent = weaponConditionPercent;
            WeaponFlags = weaponFlags;
        }

        public int AccuracyMelee { get; }
        public int Strength { get; }
        public bool HasWeapon { get; }
        public int WeaponAccuracy { get; }
        public int WeaponBase { get; }
        public int ClassGroupModifier { get; }
        public int WeaponConditionPercent { get; }
        public ItemFlags WeaponFlags { get; }
    }

    /// <summary>What the defender brings.</summary>
    public readonly struct Defender {
        public Defender(int defenseRating, int armorRating = 0, bool immune = false,
            bool applyArmor = true, int? absorbPool = null) {
            DefenseRating = defenseRating;
            ArmorRating = armorRating;
            Immune = immune;
            ApplyArmor = applyArmor;
            AbsorbPool = absorbPool;
        }

        public int DefenseRating { get; }
        public int ArmorRating { get; }
        public bool Immune { get; }
        public bool ApplyArmor { get; }
        public int? AbsorbPool { get; }
    }

    /// <summary>
    /// Resolve one swing against a live combatant, writing the result onto it.
    /// </summary>
    /// <param name="rnd">
    /// <c>rnd(n)</c> returns a value in <c>[0, n)</c>. Used for the to-hit roll and, inside
    /// <see cref="CombatFormulas.ApplyDamage"/>, for the armour floor. <b>The damage roll is not a
    /// roll</b> — <see cref="CombatFormulas.MeleeDamage"/> is deterministic in strength, weapon and
    /// enchantment, so a swing that lands does a fixed amount before armour.
    /// </param>
    /// <remarks>
    /// <b>A dead or absent defender is a miss, not an exception.</b> Targets die between a decision
    /// and its execution — the AI picks in one step and the swing lands in another — so hitting a
    /// corpse has to be an ordinary outcome.
    ///
    /// <para><b>Parry is read off the defender's own flags</b> rather than passed in, because the
    /// penalty applies to the ROLL and not to the chance (see
    /// <see cref="CombatFormulas.MeleeHits"/>); routing it through the caller invites someone to
    /// subtract it from the chance instead, where the 2..98 clamp would swallow it.</para>
    /// </remarks>
    public static Result Resolve(Combatant attacker, Combatant defender,
        Attacker attackerStats, Defender defenderStats, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        if (attacker == null || defender == null || defender.IsDead) {
            return Result.Miss;
        }

        int chance = CombatFormulas.MeleeHitChance(
            attackerStats.AccuracyMelee, attackerStats.HasWeapon, attackerStats.WeaponAccuracy,
            attackerStats.ClassGroupModifier, attackerStats.WeaponConditionPercent,
            attackerStats.WeaponFlags, defenderStats.DefenseRating);

        bool parrying = (defender.Flags & CombatantFlags.Parry) != 0;
        if (!CombatFormulas.MeleeHits(rnd(100), chance, parrying)) {
            return Result.Miss;
        }

        int enchantment = attackerStats.HasWeapon
            ? CombatFormulas.WeaponEnchantmentBonus(attackerStats.WeaponFlags, attackerStats.WeaponBase)
            : 0;
        int rolled = CombatFormulas.MeleeDamage(
            attackerStats.Strength, attackerStats.HasWeapon, attackerStats.WeaponBase,
            attackerStats.WeaponConditionPercent, enchantment, doubled: false);

        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            rolled, defender.Stamina, defender.Health, defenderStats.Immune,
            defenderStats.ApplyArmor, defenderStats.ArmorRating, defenderStats.AbsorbPool,
            fromDirectAttack: true, negated: false,
            weakToDamageType: false, resistsDamageType: false, rnd);

        int before = defender.Health + defender.Stamina;
        defender.Stamina = outcome.Stamina;
        defender.Health = outcome.Health;
        int taken = before - (defender.Health + defender.Stamina);

        return new Result(true, taken < 0 ? 0 : taken, defender.Health <= 0);
    }
}
