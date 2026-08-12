namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Combat arithmetic: to-hit, damage, and the shared damage-application pipeline.
///
/// <para>Ported from <c>SRC/COMBAT/STATS/CBSTAT.C</c>, <c>SRC/COMBAT/ENC/CBENC.C</c>,
/// <c>SRC/COMBAT/ACTOR/CACTOR.C</c> and <c>SRC/COMBAT/ARENA/COMBAT.C</c>, cross-read against
/// <c>docs/combat-formulas.md</c>. These decide the game's combat balance, so the integer truncation
/// is deliberate: the original works in 16-bit integers and divides at each step. Do not fold the
/// arithmetic together.</para>
///
/// <para>Everything here takes plain numbers rather than actor objects — there is no combat-actor
/// model yet, and keeping these pure makes every branch testable. TASK-94 supplies the state.</para>
/// </summary>
public static class CombatFormulas {
    /// <summary>Hit chance is clamped to this band, so nothing is ever a certain hit or a certain
    /// miss. 0x62 in the original.</summary>
    public const int MinHitChance = 2;

    /// <inheritdoc cref="MinHitChance"/>
    public const int MaxHitChance = 98;

    /// <summary>Added to the attacker's roll when the target is parrying, making it harder to hit.
    /// This is what the Defend command buys.</summary>
    public const int ParryRollPenalty = 20;

    /// <summary>Hit chance falls by this much per grid cell between shooter and target.</summary>
    private const int RangedPenaltyPerCell = 2;

    /// <summary>Item wear applied per event (see <c>docs/combat-formulas.md</c> §5).</summary>
    public const int ArmorWearOnMeleeHit = 256;

    /// <inheritdoc cref="ArmorWearOnMeleeHit"/>
    public const int ArmorWearOnRangedHit = 512;

    /// <inheritdoc cref="ArmorWearOnMeleeHit"/>
    public const int WeaponWearOnSwing = 256;

    /// <inheritdoc cref="ArmorWearOnMeleeHit"/>
    public const int WeaponWearOnThrust = 128;

    /// <summary>
    /// Scales a value by the blessing on an equipped item of the relevant category: +5% / +10% / +15%.
    /// </summary>
    /// <remarks>
    /// Tiers do <b>not</b> stack. The original tests them in ascending order and each assignment
    /// overwrites the last from the <i>original</i> value, so the highest bit present wins outright.
    /// It also considers only the first equipped item of the category and stops there.
    /// </remarks>
    public static int ApplyEquippedBlessing(int value, ItemFlags equippedItemFlags) {
        int result = value;
        if ((equippedItemFlags & ItemFlags.Blessed1) != 0) {
            result = value * 105 / 100;
        }
        if ((equippedItemFlags & ItemFlags.Blessed2) != 0) {
            result = value * 110 / 100;
        }
        if ((equippedItemFlags & ItemFlags.Blessed3) != 0) {
            result = value * 115 / 100;
        }
        return result;
    }

    /// <summary>
    /// The target's defence contribution to melee to-hit: a quarter of Defense, blessed by worn
    /// armour, clamped to [0, 98]. An actor that cannot act contributes nothing — being stunned or
    /// asleep makes you trivially hittable.
    /// </summary>
    public static int DefenseRating(int defense, bool canAct, ItemFlags equippedArmorFlags) {
        int value = canAct ? defense >> 2 : 0;
        value = ApplyEquippedBlessing(value, equippedArmorFlags);
        if (value > MaxHitChance) {
            value = MaxHitChance;
        }
        return value < 0 ? 0 : value;
    }

    /// <summary>
    /// The percentage of incoming damage worn armour absorbs: a quarter of Defense plus the first
    /// equipped armour's condition-scaled rating, the whole then scaled by the wearer's class
    /// affinity for that armour, capped at 98.
    /// </summary>
    /// <remarks>
    /// docs/combat-formulas.md described this as a sum over all equipped armour plus a racial term.
    /// It is neither: the original stops at the <b>first</b> equipped armour item, and the class
    /// modifier <b>multiplies</b> the running total rather than being added.
    /// </remarks>
    public static int ArmorRating(
        int defense, bool hasArmorEquipped, int armorConditionPercent, int armorRating, int classGroupModifier) {
        int result = defense >> 2;
        if (hasArmorEquipped) {
            result += armorConditionPercent * armorRating / 100;
            result = result * (classGroupModifier + 100) / 100;
            if (result > MaxHitChance) {
                result = MaxHitChance;
            }
        }
        return result;
    }

