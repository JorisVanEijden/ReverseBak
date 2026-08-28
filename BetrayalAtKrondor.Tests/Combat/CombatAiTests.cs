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

    // ---- the DISTANCE model, which replaced a melee/ranged split that was wrong ---------------

    [Theory]
    // *** THE THREE THAT WERE BACKWARDS. *** Each was mapped to melee from the original's function
    // name; each opens with a ranged branch. Far enough away, with a passing roll, they SHOOT.
    [InlineData(CombatAi.SpeciesRoutine.RangedKnockbackElseCloseIn, 5, 99)]
    [InlineData(CombatAi.SpeciesRoutine.RandomRangedVariantBeyondTwoTiles, 5, 99)]
    [InlineData(CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate, 5, 99)]
    [InlineData(CombatAi.SpeciesRoutine.RangedPoisonAttack, 5, 99)]
    [InlineData(CombatAi.SpeciesRoutine.RangedAttackTurn, 5, 99)]
    public void THEROUTINESThatWereMappedToMeleeActuallyShoot(
        CombatAi.SpeciesRoutine routine, int distance, int roll) =>
        Assert.Equal(AiAction.Shoot, CombatAi.RangedBranchFor(routine, distance, roll));

    [Theory]
    [InlineData(CombatAi.SpeciesRoutine.RangedKnockbackElseCloseIn, 1)]
    [InlineData(CombatAi.SpeciesRoutine.RangedPoisonAttack, 1)]
    [InlineData(CombatAi.SpeciesRoutine.RandomRangedVariantBeyondTwoTiles, 2)]
    [InlineData(CombatAi.SpeciesRoutine.ShootAtRangeElseDelegate, 2)]
    public void TOOCLOSeRefusesTheRangedBranch(CombatAi.SpeciesRoutine routine, int distance) =>
        // Null means "fall back", which is where the melee/delegate behaviour lives.
        Assert.Null(CombatAi.RangedBranchFor(routine, distance, 99));

    [Fact]
    public void THECOINFLIPRoutineHasNoMinimumDistance() {
        // 0x29's group is gated on line of fire and RND(100) >= 50 and NOTHING ELSE -- it will shoot
        // an adjacent target. A distance model that assumed every ranged routine needs range would
        // have got this one wrong in the other direction.
        Assert.Equal(0, CombatAi.MinRangedDistance(CombatAi.SpeciesRoutine.RangedAttackTurn));
        Assert.Equal(AiAction.Shoot,
            CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.RangedAttackTurn, 1, 50));
        Assert.Null(CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.RangedAttackTurn, 9, 49));
    }

    [Fact]
    public void THEWALKROUTINECastsOnAnINVERTEDRoll() {
        // Its roll is `< 0x50`, an upper bound where every other routine uses a floor. Folding it
        // into RangedRollFloor would read as the same rule and silently invert it.
        Assert.Equal(AiAction.Cast,
            CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.WalkRandomTileThenAttackOrBrace, 4, 0x4f));
        Assert.Null(
            CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.WalkRandomTileThenAttackOrBrace, 4, 0x50));
    }

    [Fact]
    public void THEDISTANCEConditionalOneCastsAtCloserTargets() {
        // RND(10) >= distance picks the spell, so the CLOSER target is likelier to be cast at --
        // the reverse of the intuitive reading, and worth a test for that reason alone.
        Assert.Equal(AiAction.Cast,
            CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.MeleeAdjacentElseCastOrShoot, 2, 8));
        Assert.Equal(AiAction.Shoot,
            CombatAi.RangedBranchFor(CombatAi.SpeciesRoutine.MeleeAdjacentElseCastOrShoot, 9, 1));
    }

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
