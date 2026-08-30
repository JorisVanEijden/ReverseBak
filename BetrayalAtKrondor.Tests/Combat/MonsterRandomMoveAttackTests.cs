namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The wander-then-act routine — <c>combataiact_random_move_attack</c> (CBTAIACT.C:39).
/// </summary>
/// <remarks>
/// <b>Written against a second model of this routine.</b> MonsterRandomMoveAttack and
/// MonsterTurnRoutines.AfterWandering both described it, with the same thresholds under different
/// names, and only MonsterTurnRoutines had a caller (MonsterTurnResolver dispatches
/// WalkRandomTileThenAttackOrBrace to it). These moved onto the survivor.
///
/// <para>The duplicate was not useless: it had IDENTIFIED the post-move guard's flag as CAF_DEAD
/// where the survivor said only "the creature's second flag". That identification was carried
/// across before deleting it — a duplicate can hold the better note as easily as the worse one.</para>
/// </remarks>
public class MonsterRandomMoveAttackTests {
    [Fact]
    public void AdjacentMeansSwing_WhateverTheRoll() {
        // The distance test comes first; no roll can talk it out of a melee it can reach.
        Assert.Equal(MonsterMove.Melee, MonsterTurnRoutines.AfterWandering(
            distanceToNearest: 1, roll: 99, halfStat: 50, lineOfFireClear: true).Move);
        Assert.Equal(MonsterMove.Melee, MonsterTurnRoutines.AfterWandering(
            distanceToNearest: 1, roll: 0, halfStat: 50, lineOfFireClear: true).Move);
    }

    [Fact]
    public void ONERollDrivesTHREEOutcomes() {
        // Under 0x32 the alternate spell, 0x32..0x4F the default one, 0x50 and over gives up and
        // defends. Rolling separately for "do I cast" and "which spell" changes the distribution
        // even with these same thresholds.
        MonsterTurn low = MonsterTurnRoutines.AfterWandering(5, 0x31, 50, true);
        MonsterTurn mid = MonsterTurnRoutines.AfterWandering(5, 0x32, 50, true);
        MonsterTurn high = MonsterTurnRoutines.AfterWandering(5, 0x50, 50, true);

        Assert.Equal(MonsterMove.Cast, low.Move);
        Assert.Equal(MonsterMove.Cast, mid.Move);
        Assert.NotEqual(low.SpellKind, mid.SpellKind);
        Assert.Equal(MonsterMove.Defend, high.Move);
    }

    [Fact]
    public void AHalfStatOfExactlyOneNEVERCasts() {
        // The guard tests inequality, not a minimum, so the weakest casters defend instead.
        // Reading it as "needs at least 1" lets them cast.
        Assert.Equal(MonsterMove.Defend, MonsterTurnRoutines.AfterWandering(
            distanceToNearest: 5, roll: 0, halfStat: 1, lineOfFireClear: true).Move);
    }

    [Fact]
    public void NoLineOfFireMeansNoCast() {
        Assert.Equal(MonsterMove.Defend, MonsterTurnRoutines.AfterWandering(
            distanceToNearest: 5, roll: 0, halfStat: 50, lineOfFireClear: false).Move);
    }

    [Fact]
    public void ACreatureThatDIEDDuringItsOwnWalkDoesNotAttack() {
        // Flag 2 is CAF_DEAD, and this guard exists only in the 1.02 CD build — the floppy attacks
        // regardless. Porting the floppy behaviour gives these creatures a free action every turn.
        Assert.False(MonsterTurnRoutines.ActsAfterMoving(secondFlagSet: true));
        Assert.True(MonsterTurnRoutines.ActsAfterMoving(secondFlagSet: false));
    }
}
