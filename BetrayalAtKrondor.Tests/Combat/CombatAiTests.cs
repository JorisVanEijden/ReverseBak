namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Monster turn decisions and target choice (CBENC.C). The cases below pin the ordering that is easy
/// to get wrong — fleeing preempts everything, species routines bypass the capability cascade — and
/// the fact that targeting returns the nearest match rather than the last one scanned.
/// </summary>
public class CombatAiTests {
    private const int OrdinaryClass = 0x01;
    private const int SpeciesClass = 0x1c;

    private static TargetCandidate At(int x, int y) => new TargetCandidate { X = x, Y = y, StaminaPercent = 100 };

    // ---- action choice -------------------------------------------------------------------

    [Fact]
    public void FleeingPreemptsEverythingElseIncludingASpeciesRoutine() {
        Assert.Equal(AiAction.Flee, CombatAi.ChooseAction(
            SpeciesClass, isFleeing: true, canCastSpells: true, canShoot: true));
    }

    [Fact]
    public void ACreatureWithItsOwnRoutineBypassesTheCascadeEvenIfItCouldCast() {
        // Structurally important: the switch on class id comes before the capability tests, so a
        // spell-capable creature on the bespoke list still runs its own routine.
        Assert.Equal(AiAction.SpeciesSpecific, CombatAi.ChooseAction(
            SpeciesClass, isFleeing: false, canCastSpells: true, canShoot: true));
    }

    [Fact]
    public void TheCascadePrefersCastingThenShootingThenClosing() {
        Assert.Equal(AiAction.Cast, CombatAi.ChooseAction(OrdinaryClass, false, canCastSpells: true, canShoot: true));
        Assert.Equal(AiAction.Shoot, CombatAi.ChooseAction(OrdinaryClass, false, canCastSpells: false, canShoot: true));
        Assert.Equal(AiAction.MeleeOrMove, CombatAi.ChooseAction(OrdinaryClass, false, false, false));
    }

    [Theory]
    [InlineData(0x13)]
    [InlineData(0x31)]
    [InlineData(0x29)]
    [InlineData(0x39)]
    [InlineData(0x36)]
    [InlineData(0x21)]
    public void TheBespokeClassesAreRecognised(int classId) {
        Assert.True(CombatAi.HasSpeciesRoutine(classId));
    }

    [Fact]
    public void AnOrdinaryClassIsNotOnThatList() {
        Assert.False(CombatAi.HasSpeciesRoutine(OrdinaryClass));
    }

    // ---- which bespoke routine (CBENC.C:925) ----------------------------------------------

    [Theory]
    [InlineData(0x13, CombatAi.SpeciesRoutine.MeleeAdjacentElseCastOrShoot)]
    [InlineData(0x31, CombatAi.SpeciesRoutine.WalkRandomTileThenAttackOrBrace)]
    [InlineData(0x29, CombatAi.SpeciesRoutine.RangedAttackTurn)]
    [InlineData(0x2a, CombatAi.SpeciesRoutine.RangedAttackTurn)]
    [InlineData(0x2b, CombatAi.SpeciesRoutine.RangedAttackTurn)]
    [InlineData(0x39, CombatAi.SpeciesRoutine.RangedAttackTurn)]
    [InlineData(0x38, CombatAi.SpeciesRoutine.RangedKnockbackElseCloseIn)]
    [InlineData(0x1d, CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate)]
    [InlineData(0x1f, CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate)]
    [InlineData(0x20, CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate)]
    [InlineData(0x21, CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate)]
    [InlineData(0x1c, CombatAi.SpeciesRoutine.RandomRangedVariantBeyondTwoTiles)]
    [InlineData(0x36, CombatAi.SpeciesRoutine.RangedPoisonAttack)]
    public void EachBespokeClassNamesItsOwnRoutine(int classId, CombatAi.SpeciesRoutine expected) =>
        Assert.Equal(expected, CombatAi.SpeciesRoutineOf(classId));

    [Fact]
    public void AnOrdinaryClassNamesNoRoutine() =>
        Assert.Null(CombatAi.SpeciesRoutineOf(OrdinaryClass));

    [Fact]
    public void THETWOTablesAgree_BecauseTheyAreOne() {
        foreach (int classId in new[] {
                     0x13, 0x31, 0x29, 0x2a, 0x2b, 0x39, 0x38, 0x1d, 0x1f, 0x20, 0x21, 0x1c, 0x36,
                 }) {
            Assert.True(CombatAi.HasSpeciesRoutine(classId));
            Assert.NotNull(CombatAi.SpeciesRoutineOf(classId));
        }
    }

    // The distance/roll rules are NOT tested here any more. They live in the routine types --
    // MonsterTurnRoutines, MonsterChargeTurn, MonsterVariantAttackTurn, MonsterHeavyRangedTurn --
    // which had them right before this file had them at all, with their own tests. A second set of
    // assertions here is how the two models drifted in the first place.

