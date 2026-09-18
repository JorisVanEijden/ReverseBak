namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which way a combatant faces — set when the arena is laid out, changed by the turn and melee rules
/// (canassa COMBAT.C:135-146, CBENC.C:993 combatenc_actor_face_target, COMBAT.C:480-490).
/// </summary>
public class CombatEncounterFacingTests {
    [Fact]
    public void TheArenaDeploysThePartyFacingInAndTheEnemiesFacingThem() {
        // COMBAT.C:135 gives the party R3D_DEG(90) = 0x4000 -> octant 4 (their backs to the camera),
        // and COMBAT.C:146 gives every enemy 0 -> octant 0 (facing the camera, i.e. the party).
        var encounter = new CombatEncounter();
        encounter.Party.Add(new Combatant { PartySlot = 1, FacingOctant = 2 });
        encounter.Enemies.Add(new Combatant { FacingOctant = 5 });

        encounter.DeployFacings();

        Assert.Equal(4, encounter.Party[0].FacingOctant);
        Assert.Equal(0, encounter.Enemies[0].FacingOctant);
    }

    [Fact]
    public void FaceTarget_TurnsTowardTheTarget() {
        var encounter = new CombatEncounter();
        var actor = new Combatant { PartySlot = 1, X = 3, Y = 3 };
        var target = new Combatant { X = 5, Y = 3 };          // +column -> 2
        var nearer = new Combatant { X = 3, Y = 4 };          // +row, closer -> 4
        encounter.Party.Add(actor);
        encounter.Enemies.Add(target);
        encounter.Enemies.Add(nearer);
        actor.Target = target;

        encounter.FaceTarget(actor);

        Assert.Equal(2, actor.FacingOctant);
    }

    [Fact]
    public void FaceTarget_WithNoTarget_TurnsTowardTheNearestLivingOpponent() {
        var encounter = new CombatEncounter();
        var enemy = new Combatant { X = 3, Y = 0, FacingOctant = 4 };
        var dead = new Combatant { X = 4, Y = 1, Flags = CombatantFlags.Dead };
        var near = new Combatant { PartySlot = 1, X = 2, Y = 2 };    // (-1,+2) from the enemy -> 5
        var far = new Combatant { PartySlot = 2, X = 7, Y = 7 };
        encounter.Enemies.Add(enemy);
        encounter.Party.Add(dead);
        encounter.Party.Add(near);
        encounter.Party.Add(far);

        encounter.FaceTarget(enemy);

        Assert.Equal(5, enemy.FacingOctant);
    }

    [Fact]
    public void ADeadCombatantDoesNotTurn() {
        // anim0_if_not_dead returns first for CAF_DEAD.
        var encounter = new CombatEncounter();
        var corpse = new Combatant { X = 0, Y = 0, Flags = CombatantFlags.Dead, FacingOctant = 0 };
        encounter.Enemies.Add(corpse);
        encounter.Party.Add(new Combatant { PartySlot = 1, X = 1, Y = 0 });

        encounter.FaceTarget(corpse);

        Assert.Equal(0, corpse.FacingOctant);
    }

    [Fact]
    public void AnEvenOnlyTurnRoundsADiagonalDown() {
        // dir_mode 3 in combat_actor_anim_play (CACTOR.C:1865): facing - facing % 2. The parry
        // sprites are played that way, so a parrying defender squares up to the octant below.
        var actor = new Combatant { X = 0, Y = 0 };
        var other = new Combatant { X = 1, Y = 1 };          // diagonal -> 3

        CombatEncounter.FaceToward(actor, other, evenOnly: true);

        Assert.Equal(2, actor.FacingOctant);
    }
}
