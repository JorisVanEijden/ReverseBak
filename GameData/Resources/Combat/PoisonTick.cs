namespace GameData.Resources.Combat;

using System;

/// <summary>
/// The poison damage a combatant takes at the end of its own turn
/// (<c>combat_arena_actor_poison_tick</c>, canassa COMBAT.C:205).
///
/// <para><b>Poison is paid per TURN, not per round.</b> The original ticks it inside
/// <c>combat_actor_pick_next</c> (@0x5e209) on the OUTGOING actor, before scanning for who acts
/// next — so a fast combatant that acts twice as often is poisoned twice as often, and a combatant
/// that never gets a turn never takes any. A port that ticks poison once per round for everyone
/// changes how deadly it is, in a way that scales with the speed spread of the fight.</para>
///
/// <para><b>Ordering matters and is not expressed here.</b> <see cref="CombatEncounter.PickNext"/>
/// deliberately does not call this: the encounter is a rules object and the turn loop owns the
/// sequence. The loop must tick the outgoing actor BEFORE picking, because poison can kill —
/// and a combatant that dies to its own poison must not then be handed a turn.</para>
/// </summary>
public static class PoisonTick {
    /// <summary>What one tick did.</summary>
    public readonly struct Result {
        public Result(int damage, int? absorbPool, bool died) {
            Damage = damage;
            AbsorbPool = absorbPool;
            Died = died;
        }

        /// <summary>Points actually removed from stamina and health. Zero when nothing was taken.</summary>
        public int Damage { get; }

        /// <summary>The absorb shield's remaining points after the tick, or null when none was up.</summary>
        public int? AbsorbPool { get; }

        /// <summary>Health reached zero on this tick.</summary>
        public bool Died { get; }

        public static readonly Result None = new Result(0, null, false);
    }

    /// <summary>
    /// Ticks poison on one combatant, writing the result into its stamina and health.
    /// </summary>
    /// <param name="actor">The combatant whose turn just ended.</param>
    /// <param name="rnd">Returns a value in <c>[0, n)</c> — the encounter's random source.</param>
    /// <param name="absorbPool">Remaining points of an active absorb shield, or null when none.</param>
    /// <param name="weakToDamageType">Creature is weak to poison.</param>
    /// <param name="resistsDamageType">Creature resists poison.</param>
    /// <remarks>
    /// <b>1 or 2 points</b> — <c>RND2(2) + 1</c>, a flat roll with no stat, weapon or level input.
    ///
    /// <para><b>Armour does not apply.</b> <c>combat_arena_apply_damage</c> has no armour step at
    /// all; the armour reduction lives on the melee path. So plate is no defence against poison,
    /// which is easy to get wrong by routing this through the attack pipeline.</para>
    ///
    /// <para>It IS direct-attack damage for the purposes of shields and negation
    /// (<c>source_type == 0</c>), so an absorb shield soaks it.</para>
    ///
    /// <para><see cref="CombatEncounter.AlwaysActsClassId"/> is immune, as it is to all damage.</para>
    /// </remarks>
    public static Result Apply(Combatant actor, Func<int, int> rnd, int? absorbPool = null,
        bool weakToDamageType = false, bool resistsDamageType = false) {
        if (actor == null) {
            throw new ArgumentNullException(nameof(actor));
        }
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }

        // Both conditions are the original's, in its order: poisoned, and not already dead. A corpse
        // is not ticked — which is why poison cannot "finish off" someone killed earlier in the round.
        if ((actor.Flags & CombatantFlags.Poisoned) == 0 || actor.IsDead) {
            return Result.None;
        }

        int rolled = rnd(2) + 1;

        DamageOutcome outcome = CombatFormulas.ApplyDamage(
            rolled, actor.Stamina, actor.Health,
            immune: actor.ClassId == CombatEncounter.AlwaysActsClassId,
            applyArmor: false, armorRating: 0,
            absorbPool: absorbPool, fromDirectAttack: true, negated: false,
            weakToDamageType: weakToDamageType, resistsDamageType: resistsDamageType,
            rnd: rnd);

        int before = actor.Health + actor.Stamina;
        actor.Stamina = outcome.Stamina;
        actor.Health = outcome.Health;
        int taken = before - (actor.Health + actor.Stamina);

        return new Result(taken < 0 ? 0 : taken, outcome.AbsorbPool, actor.Health <= 0);
    }
}