    // ---- target selection ----------------------------------------------------------------

    [Fact]
    public void TheNearestMatchWinsNotTheLastOneScanned() {
        // The far candidate is scanned last; each acceptance tightens the radius, so the near one
        // still wins.
        var candidates = new List<TargetCandidate> { At(1, 0), At(6, 0) };

        int chosen = CombatAi.SelectTarget(0, 0, candidates, maxDistance: 10, TargetRole.Anyone, 0);

        Assert.Equal(0, chosen);
    }

    [Fact]
    public void AndThatHoldsWhenTheNearOneComesLastToo() {
        var candidates = new List<TargetCandidate> { At(6, 0), At(1, 0) };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Anyone, 0));
    }

    [Fact]
    public void NobodyInRangeMeansNoTarget() {
        var candidates = new List<TargetCandidate> { At(7, 12) };

        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, candidates, maxDistance: 3, TargetRole.Anyone, 0));
    }

    [Fact]
    public void TheDeadAreNeverTargeted() {
        var candidates = new List<TargetCandidate> { new TargetCandidate { X = 1, Y = 0, IsDead = true } };

        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Anyone, 0));
    }

    [Fact]
    public void ACandidateWithAlliesTooCloseIsPassedOverForAStraggler() {
        var candidates = new List<TargetCandidate> {
            new TargetCandidate { X = 1, Y = 0, AlliesNearby = 2, StaminaPercent = 100 },
            At(4, 0),
        };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Anyone, minAllyClearance: 2));
    }

    [Fact]
    public void ZeroClearanceTurnsThatTestOffEntirely() {
        var candidates = new List<TargetCandidate> {
            new TargetCandidate { X = 1, Y = 0, AlliesNearby = 5, StaminaPercent = 100 },
        };

        Assert.Equal(0, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Anyone, minAllyClearance: 0));
    }

    [Fact]
    public void TheSpellcasterFilterPicksOffMages() {
        var candidates = new List<TargetCandidate> {
            At(1, 0),
            new TargetCandidate { X = 3, Y = 0, CanCastSpells = true, StaminaPercent = 100 },
        };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Spellcaster, 0));
    }

    [Fact]
    public void TheWoundedFilterTakesAnyoneAtHalfStaminaOrLess() {
        var candidates = new List<TargetCandidate> {
            new TargetCandidate { X = 1, Y = 0, StaminaPercent = 51 },
            new TargetCandidate { X = 3, Y = 0, StaminaPercent = 50 },
        };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Wounded, 0));
    }

    [Fact]
    public void EngagedAndDisengagedAreOppositeSidesOfTheSameTest() {
        var busy = new TargetCandidate { X = 1, Y = 0, HasTarget = true, TargetIsDead = false, StaminaPercent = 100 };
        var freed = new TargetCandidate { X = 2, Y = 0, HasTarget = true, TargetIsDead = true, StaminaPercent = 100 };
        var idle = new TargetCandidate { X = 3, Y = 0, HasTarget = false, StaminaPercent = 100 };
        var candidates = new List<TargetCandidate> { busy, freed, idle };

        Assert.Equal(0, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Engaged, 0));
        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Disengaged, 0));
    }

    [Fact]
    public void SomeoneWithNoTargetAtAllSatisfiesNeither() {
        var candidates = new List<TargetCandidate> {
            new TargetCandidate { X = 1, Y = 0, HasTarget = false, StaminaPercent = 100 },
        };

        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Engaged, 0));
        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.Disengaged, 0));
    }

    [Fact]
    public void TheMissileFilterPicksShooters() {
        var candidates = new List<TargetCandidate> {
            At(1, 0),
            new TargetCandidate { X = 3, Y = 0, CanShoot = true, StaminaPercent = 100 },
        };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.MissileCapable, 0));
    }

    [Fact]
    public void TheLeaderFilterPicksWhoeverIsGoingForTheLeader() {
        var candidates = new List<TargetCandidate> {
            At(1, 0),
            new TargetCandidate { X = 3, Y = 0, TargetsTheLeader = true, StaminaPercent = 100 },
        };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, candidates, 10, TargetRole.TargetingTheLeader, 0));
    }

    [Fact]
    public void AnUnrecognisedFilterMatchesNobodyRatherThanEverybody() {
        // The original's switch defaults to skipping, so a bad filter leaves the monster targetless.
        var candidates = new List<TargetCandidate> { At(1, 0) };

        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, candidates, 10, (TargetRole)99, 0));
    }

    [Fact]
    public void DistanceIsChebyshevSoADiagonalNeighbourIsAsCloseAsAnOrthogonalOne() {
        var candidates = new List<TargetCandidate> { At(1, 1) };

        Assert.Equal(0, CombatAi.SelectTarget(0, 0, candidates, maxDistance: 1, TargetRole.Anyone, 0));
    }
}
