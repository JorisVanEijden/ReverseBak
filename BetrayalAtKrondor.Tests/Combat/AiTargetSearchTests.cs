namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// How far a monster looks for a target, and how much room it needs around one (TASK-227).
/// </summary>
/// <remarks>
/// <b>Both of these were resolver-wide constants that matched nothing in the original</b>, and the
/// live caller took both defaults — 12 cells and zero clearance — so every monster searched a radius
/// no family uses and every monster was a perfect shot. These pin the per-behaviour values instead.
/// </remarks>
public class AiTargetSearchTests {
    [Fact]
    public void EachFAMILYSearchesItsOwnDistance() {
        // The three families named in IDA, and the "anyone" fallback they share.
        Assert.Equal(100, CombatAi.SearchRadiusFor(AiAction.MeleeOrMove, TargetRole.Spellcaster));
        Assert.Equal(10, CombatAi.SearchRadiusFor(AiAction.Shoot, TargetRole.Spellcaster));
        Assert.Equal(6, CombatAi.SearchRadiusFor(AiAction.MeleeOrMove, TargetRole.Anyone));
        Assert.Equal(6, CombatAi.SearchRadiusFor(AiAction.Shoot, TargetRole.Anyone));
    }

    [Fact]
    public void ACasterUsesTheWrapperFamilysSix_NotMelees100() {
        // *** The arm that is easy to miss. *** Casting runs through the combat_ai_execute_turn
        // wrappers, which pass 6 — but a caster picks a SPECIFIC role, so a rule written as
        // "Anyone -> 6, else melee -> 100" hands it 100 and has mages engaging across the field.
        Assert.Equal(AiTurnPackets.TargetSearchRadius,
            CombatAi.SearchRadiusFor(AiAction.Cast, TargetRole.Spellcaster));
        Assert.Equal(6, CombatAi.SearchRadiusFor(AiAction.Cast, TargetRole.Wounded));
    }

    [Fact]
    public void NoFamilyUsesTwelve() {
        // The value that was actually shipping. Named so the regression has something to fail on.
        foreach (AiAction action in new[] { AiAction.MeleeOrMove, AiAction.Shoot, AiAction.Cast }) {
            foreach (TargetRole role in new[] { TargetRole.Anyone, TargetRole.Spellcaster }) {
                Assert.NotEqual(12, CombatAi.SearchRadiusFor(action, role));
            }
        }
    }

    [Fact]
    public void ClearanceFallsFromFourToZeroAcrossTheAccuracyRange() {
        Assert.Equal(4, CombatAi.AllyClearanceForAccuracy(0));
        Assert.Equal(3, CombatAi.AllyClearanceForAccuracy(25));
        Assert.Equal(2, CombatAi.AllyClearanceForAccuracy(50));
        Assert.Equal(1, CombatAi.AllyClearanceForAccuracy(75));
        Assert.Equal(0, CombatAi.AllyClearanceForAccuracy(100));
    }

    [Fact]
    public void THESTEPSDoNotFallOnMultiplesOf25() {
        // *** The whole reason this needs a test rather than a table. *** idiv truncates toward
        // zero, so `4 - (accuracy + 24) / 25` breaks at 1, 26, 51 and 76 — NOT at 0/25/50/75. The
        // five-row table in the task samples exactly the values where both readings agree, so a
        // port built from it is wrong for one accuracy point in four and passes the table anyway.
        Assert.Equal(4, CombatAi.AllyClearanceForAccuracy(0));
        Assert.Equal(3, CombatAi.AllyClearanceForAccuracy(1));    // already stepped
        Assert.Equal(3, CombatAi.AllyClearanceForAccuracy(25));   // has NOT stepped again
        Assert.Equal(2, CombatAi.AllyClearanceForAccuracy(26));
        Assert.Equal(2, CombatAi.AllyClearanceForAccuracy(50));
        Assert.Equal(1, CombatAi.AllyClearanceForAccuracy(51));
        Assert.Equal(1, CombatAi.AllyClearanceForAccuracy(75));
        Assert.Equal(0, CombatAi.AllyClearanceForAccuracy(76));
    }

    [Fact]
    public void AnAccuracyAboveOneHundredDoesNotGoNegative() {
        // A negative clearance is not a thing the rule can mean — a perfect shot already fires
        // regardless — and a non-zero value re-arms the disqualification test in SelectTarget.
        Assert.Equal(0, CombatAi.AllyClearanceForAccuracy(101));
        Assert.Equal(0, CombatAi.AllyClearanceForAccuracy(255));
    }

    private static TargetCandidate At(int x, int y) =>
        new TargetCandidate { X = x, Y = y };

    [Fact]
    public void TheMELEESelectorSkipsACandidateAtExactlyTheRadius() {
        // *** The two selectors bound distance differently. *** combat_selectTargetByMode @0x63ce6
        // accepts dist <= maxDistance; combat_selectTargetByCriterion @0x64ff6 skips on
        // dist >= maxDistance. At 100 it never shows; at the radius-6 "engage anyone" sweep it is
        // the difference between reaching 5 and reaching 6.
        var six = new[] { At(6, 0) };

        Assert.Equal(0, CombatAi.SelectTarget(0, 0, six, 6, TargetRole.Anyone, 0));
        Assert.Equal(-1, CombatAi.SelectTarget(0, 0, six, 6, TargetRole.Anyone, 0,
            excludeAtMaxDistance: true));
    }

    [Fact]
    public void TheNEARESTCandidateStillWins_UnderEitherBound() {
        var spread = new[] { At(4, 0), At(1, 0), At(3, 0) };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, spread, 6, TargetRole.Anyone, 0));
        Assert.Equal(1, CombatAi.SelectTarget(0, 0, spread, 6, TargetRole.Anyone, 0,
            excludeAtMaxDistance: true));
    }

    [Fact]
    public void TheEQUIDISTANTTieBreakIsTheSameUnderBothBounds() {
        // *** Why the radius and the nearest-so-far bound are separate variables. *** They were one
        // variable that each acceptance tightened; applying the melee `>=` to that tightened value
        // would also reject a candidate at the same distance as the best so far, flipping which of
        // two equidistant targets wins. Nothing observed says the exclusive bound touches the tie
        // break, so it must not.
        var tied = new[] { At(2, 0), At(0, 2) };

        Assert.Equal(1, CombatAi.SelectTarget(0, 0, tied, 6, TargetRole.Anyone, 0));
        Assert.Equal(1, CombatAi.SelectTarget(0, 0, tied, 6, TargetRole.Anyone, 0,
            excludeAtMaxDistance: true));
    }
}