    /// <summary>
    /// Melee hit chance, before the roll. The weapon term is the weapon's accuracy field scaled by
    /// the attacker's class affinity for it and by its condition, so a worn or ill-suited weapon
    /// misses more.
    /// </summary>
    /// <param name="weaponAccuracy">The weapon's swing or thrust accuracy — the caller picks which,
    /// exactly as the original passes one or the other in.</param>
    /// <param name="classGroupModifier">Class-vs-weapon affinity, as a percentage delta.</param>
    /// <param name="weaponConditionPercent">Weapon condition, 0..100+.</param>
    /// <param name="weaponFlags">Flags of the equipped weapon, for the blessing bonus.</param>
    /// <param name="hasWeapon">Unarmed contributes no weapon term at all.</param>
    public static int MeleeHitChance(
        int accuracyMelee, bool hasWeapon, int weaponAccuracy, int classGroupModifier,
        int weaponConditionPercent, ItemFlags weaponFlags, int targetDefenseRating) {
        var weaponTerm = 0;
        if (hasWeapon) {
            weaponTerm = weaponAccuracy * (classGroupModifier + 100) / 100;
            weaponTerm = weaponTerm * weaponConditionPercent / 100;
        }

        int chance = accuracyMelee + weaponTerm;
        if (hasWeapon) {
            chance = ApplyEquippedBlessing(chance, weaponFlags);
        }
        chance -= targetDefenseRating;

        if (chance < MinHitChance) {
            chance = MinHitChance;
        }
        return chance > MaxHitChance ? MaxHitChance : chance;
    }

    /// <summary>
    /// Whether a melee attack lands. The parry penalty is added to the <i>roll</i>, not subtracted
    /// from the chance, so it is not clamped away by the 2..98 band.
    /// </summary>
    /// <param name="roll">A roll in [0, 100).</param>
    public static bool MeleeHits(int roll, int hitChance, bool targetParrying) {
        if (targetParrying) {
            roll += ParryRollPenalty;
        }
        return roll < hitChance;
    }

    /// <summary>
    /// Ranged hit chance. Distance is the only defensive term — <b>the target's defence is ignored
    /// entirely</b>, so armour never helps against arrows.
    /// </summary>
    /// <param name="baseSkill">AccuracyCrossbow for a shot, AccuracyCasting for a spell.</param>
    /// <param name="chebyshevDistance">Grid distance; adjacent (1) costs nothing.</param>
    /// <param name="ammoAccuracyBonus">The quarrel's accuracy field; 0 for a spell.</param>
    public static int RangedHitChance(int baseSkill, int chebyshevDistance, int ammoAccuracyBonus) {
        int range = chebyshevDistance - 1;
        if (range < 0) {
            range = 0;
        }
        int chance = baseSkill - range * RangedPenaltyPerCell + ammoAccuracyBonus;
        return chance < 0 ? 0 : chance;
    }

    /// <summary>Whether a ranged attack lands. Unlike melee this has no floor or ceiling, so a
    /// 0-chance shot always misses.</summary>
    /// <param name="roll">A roll in [0, 100).</param>
    public static bool RangedHits(int roll, int hitChance) => roll < hitChance;

    /// <summary>
    /// The damage a weapon's enchantment adds, before the target's protection against that element.
    /// </summary>
    /// <remarks>
    /// As with blessings, the tiers do not stack — the last matching flag wins. <paramref name="weaponBase"/>
    /// is the weapon's swing or thrust base, matching the attack being made.
    /// </remarks>
    public static int WeaponEnchantmentBonus(ItemFlags weaponFlags, int weaponBase) {
        var bonus = 0;
        if ((weaponFlags & ItemFlags.Poisoned) != 0) {
            bonus = 10;
        }
        if ((weaponFlags & ItemFlags.Flaming) != 0) {
            bonus = weaponBase * 75 / 100;
        }
        if ((weaponFlags & ItemFlags.SteelFired) != 0) {
            bonus = weaponBase;
        }
        if ((weaponFlags & ItemFlags.Frosted) != 0) {
            bonus = weaponBase >> 1;
        }
        if ((weaponFlags & ItemFlags.Enhanced1) != 0) {
            bonus = weaponBase << 1;
        }
        if ((weaponFlags & ItemFlags.Enhanced2) != 0) {
            bonus = weaponBase * 75 / 100;
        }
        return bonus;
    }

    /// <summary>
    /// Melee damage: Strength, plus the weapon's condition-scaled base, plus its enchantment.
    /// </summary>
    /// <param name="enchantmentBonus">From <see cref="WeaponEnchantmentBonus"/>, already reduced by
    /// the target's protection against that element.</param>
    /// <param name="doubled">The Guarda Revanche against a moredhel warrior or spellcaster.</param>
    /// <remarks>
    /// The upper cap of 255 is <b>CD-only</b> (<c>#ifdef V102CD</c>). We target the 1.02 CD build so
    /// it applies, but the 1.00 floppy has no ceiling here.
    /// </remarks>
    public static int MeleeDamage(
        int strength, bool hasWeapon, int weaponBase, int weaponConditionPercent,
        int enchantmentBonus, bool doubled) {
        int damage = strength;
        if (hasWeapon) {
            damage += weaponBase * weaponConditionPercent / 100;
        }
        damage += enchantmentBonus;
        if (doubled) {
            damage <<= 1;
        }
        if (damage < 1) {
            damage = 1;
        }
        return damage > 255 ? 255 : damage;
    }

