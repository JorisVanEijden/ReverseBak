namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;

using System;
using System.Collections.Generic;

using Xunit;

/// <summary>
/// The end-of-turn poison tick (<c>combat_arena_actor_poison_tick</c>).
/// </summary>
public class PoisonTickTests {
    // Returns the queued values in order, then throws — an exhausted queue must fail the test
    // rather than quietly fall back to a default, which is how a fixture ends up measuring itself.
    private static Func<int, int> Rolls(params int[] values) {
        var queue = new Queue<int>(values);
        return _ => queue.Count > 0
            ? queue.Dequeue()
            : throw new InvalidOperationException("the routine drew more randoms than the test queued");
    }

    private static Combatant Poisoned(int health = 20, int stamina = 10) => new Combatant {
        Health = health,
        Stamina = stamina,
        Flags = CombatantFlags.Ready | CombatantFlags.Poisoned,
    };

    [Fact]
    public void TheRollIsOneOrTwo_NeverZero() {
        // RND2(2) + 1 over a rnd returning [0, n): the two reachable values are 1 and 2.
        foreach ((int roll, int expected) in new[] { (0, 1), (1, 2) }) {
            Combatant actor = Poisoned();
            PoisonTick.Result result = PoisonTick.Apply(actor, Rolls(roll));
            Assert.Equal(expected, result.Damage);
        }
    }

    [Fact]
    public void StaminaAbsorbsBeforeHealth() {
        Combatant actor = Poisoned(health: 20, stamina: 10);

        PoisonTick.Apply(actor, Rolls(1));

        Assert.Equal(8, actor.Stamina);
        Assert.Equal(20, actor.Health);
    }

    [Fact]
    public void AnUnpoisonedCombatantIsUntouched() {
        var actor = new Combatant { Health = 20, Stamina = 10, Flags = CombatantFlags.Ready };

        // No roll queued: a tick that drew one would throw, so this also proves the routine
        // short-circuits before consuming from the random stream. Order in a shared stream is
        // observable — a stray draw shifts every later roll in the fight.
        PoisonTick.Result result = PoisonTick.Apply(actor, Rolls());

        Assert.Equal(0, result.Damage);
        Assert.Equal(10, actor.Stamina);
    }

    [Fact]
    public void ACorpseIsNotTicked() {
        Combatant actor = Poisoned(health: 1, stamina: 0);
        actor.Flags |= CombatantFlags.Dead;

        PoisonTick.Result result = PoisonTick.Apply(actor, Rolls());

        Assert.Equal(0, result.Damage);
        Assert.False(result.Died);
        // Poison cannot finish off someone already killed earlier in the round — the original tests
        // CAF_DEAD before rolling.
        Assert.Equal(1, actor.Health);
    }

    [Fact]
    public void PoisonCanKill() {
        Combatant actor = Poisoned(health: 1, stamina: 0);

        PoisonTick.Result result = PoisonTick.Apply(actor, Rolls(1));

        Assert.True(result.Died);
        Assert.True(actor.Health <= 0);
    }

    [Fact]
    public void AnAbsorbShieldSoaksIt() {
        // Poison is source_type == 0, so shields apply to it.
        Combatant actor = Poisoned(health: 20, stamina: 10);

        PoisonTick.Result result = PoisonTick.Apply(actor, Rolls(1), absorbPool: 50);

        Assert.Equal(10, actor.Stamina);
        Assert.Equal(20, actor.Health);
        Assert.True(result.AbsorbPool < 50, "the shield must have taken the points instead");
    }

    [Fact]
    public void TheAlwaysActsClassIsImmune() {
        Combatant actor = Poisoned(health: 20, stamina: 10);
        actor.ClassId = CombatEncounter.AlwaysActsClassId;

        PoisonTick.Result result = PoisonTick.Apply(actor, Rolls(1));

        Assert.Equal(0, result.Damage);
        Assert.Equal(10, actor.Stamina);
    }

    [Fact]
    public void PickNextDoesNotTick_TheTurnLoopOwnsIt() {
        // A fence on the split, not on behaviour. The original ticks poison inside its pick-next;
        // ours deliberately does not, so that the loop can act on a poison death before handing out
        // a turn. If someone "helpfully" moves the tick into PickNext, this goes red and they read
        // why in PoisonTick's summary.
        Combatant poisoned = Poisoned(health: 20, stamina: 10);
        // Fast enough to be picked, or it never becomes Current and the second call has no outgoing
        // actor to tick — the assertion would then hold for the wrong reason.
        poisoned.Speed = 5;
        // PartySlot must be non-zero or PartyAlive() counts nobody (the 1.02 CD rule) and the
        // encounter reports itself over before anyone is picked.
        poisoned.PartySlot = 1;
        var encounter = new CombatEncounter();
        encounter.Party.Add(poisoned);
        encounter.Enemies.Add(new Combatant { Health = 5, Stamina = 5, Speed = 1, Flags = CombatantFlags.Ready });

        Assert.Same(poisoned, encounter.PickNext());
        encounter.PickNext();

        Assert.Equal(10, poisoned.Stamina);
        Assert.Equal(20, poisoned.Health);
    }
}