    /// <summary>Thrown-rock projectile kind, damaged by a flat roll rather than by weapon stats.</summary>
    public const int ThrownRockKind = 8;

    /// <summary>The other flat-roll projectile kind.</summary>
    public const int FlatRollKind = 9;

    /// <summary>
    /// Ranged damage: crossbow base plus quarrel base, with <b>no Strength term</b> — a weak archer
    /// hits as hard as a strong one. Two projectile kinds ignore the weapon entirely and roll flat.
    /// </summary>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    /// <returns>-1 when the projectile kind has no quarrel record, as the original does.</returns>
    public static int RangedDamage(int projectileKind, int crossbowBase, int? quarrelBase, Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        return projectileKind switch {
            ThrownRockKind => 15 + rnd(20),  // 15..34
            FlatRollKind => 5 + rnd(7),      // 5..11
            _ => quarrelBase.HasValue ? crossbowBase + quarrelBase.Value : -1,
        };
    }

    /// <summary>
    /// The shared pipeline every confirmed hit runs through: armour, absorb shield, negation,
    /// creature weakness/resistance, then stamina before health.
    /// </summary>
    /// <param name="applyArmor">False for damage that bypasses armour (spell effects, stamina costs).</param>
    /// <param name="absorbPool">Remaining points of an active absorb shield, or null when none is up.
    /// Only consulted for <paramref name="fromDirectAttack"/> damage.</param>
    /// <param name="fromDirectAttack">The original's <c>source_type == 0</c>: shields and negation
    /// apply only to direct attacks.</param>
    /// <param name="weakToDamageType">Creature is weak to this damage type — takes half again as much.</param>
    /// <param name="resistsDamageType">Creature resists it — takes half.</param>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    public static DamageOutcome ApplyDamage(
        int damage, int stamina, int health, bool immune,
        bool applyArmor, int armorRating,
        int? absorbPool, bool fromDirectAttack, bool negated,
        bool weakToDamageType, bool resistsDamageType,
        Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (immune || damage < 1) {
            return DamageOutcome.NoEffect(stamina, health, absorbPool);
        }

        if (applyArmor) {
            damage = damage * (100 - armorRating) / 100;
            if (damage == 0) {
                // Armour never reduces a hit to nothing; a token 1-2 always gets through.
                damage = rnd(2) + 1;
            }
        }

        int? poolAfter = absorbPool;
        var shieldBroken = false;
        if (fromDirectAttack && absorbPool.HasValue) {
            int remaining = absorbPool.Value - damage;
            if (remaining >= 0) {
                return DamageOutcome.Absorbed(stamina, health, remaining);
            }
            // Overflow carries through and the shield breaks.
            damage = -remaining;
            poolAfter = null;
            shieldBroken = true;
        }

        if (fromDirectAttack && negated) {
            damage = 0;
        }

        if (weakToDamageType) {
            damage += damage >> 1;
        }
        if (resistsDamageType) {
            damage >>= 1;
        }

        // Stamina is the buffer; only the overflow reaches health.
        int staminaAfter = stamina;
        int healthAfter = health;
        if (stamina < damage) {
            int toHealth = damage - stamina;
            staminaAfter = 0;
            healthAfter = toHealth > health ? 0 : health - toHealth;
        } else {
            staminaAfter = stamina - damage;
        }

        return new DamageOutcome(damage, staminaAfter, healthAfter, poolAfter, shieldBroken, healthAfter <= 0);
    }
}

/// <summary>What one <see cref="CombatFormulas.ApplyDamage"/> call did.</summary>
public readonly struct DamageOutcome {
    internal DamageOutcome(int dealt, int stamina, int health, int? absorbPool, bool shieldBroken, bool died) {
        DamageDealt = dealt;
        Stamina = stamina;
        Health = health;
        AbsorbPool = absorbPool;
        ShieldBroken = shieldBroken;
        Died = died;
    }

    /// <summary>Damage after every modifier — the number the original floats above the target.
    /// Note it is the pre-split total, so a hit absorbed entirely by stamina still shows in full.</summary>
    public int DamageDealt { get; }

    /// <summary>Stamina after the hit.</summary>
    public int Stamina { get; }

    /// <summary>Health after the hit; 0 means dead.</summary>
    public int Health { get; }

    /// <summary>Remaining absorb-shield points, or null once the shield is gone.</summary>
    public int? AbsorbPool { get; }

    /// <summary>The absorb shield was exhausted by this hit and should be removed.</summary>
    public bool ShieldBroken { get; }

    /// <summary>Health reached 0.</summary>
    public bool Died { get; }

    // Died stays false: nothing happened, so a caller must not run death handling off this.
    internal static DamageOutcome NoEffect(int stamina, int health, int? absorbPool) =>
        new DamageOutcome(0, stamina, health, absorbPool, shieldBroken: false, died: false);

    internal static DamageOutcome Absorbed(int stamina, int health, int poolAfter) =>
        new DamageOutcome(0, stamina, health, poolAfter, shieldBroken: false, died: false);
}
